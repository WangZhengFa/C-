using System;
using System.Collections.Generic;
using System.Linq;

namespace FoodEnterpriseIMS.TreeCore
{
    /// <summary>
    /// 树形4位分段编码工具，和Python TreeCodeHelper完全逻辑一致
    /// 分段固定4位，根段1001~9999，子段0000~9999
    /// </summary>
    public static class TreeCodeHelper
    {
        public const int SegmentLen = 4;
        public const int RootMin = 1001;
        public const int RootMax = 9999;

        /// <summary>
        /// 标准化编码，空/空白返回null
        /// </summary>
        public static string? Normalize(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;
            return code.Trim();
        }

        /// <summary>
        /// 校验编码合法性，非法直接抛异常
        /// </summary>
        public static void Validate(string? code)
        {
            string realCode = Normalize(code) ?? throw new ArgumentException("node code is empty");
            if (realCode.Length % SegmentLen != 0)
                throw new ArgumentException("node code length must be a multiple of 4");

            List<string> parts = Split(realCode);
            for (int i = 0; i < parts.Count; i++)
            {
                string seg = parts[i];
                if (!seg.All(char.IsDigit))
                    throw new ArgumentException("node code must be numeric");

                int val = int.Parse(seg);
                if (i == 0)
                {
                    if (val < RootMin || val > RootMax)
                        throw new ArgumentException("root segment must stay within 1001-9999");
                }
                else
                {
                    if (val < 0 || val > RootMax)
                        throw new ArgumentException("child segment must stay within 0000-9999");
                }
            }
        }

        /// <summary>
        /// 按4位拆分编码为分段数组
        /// </summary>
        public static List<string> Split(string code)
        {
            List<string> list = new();
            for (int i = 0; i < code.Length; i += SegmentLen)
            {
                list.Add(code.Substring(i, SegmentLen));
            }
            return list;
        }

        /// <summary>
        /// 获取父节点编码，根节点返回null
        /// </summary>
        public static string? ParentCode(string? code)
        {
            string real = Normalize(code);
            if (real == null || real.Length <= SegmentLen)
                return null;
            return real[..^SegmentLen];
        }

        /// <summary>
        /// 计算层级：0=空，4位=1层，8位=2层...
        /// </summary>
        public static int Depth(string? code)
        {
            string real = Normalize(code);
            return real == null ? 0 : real.Length / SegmentLen;
        }

        /// <summary>
        /// 判断ancestor是否是candidate祖先
        /// </summary>
        public static bool IsAncestor(string? ancestor, string? candidate)
        {
            string a = Normalize(ancestor);
            string c = Normalize(candidate);
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(c))
                return false;
            return c.StartsWith(a) && c != a;
        }

        /// <summary>
        /// 生成同级下一个未占用4位子编码
        /// </summary>
        public static string NextChildCode(string? parentCode, IEnumerable<string> siblings)
        {
            string prefix = Normalize(parentCode) ?? "";
            HashSet<int> used = new();
            foreach (var c in siblings)
            {
                string clean = Normalize(c);
                if (clean == null || !clean.StartsWith(prefix)) continue;
                string suffix = clean.Substring(prefix.Length, SegmentLen);
                if (int.TryParse(suffix, out int num))
                    used.Add(num);
            }

            int start = string.IsNullOrEmpty(prefix) ? RootMin : 1;
            int candidate = start;
            while (used.Contains(candidate)) candidate++;
            if (candidate > RootMax)
                throw new InvalidOperationException("Sibling slot exhausted (>9999)");

            return $"{prefix}{candidate:D4}";
        }
    }
}
