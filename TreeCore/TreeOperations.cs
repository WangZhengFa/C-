using System;
using System.Collections.Generic;
using System.Linq;

namespace FoodEnterpriseIMS.TreeCore
{
    /// <summary>
    /// 树形业务操作层，封装所有业务逻辑，对应Python TreeOperations
    /// </summary>
    public class TreeOperations
    {
        private readonly TreeRepository _repo;
        public TreeRepository Repo => _repo;

        public TreeOperations(TreeRepository repository)
        {
            _repo = repository;
        }

        #region 查询
        /// <summary>
        /// 构建完整内存树，返回根节点集合
        /// </summary>
        public List<TreeNode> BuildTree(int? maxDepth = null)
        {
            var rows = _repo.ListNodes(maxDepth);
            Dictionary<string, TreeNode> allNodes = new();
            foreach (var r in rows)
            {
                var node = new TreeNode
                {
                    Code = r["code"].ToString()!,
                    Title = r["title"].ToString()!,
                    ParentCode = r["parent_code"]?.ToString(),
                    SortOrder = Convert.ToInt32(r["sort_order"]),
                    Payload = (Dictionary<string, object>)r["payload"]
                };
                allNodes[node.Code] = node;
            }

            List<TreeNode> roots = new();
            foreach (var n in allNodes.Values)
            {
                if (string.IsNullOrEmpty(n.ParentCode) || !allNodes.ContainsKey(n.ParentCode))
                    roots.Add(n);
                else
                    allNodes[n.ParentCode].Children.Add(n);
            }
            // 同级排序
            foreach (var n in allNodes.Values)
                n.Children.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            roots.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return roots;
        }

        /// <summary>
        /// 根据编码BFS查找节点
        /// </summary>
        public TreeNode? FindNode(string code)
        {
            var queue = BuildTree().ToList();
            while (queue.Count > 0)
            {
                var cur = queue[0];
                queue.RemoveAt(0);
                if (cur.Code == code) return cur;
                queue.AddRange(cur.Children);
            }
            return null;
        }

        /// <summary>
        /// 按自定义条件搜索所有匹配节点
        /// </summary>
        public List<TreeNode> Search(Func<TreeNode, bool> predicate)
        {
            List<TreeNode> res = new();
            var queue = BuildTree().ToList();
            while (queue.Count > 0)
            {
                var cur = queue[0];
                queue.RemoveAt(0);
                if (predicate(cur)) res.Add(cur);
                queue.AddRange(cur.Children);
            }
            return res;
        }
        #endregion

        #region 增删改移动
        /// <summary>
        /// 新增节点，自动生成编码、排序
        /// </summary>
        public TreeNode AddNode(string title, string? parentCode = null, Dictionary<string, object>? payload = null, int? insertIndex = null)
        {
            var allSiblings = _repo.ListNodes()
                .Where(x => TreeRepository.NormalizeValue(x["parent_code"]) == parentCode)
                .Select(x => x["code"].ToString()!)
                .ToList();

            string newCode = TreeCodeHelper.NextChildCode(parentCode, allSiblings);
            int idx = insertIndex ?? allSiblings.Count;
            idx = Math.Clamp(idx, 0, allSiblings.Count);
            allSiblings.Insert(idx, newCode);

            var newNode = new TreeNode
            {
                Code = newCode,
                Title = title,
                ParentCode = parentCode,
                SortOrder = idx + 1,
                Payload = payload ?? new()
            };
            _repo.SaveNode(newNode);
            _repo.Resequence(parentCode, allSiblings);
            return newNode;
        }

        /// <summary>
        /// 仅更新标题、Payload，不修改编码和父级
        /// </summary>
        public TreeNode UpdateNode(string code, string? title = null, Dictionary<string, object>? payload = null)
        {
            var data = _repo.GetNode(code) ?? throw new ArgumentException("节点不存在");
            var node = new TreeNode
            {
                Code = code,
                Title = title ?? data["title"].ToString()!,
                ParentCode = TreeRepository.NormalizeValue(data["parent_code"]),
                SortOrder = Convert.ToInt32(data["sort_order"]),
                Payload = payload ?? (Dictionary<string, object>)data["payload"]
            };
            _repo.SaveNode(node);
            return node;
        }

        /// <summary>
        /// 删除节点，cascade=true递归删除所有子节点
        /// </summary>
        public void DeleteNode(string code, bool cascade = true)
        {
            TreeCodeHelper.Validate(code);
            List<string> delCodes = new() { code };
            if (cascade)
            {
                var all = _repo.ListNodes();
                delCodes.AddRange(all
                    .Where(x => x["code"].ToString()!.StartsWith(code) && x["code"].ToString() != code)
                    .Select(x => x["code"].ToString()!));
            }
            // 长编码先删，避免子节点残留
            foreach (var c in delCodes.OrderByDescending(x => x.Length))
                _repo.DeleteNode(c);
        }

        /// <summary>
        /// 移动节点，支持更换父级、重新生成编码、指定插入位置
        /// </summary>
        public TreeNode MoveNode(string code, string? newParent, bool regenerateCodes = false, int? targetIndex = null, bool resequenceChildren = true)
        {
            var nodeData = _repo.GetNode(code) ?? throw new ArgumentException("节点不存在");
            // 禁止移动到自己子树下或自身
            if (code == newParent)
                throw new InvalidOperationException("无法将节点移动到自身下");
            if (TreeCodeHelper.IsAncestor(code, newParent))
                throw new InvalidOperationException("无法将节点移动到其子节点下");

            // 获取新父同级现有编码
            var siblings = _repo.ListNodes()
                .Where(x => TreeRepository.NormalizeValue(x["parent_code"]) == newParent && x["code"].ToString() != code)
                .Select(x => x["code"].ToString())
                .ToList();

            string oldCode = code;
            string targetCode = code;
            // 需要重新生成整套子编码
            if (regenerateCodes)
            {
                targetCode = TreeCodeHelper.NextChildCode(newParent, siblings);
                var allNodes = _repo.ListNodes();
                var descendants = allNodes
                    .Where(x => x["code"].ToString()!.StartsWith(oldCode))
                    .Select(x => x["code"].ToString()!)
                    .ToList();
                Dictionary<string, string> map = new();
                foreach (var d in descendants)
                {
                    string suffix = d.Substring(oldCode.Length);
                    map[d] = targetCode + suffix;
                }
                _repo.BulkUpdateCodes(map);
                if (_repo.GetNode(oldCode) != null)
                    _repo.DeleteNode(oldCode);
                code = targetCode;
                nodeData = _repo.GetNode(code)!;
            }

            int insertIdx = targetIndex ?? siblings.Count;
            insertIdx = Math.Clamp(insertIdx, 0, siblings.Count);
            siblings.Insert(insertIdx, code);

            var moved = new TreeNode
            {
                Code = code,
                Title = nodeData["title"].ToString()!,
                ParentCode = newParent,
                SortOrder = insertIdx + 1,
                Payload = (Dictionary<string, object>)nodeData["payload"]
            };
            _repo.SaveNode(moved);
            if (resequenceChildren)
                _repo.Resequence(newParent, siblings);
            return moved;
        }

        /// <summary>
        /// 统一规范化所有同级sort_order从1开始连续
        /// </summary>
        public int NormalizeAllSortOrders()
        {
            var all = _repo.ListNodes();
            Dictionary<string?, List<string>> groups = new();
            foreach (var item in all)
            {
                string? p = TreeRepository.NormalizeValue(item["parent_code"]);
                if (!groups.ContainsKey(p)) groups[p] = new();
                groups[p].Add(item["code"].ToString()!);
            }
            int affected = 0;
            foreach (var pair in groups)
            {
                // 按原有sort排序再重编号
                var sorted = all
                    .Where(x => TreeRepository.NormalizeValue(x["parent_code"]) == pair.Key)
                    .OrderBy(x => Convert.ToInt32(x["sort_order"]))
                    .ThenBy(x => x["code"].ToString())
                    .Select(x => x["code"].ToString())
                    .ToList();
                _repo.Resequence(pair.Key, sorted);
                affected++;
            }
            return affected;
        }
        #endregion

        #region 完整性审计、导入导出
        /// <summary>
        /// 校验树编码、父级、重复问题
        /// </summary>
        public Dictionary<string, List<string>> AuditIntegrity()
        {
            Dictionary<string, List<string>> result = new()
            {
                ["invalid_codes"] = new(),
                ["missing_parents"] = new(),
                ["duplicates"] = new()
            };
            var all = _repo.ListNodes();
            HashSet<string> allCodes = new();
            foreach (var row in all)
            {
                string c = row["code"].ToString()!;
                // 校验编码格式
                try
                {
                    TreeCodeHelper.Validate(c);
                }
                catch (Exception ex)
                {
                    result["invalid_codes"].Add($"{c}:{ex.Message}");
                }
                // 重复编码
                if (allCodes.Contains(c))
                    result["duplicates"].Add(c);
                allCodes.Add(c);
                // 父编码不存在
                string? p = TreeRepository.NormalizeValue(row["parent_code"]);
                if (!string.IsNullOrEmpty(p) && !allCodes.Contains(p))
                    result["missing_parents"].Add($"{c} -> {p}");
            }
            return result;
        }

        /// <summary>
        /// 导出整树JSON字典列表
        /// </summary>
        public List<Dictionary<string, object>> ExportTree()
        {
            return BuildTree().Select(x => x.ToDict()).ToList();
        }

        /// <summary>
        /// JSON结构导入树
        /// </summary>
        public void ImportTree(IEnumerable<Dictionary<string, object>> serialized, bool clearExisting = true)
        {
            if (clearExisting) _repo.ClearAll();
            void RecurseInsert(Dictionary<string, object> data, string? parent)
            {
                var node = TreeNode.FromDict(data);
                node.ParentCode = parent;
                _repo.SaveNode(node);
                if (data.TryGetValue("children", out var childrenObj) && childrenObj is IEnumerable<object> childEnum)
                {
                    foreach (var childDict in childEnum.OfType<Dictionary<string, object>>())
                    {
                        RecurseInsert(childDict, node.Code);
                    }
                }
            }
            foreach (var rootDict in serialized)
                RecurseInsert(rootDict, null);
        }
        #endregion
    }
}