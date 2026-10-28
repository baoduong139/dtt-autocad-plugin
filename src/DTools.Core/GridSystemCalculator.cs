using System;
using System.Collections.Generic;
using System.Globalization;

namespace DTools.Core
{
    /// <summary>
    /// Thuật toán phân tích và tính toán hình học lưới trục kiến trúc / kết cấu đa nhịp
    /// </summary>
    public static class GridSystemCalculator
    {
        /// <summary>
        /// Phân tích chuỗi nhịp người dùng nhập (hỗ trợ dấu phẩy, khoảng trắng và cú pháp nhân như 3*4000)
        /// </summary>
        public static List<double> ParseSpans(string input)
        {
            var result = new List<double>();
            if (string.IsNullOrWhiteSpace(input)) return result;

            var rawParts = input.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in rawParts)
            {
                string token = part.Trim();
                if (token.Contains("*"))
                {
                    var sub = token.Split('*');
                    if (sub.Length == 2 &&
                        int.TryParse(sub[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) &&
                        double.TryParse(sub[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double spanVal) &&
                        count > 0 && spanVal > 0)
                    {
                        for (int i = 0; i < count; i++) result.Add(spanVal);
                        continue;
                    }
                }

                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) && val > 0)
                {
                    result.Add(val);
                }
            }

            return result;
        }

        /// <summary>
        /// Tính toán tọa độ dồn tích lũy bắt đầu từ 0
        /// Ví dụ: [4000, 5000, 4500] -> [0, 4000, 9000, 13500]
        /// </summary>
        public static List<double> CalculateOffsets(List<double> spans)
        {
            var offsets = new List<double> { 0.0 };
            double accum = 0.0;
            if (spans != null)
            {
                foreach (var s in spans)
                {
                    accum += s;
                    offsets.Add(accum);
                }
            }
            return offsets;
        }

        /// <summary>
        /// Sinh chuỗi ký hiệu trục (Số 1, 2, 3.. cho phương X hoặc Chữ A, B, C.. cho phương Y)
        /// </summary>
        public static List<string> GenerateLabels(int count, bool isAlphabet = false)
        {
            var labels = new List<string>();
            if (count <= 0) return labels;

            if (!isAlphabet)
            {
                for (int i = 1; i <= count; i++)
                {
                    labels.Add(i.ToString());
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    labels.Add(GetAlphabetLabel(i));
                }
            }

            return labels;
        }

        private static string GetAlphabetLabel(int index)
        {
            if (index < 26)
            {
                return ((char)('A' + index)).ToString();
            }

            int first = (index / 26) - 1;
            int second = index % 26;
            return $"{(char)('A' + first)}{(char)('A' + second)}";
        }
    }
}
