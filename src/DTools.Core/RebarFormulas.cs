using System;

namespace DTools.Core
{
    /// <summary>
    /// Các công thức tính toán cốt thép xây dựng chuẩn TCVN thuần C#
    /// </summary>
    public static class RebarFormulas
    {
        /// <summary>
        /// Trọng lượng đơn vị 1 mét thép thanh theo đường kính: W = d^2 / 162 (kg/m)
        /// </summary>
        public static double CalculateWeightPerMeter(double diameterMm)
        {
            if (diameterMm <= 0) return 0.0;
            return (diameterMm * diameterMm) / 162.0;
        }

        /// <summary>
        /// Chiều dài đoạn bẻ móc vuông 90 độ: tối thiểu 6d hoặc 60mm
        /// </summary>
        public static double CalculateHookLength90(double diameterMm)
        {
            if (diameterMm <= 0) return 0.0;
            return Math.Max(6.0 * diameterMm, 60.0);
        }

        /// <summary>
        /// Chiều dài đoạn bẻ móc neo đai 135 độ: tối thiểu 10d hoặc 75mm (TCVN 5574:2018)
        /// </summary>
        public static double CalculateHookLength135(double diameterMm)
        {
            if (diameterMm <= 0) return 0.0;
            return Math.Max(10.0 * diameterMm, 75.0);
        }

        /// <summary>
        /// Tính tổng chu vi đai thép dầm/cột hình chữ nhật: 2*(w + h - 4*cover) + 2*hook135
        /// </summary>
        public static double CalculateStirrupLength(double sectionWidth, double sectionHeight, double coverMm = 25.0, double stirrupDiameterMm = 8.0)
        {
            if (sectionWidth <= 0 || sectionHeight <= 0) return 0.0;
            double coreW = Math.Max(0, sectionWidth - 2.0 * coverMm);
            double coreH = Math.Max(0, sectionHeight - 2.0 * coverMm);
            double perimeter = 2.0 * (coreW + coreH);
            double hooks = 2.0 * CalculateHookLength135(stirrupDiameterMm);
            return perimeter + hooks;
        }

        /// <summary>
        /// Tính tổng khối lượng thanh thép: Tổng chiều dài (m) * Trọng lượng đơn vị (kg/m)
        /// </summary>
        public static double CalculateTotalRebarWeight(double diameterMm, double totalLengthMeters)
        {
            if (diameterMm <= 0 || totalLengthMeters <= 0) return 0.0;
            return totalLengthMeters * CalculateWeightPerMeter(diameterMm);
        }
    }
}
