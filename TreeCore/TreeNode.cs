using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FoodEnterpriseIMS.TreeCore
{
    /// <summary>
    /// 内存树形节点模型，对应Python TreeNode dataclass
    /// </summary>
    public class TreeNode
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Dictionary<string, object> Payload { get; set; } = new();
        public int SortOrder { get; set; }
        public string? ParentCode { get; set; }
        public List<TreeNode> Children { get; set; } = new();

        /// <summary>
        /// 序列化为字典，用于导出JSON
        /// </summary>
        public Dictionary<string, object> ToDict()
        {
            return new()
            {
                ["code"] = Code,
                ["title"] = Title,
                ["payload"] = Payload,
                ["sort_order"] = SortOrder,
                ["parent_code"] = ParentCode,
                ["children"] = Children.OrderBy(x => x.SortOrder).Select(x => x.ToDict()).ToList()
            };
        }

        /// <summary>
        /// 从字典反序列化节点（导入用）
        /// </summary>
        public static TreeNode FromDict(Dictionary<string, object> data)
        {
            var node = new TreeNode
            {
                Code = data["code"]?.ToString() ?? "",
                Title = data["title"]?.ToString() ?? "",
                ParentCode = data["parent_code"]?.ToString(),
                SortOrder = Convert.ToInt32(data["sort_order"] ?? 0)
            };

            // 解析payload
            if (data.TryGetValue("payload", out var plObj))
            {
                if (plObj is Dictionary<string, object> plDict)
                    node.Payload = plDict;
                else
                    node.Payload = new();
            }

            // 递归子节点
            if (data.TryGetValue("children", out var childObj) && childObj is List<object> childList)
            {
                foreach (var childItem in childList.OfType<Dictionary<string, object>>())
                {
                    node.Children.Add(FromDict(childItem));
                }
            }
            return node;
        }
    }
}