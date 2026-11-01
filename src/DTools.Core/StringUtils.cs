using System;
using System.Globalization;
using System.Text;

namespace DTools.Core
{
    /// <summary>
    /// Các tiện ích xử lý chuỗi tối ưu cho tiếng Việt và tìm kiếm không dấu
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Loại bỏ toàn bộ dấu thanh và dấu phụ tiếng Việt (ví dụ: "Cầu Thang" -> "Cau Thang")
        /// </summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Xử lý riêng ký tự đặc thù đ/Đ của bảng chữ cái tiếng Việt
            text = text.Replace('đ', 'd').Replace('Đ', 'D');

            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Kiểm tra chuỗi nguồn có chứa từ khóa hay không, không phân biệt hoa thường và không phân biệt dấu tiếng Việt
        /// </summary>
        public static bool ContainsUnaccented(string source, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (string.IsNullOrEmpty(source)) return false;

            string normSource = RemoveDiacritics(source).ToLowerInvariant();
            string normQuery = RemoveDiacritics(query).ToLowerInvariant();

            return normSource.Contains(normQuery);
        }
    }
}

