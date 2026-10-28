using System;

namespace DTools.Core
{
    /// <summary>
    /// Các công thức tính toán kỹ thuật điện chuẩn IEC / TCVN thuần C#
    /// </summary>
    public static class ElectricalFormulas
    {
        private static readonly double[] StandardCableSections = new double[]
        {
            1.5, 2.5, 4.0, 6.0, 10.0, 16.0, 25.0, 35.0, 50.0, 70.0, 95.0, 120.0, 150.0, 185.0, 240.0, 300.0
        };

        private static readonly double[] StandardBreakerRatings = new double[]
        {
            6, 10, 16, 20, 25, 32, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250, 1600
        };

        /// <summary>
        /// Tính dòng điện định mức 3 pha: I = P / (sqrt(3) * U * cosPhi)
        /// </summary>
        public static double Calculate3PhaseCurrent(double powerWatts, double voltageVolts = 380.0, double powerFactor = 0.85)
        {
            if (powerWatts <= 0) return 0.0;
            if (voltageVolts <= 0 || powerFactor <= 0) throw new ArgumentException("Điện áp và hệ số công suất phải > 0");
            return powerWatts / (Math.Sqrt(3.0) * voltageVolts * powerFactor);
        }

        /// <summary>
        /// Tính dòng điện định mức 1 pha: I = P / (U * cosPhi)
        /// </summary>
        public static double CalculateSinglePhaseCurrent(double powerWatts, double voltageVolts = 220.0, double powerFactor = 0.9)
        {
            if (powerWatts <= 0) return 0.0;
            if (voltageVolts <= 0 || powerFactor <= 0) throw new ArgumentException("Điện áp và hệ số công suất phải > 0");
            return powerWatts / (voltageVolts * powerFactor);
        }

        /// <summary>
        /// Tính sụt áp trên đường cáp: deltaU = (k * L * I * rho) / S (Volts)
        /// k = sqrt(3) cho 3 pha, k = 2 cho 1 pha. rho đồng = 0.0175 Ohm*mm2/m
        /// </summary>
        public static double CalculateVoltageDrop(double currentAmps, double lengthMeters, double cableSectionMm2, bool isThreePhase = true, double resistivity = 0.0175)
        {
            if (currentAmps <= 0 || lengthMeters <= 0 || cableSectionMm2 <= 0) return 0.0;
            double k = isThreePhase ? Math.Sqrt(3.0) : 2.0;
            return (k * lengthMeters * currentAmps * resistivity) / cableSectionMm2;
        }

        /// <summary>
        /// Chọn tiết diện cáp đồng khuyến nghị theo dòng tải (mật độ J ~ 4 - 6 A/mm2)
        /// </summary>
        public static double SelectRecommendedCable(double currentAmps)
        {
            if (currentAmps <= 0) return 1.5;

            // Bảng tra khả năng tải dòng gần đúng (In Air/Conduit):
            // 1.5: 16A | 2.5: 24A | 4.0: 32A | 6.0: 42A | 10: 58A | 16: 78A | 25: 105A | 35: 130A | 50: 160A...
            double[] maxCurrent = { 16, 24, 32, 42, 58, 78, 105, 130, 160, 205, 250, 290, 335, 380, 450, 520 };

            for (int i = 0; i < maxCurrent.Length; i++)
            {
                if (currentAmps <= maxCurrent[i])
                    return StandardCableSections[i];
            }

            return StandardCableSections[StandardCableSections.Length - 1];
        }

        /// <summary>
        /// Chọn định mức Aptomat tiêu chuẩn bảo vệ quá tải: In >= 1.25 * I_load
        /// </summary>
        public static double SelectRecommendedBreaker(double currentAmps)
        {
            if (currentAmps <= 0) return 10;
            double target = currentAmps * 1.25;

            foreach (double rating in StandardBreakerRatings)
            {
                if (rating >= target)
                    return rating;
            }

            return StandardBreakerRatings[StandardBreakerRatings.Length - 1];
        }
    }
}
