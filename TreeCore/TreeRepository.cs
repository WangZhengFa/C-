using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySqlConnector;
using Newtonsoft.Json;

namespace FoodEnterpriseIMS.TreeCore
{
    /// <summary>
    /// MySQL树形仓储，对应Python TreeRepository
    /// 支持多树隔离 tree_key
    /// </summary>
    public class TreeRepository
    {
        private readonly MySqlConnection _dbConn;
        public string TableName { get; } = "tree_nodes";
        public string? TreeKey { get; }
        public string TreeKeyColumn { get; } = "tree_key";

        // 字段映射：逻辑名 -> 数据库真实字段
        private readonly Dictionary<string, string> _columns;

        public TreeRepository(MySqlConnection dbConnection, string? treeKey = null, Dictionary<string, string>? columns = null)
        {
            _dbConn = dbConnection;
            TreeKey = treeKey;
            _columns = columns ?? new()
            {
                ["code"] = "node_code",
                ["parent"] = "parent_code",
                ["title"] = "title",
                ["payload"] = "payload_json",
                ["sort"] = "sort_order"
            };
        }

        #region 内部工具
        private MySqlCommand CreateCmd(string sql, params MySqlParameter[] pars)
        {
            var cmd = new MySqlCommand(sql, _dbConn);
            cmd.Parameters.AddRange(pars);
            return cmd;
        }

        /// <summary>
        /// 生成WHERE过滤片段（tree_key + 可选code条件）
        /// </summary>
        private (string whereSql, List<MySqlParameter> pars) GetWhereScope(string? codeCol = null)
        {
            List<MySqlParameter> pars = new();
            List<string> wheres = new();
            if (!string.IsNullOrEmpty(TreeKey))
            {
                wheres.Add($"{TreeKeyColumn}=@tk");
                pars.Add(new MySqlParameter("@tk", TreeKey));
            }
            if (!string.IsNullOrEmpty(codeCol))
            {
                wheres.Add($"{codeCol}=@cd");
            }
            string sql = wheres.Count == 0 ? "" : string.Join(" AND ", wheres);
            return (sql, pars);
        }

        public static string? NormalizeValue(object? val)
        {
            if (val == null || ReferenceEquals(val, DBNull.Value)) return null;
            string s = val.ToString()!.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static Dictionary<string, object> ParsePayload(object? dbVal)
        {
            string json = NormalizeValue(dbVal) ?? "{}";
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json)!;
            }
            catch
            {
                return new();
            }
        }
        #endregion

        #region 查询
        /// <summary>
        /// 查询当前树全部节点，可限制最大层级
        /// </summary>
        public List<Dictionary<string, object>> ListNodes(int? maxDepth = null)
        {
            var (whereSql, pars) = GetWhereScope();
            string baseCols = $"{_columns["code"]},{_columns["parent"]},{_columns["title"]},{_columns["payload"]},{_columns["sort"]}";
            string sql = $"SELECT {baseCols} FROM {TableName} ";

            List<MySqlParameter> allPars = new(pars);
            if (maxDepth.HasValue && maxDepth > 0)
            {
                int maxByte = maxDepth.Value * TreeCodeHelper.SegmentLen;
                string lenCond = $"LENGTH({_columns["code"]}) <= @maxlen";
                if (string.IsNullOrEmpty(whereSql))
                    whereSql = lenCond;
                else
                    whereSql += " AND " + lenCond;
                allPars.Add(new MySqlParameter("@maxlen", maxByte));
            }

            if (!string.IsNullOrEmpty(whereSql))
                sql += $"WHERE {whereSql}";
            sql += $" ORDER BY {_columns["parent"]} ASC,{_columns["sort"]} ASC,{_columns["code"]} ASC";

            List<Dictionary<string, object>> result = new();
            using var cmd = CreateCmd(sql, allPars.ToArray());
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new()
                {
                    ["code"] = NormalizeValue(reader[0]),
                    ["parent_code"] = NormalizeValue(reader[1]),
                    ["title"] = NormalizeValue(reader[2]) ?? "",
                    ["payload"] = ParsePayload(reader[3]),
                    ["sort_order"] = Convert.ToInt32(reader[4] ?? 0)
                });
            }
            return result;
        }

        /// <summary>
        /// 根据编码查询单个节点
        /// </summary>
        public Dictionary<string, object>? GetNode(string code)
        {
            var (whereSql, pars) = GetWhereScope(_columns["code"]);
            pars.Add(new MySqlParameter("@cd", code));
            string baseCols = $"{_columns["code"]},{_columns["parent"]},{_columns["title"]},{_columns["payload"]},{_columns["sort"]}";
            string sql = $"SELECT {baseCols} FROM {TableName} WHERE {whereSql}";

            using var cmd = CreateCmd(sql, pars.ToArray());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new()
            {
                ["code"] = NormalizeValue(reader[0]),
                ["parent_code"] = NormalizeValue(reader[1]),
                ["title"] = NormalizeValue(reader[2]) ?? "",
                ["payload"] = ParsePayload(reader[3]),
                ["sort_order"] = Convert.ToInt32(reader[4] ?? 0)
            };
        }
        #endregion

        #region 增改删
        /// <summary>
        /// 保存节点（存在更新，不存在插入）
        /// </summary>
        public void SaveNode(TreeNode node)
        {
            TreeCodeHelper.Validate(node.Code);
            string payloadJson = JsonConvert.SerializeObject(node.Payload, Formatting.None);
            var (where, parsWhere) = GetWhereScope(_columns["code"]);
            parsWhere.Add(new MySqlParameter("@cd", node.Code));

            // 判断是否存在
            string existSql = $"SELECT 1 FROM {TableName} WHERE {where}";
            using var existCmd = CreateCmd(existSql, parsWhere.ToArray());
            bool exists = existCmd.ExecuteScalar() != null;

            if (exists)
            {
                // UPDATE
                List<MySqlParameter> pars = new()
                {
                    new("@pcode", node.ParentCode),
                    new("@title", node.Title),
                    new("@pay", payloadJson),
                    new("@sort", node.SortOrder)
                };
                pars.AddRange(parsWhere);
                string setSql = $@"
UPDATE {TableName}
SET {_columns["parent"]}=@pcode,{_columns["title"]}=@title,{_columns["payload"]}=@pay,{_columns["sort"]}=@sort
WHERE {where}";
                using var updCmd = CreateCmd(setSql, pars.ToArray());
                updCmd.ExecuteNonQuery();
            }
            else
            {
                // INSERT
                List<string> colNames = new();
                List<string> place = new();
                List<MySqlParameter> pars = new();
                if (!string.IsNullOrEmpty(TreeKey))
                {
                    colNames.Add(TreeKeyColumn);
                    place.Add("@tk");
                    pars.Add(new("@tk", TreeKey));
                }
                colNames.AddRange(new[] { _columns["code"], _columns["parent"], _columns["title"], _columns["payload"], _columns["sort"] });
                place.AddRange(new[] { "@cd", "@pcode", "@title", "@pay", "@sort" });
                pars.AddRange(new MySqlParameter[]
                {
                    new("@cd", node.Code),
                    new("@pcode", node.ParentCode),
                    new("@title", node.Title),
                    new("@pay", payloadJson),
                    new("@sort", node.SortOrder)
                });
                string insertSql = $"INSERT INTO {TableName} ({string.Join(",", colNames)}) VALUES ({string.Join(",", place)})";
                using var insCmd = CreateCmd(insertSql, pars.ToArray());
                insCmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 删除单个节点
        /// </summary>
        public void DeleteNode(string code)
        {
            var (where, pars) = GetWhereScope(_columns["code"]);
            pars.Add(new MySqlParameter("@cd", code));
            string sql = $"DELETE FROM {TableName} WHERE {where}";
            using var cmd = CreateCmd(sql, pars.ToArray());
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 批量替换编码（移动节点再生编码用）
        /// </summary>
        public void BulkUpdateCodes(Dictionary<string, string> replacements)
        {
            if (replacements.Count == 0) return;
            var basePars = new List<MySqlParameter>();
            string baseWhere = "";
            if (!string.IsNullOrEmpty(TreeKey))
            {
                baseWhere = $"{TreeKeyColumn}=@tk AND ";
                basePars.Add(new("@tk", TreeKey));
            }
            // 从长编码往短更新，防止子节点先匹配旧父编码
            var sorted = replacements.OrderByDescending(kv => kv.Key.Length).ToList();
            foreach (var kv in sorted)
            {
                // 更新自身code
                List<MySqlParameter> p1 = new(basePars);
                p1.Add(new("@new", kv.Value));
                p1.Add(new("@old", kv.Key));
                string sql1 = $"UPDATE {TableName} SET {_columns["code"]}=@new WHERE {baseWhere}{_columns["code"]}=@old";
                CreateCmd(sql1, p1.ToArray()).ExecuteNonQuery();

                // 更新子节点parent_code
                List<MySqlParameter> p2 = new(basePars);
                p2.Add(new("@new", kv.Value));
                p2.Add(new("@old", kv.Key));
                string sql2 = $"UPDATE {TableName} SET {_columns["parent"]}=@new WHERE {baseWhere}{_columns["parent"]}=@old";
                CreateCmd(sql2, p2.ToArray()).ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 批量重排序，按传入顺序更新sort_order=1,2,3...
        /// </summary>
        public void Resequence(string? parentCode, IEnumerable<string> orderedCodes)
        {
            var basePars = new List<MySqlParameter>();
            string baseWhere = "";
            if (!string.IsNullOrEmpty(TreeKey))
            {
                baseWhere = $"{TreeKeyColumn}=@tk AND ";
                basePars.Add(new("@tk", TreeKey));
            }
            foreach (var (code, idx) in orderedCodes.Select((c, i) => (c, i + 1)))
            {
                List<MySqlParameter> pars = new(basePars);
                pars.Add(new("@sort", idx));
                pars.Add(new("@cd", code));
                string sql = $"UPDATE {TableName} SET {_columns["sort"]}=@sort WHERE {baseWhere}{_columns["code"]}=@cd";
                CreateCmd(sql, pars.ToArray()).ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 清空当前tree_key下所有节点
        /// </summary>
        public void ClearAll()
        {
            string sql;
            List<MySqlParameter> pars = new();
            if (string.IsNullOrEmpty(TreeKey))
            {
                sql = $"DELETE FROM {TableName}";
            }
            else
            {
                sql = $"DELETE FROM {TableName} WHERE {TreeKeyColumn}=@tk";
                pars.Add(new("@tk", TreeKey));
            }
            CreateCmd(sql, pars.ToArray()).ExecuteNonQuery();
        }
        #endregion
    }
}