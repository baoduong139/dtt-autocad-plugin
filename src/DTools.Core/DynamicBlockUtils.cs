using System;
using System.Globalization;

namespace DTools.Core
{
    /// <summary>
    /// Các tiện ích tính toán và chuẩn hóa thông số cho Dynamic Blocks kiến trúc & kết cấu
    /// </summary>
    public static class DynamicBlockUtils
    {
        /// <summary>
        /// Tạo nhãn ký hiệu cửa đi chuẩn (vd: D (900x2200))
        /// </summary>
        public static string FormatDoorTag(double width, double height = 2200)
        {
            return string.Format(CultureInfo.InvariantCulture, "D ({0:0}x{1:0})", width, height);
        }

        /// <summary>
        /// Tạo nhãn ký hiệu cột bê tông cốt thép chuẩn (vd: C220x220)
        /// </summary>
        public static string FormatColumnTag(double width, double height)
        {
            return string.Format(CultureInfo.InvariantCulture, "C{0:0}x{1:0}", width, height);
        }

        /// <summary>
        /// Tạo chuỗi ký hiệu cao độ chuẩn kiến trúc có dấu +/- (vd: +0.000, +3.600, -0.450)
        /// </summary>
        public static string FormatElevationTag(double elevationMeters)
        {
            string sign = elevationMeters >= 0 ? "+" : "";
            return string.Format(CultureInfo.InvariantCulture, "{0}{1:F3}", sign, elevationMeters);
        }

        /// <summary>
        /// Kiểm tra bề rộng cửa đi có nằm trong dải tiêu chuẩn xây dựng dân dụng (600 - 3000 mm)
        /// </summary>
        public static bool IsValidDoorWidth(double width)
        {
            return width >= 600 && width <= 3000;
        }

        /// <summary>
        /// Kiểm tra kích thước cột có nằm trong dải tiêu chuẩn kết cấu (150 - 1500 mm)
        /// </summary>
        public static bool IsValidColumnSize(double size)
        {
            return size >= 150 && size <= 1500;
        }
    }
}

