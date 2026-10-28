using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DTools.Core;

namespace DTools.Tests
{
    [TestClass]
    public class CoreTests
    {
        #region 1. Unit Tests Hình Học 2D (Geometry2D)

        [TestMethod]
        public void Point2D_Distance_ShouldBeAccurate()
        {
            var p1 = new Point2D(0, 0);
            var p2 = new Point2D(3, 4);
            Assert.AreEqual(5.0, p1.DistanceTo(p2), 1e-6);
        }

        [TestMethod]
        public void Line2D_Offset_ShouldCreateParallelLines()
        {
            var start = new Point2D(0, 0);
            var end = new Point2D(100, 0);
            var line = new Line2D(start, end);

            var (left, right) = line.Offset(10.0);

            Assert.AreEqual(100.0, left.Length, 1e-6);
            Assert.AreEqual(100.0, right.Length, 1e-6);
            Assert.AreEqual(10.0, left.Start.Y, 1e-6);
            Assert.AreEqual(-10.0, right.Start.Y, 1e-6);
        }

        [TestMethod]
        public void Polyline2D_Area_ShouldCalculateRectangleArea()
        {
            var pts = new[]
            {
                new Point2D(0, 0),
                new Point2D(200, 0),
                new Point2D(200, 100),
                new Point2D(0, 100)
            };
            var poly = new Polyline2D(pts, true);

            double area = poly.CalculateArea();
            Assert.AreEqual(20000.0, area, 1e-6);
            Assert.AreEqual(600.0, poly.TotalLength(), 1e-6);
        }

        #endregion

        #region 2. Unit Tests Công Thức Điện (ElectricalFormulas)

        [TestMethod]
        public void Calculate3PhaseCurrent_ShouldMatchIecStandard()
        {
            // P = 15 kW, U = 380V, cosPhi = 0.85 -> I ~ 26.82 A
            double current = ElectricalFormulas.Calculate3PhaseCurrent(15000, 380, 0.85);
            Assert.IsTrue(current > 26.0 && current < 27.5, $"Dòng điện tính được: {current}A");
        }

        [TestMethod]
        public void SelectRecommendedCable_ShouldSelectAdequateCrossSection()
        {
            // Dòng 26.8A -> cáp 4.0 mm2 hoặc 6.0 mm2
            double section = ElectricalFormulas.SelectRecommendedCable(26.8);
            Assert.IsTrue(section >= 4.0, $"Tiết diện cáp: {section} mm2");
        }

        [TestMethod]
        public void SelectRecommendedBreaker_ShouldSelectStandardAptomat()
        {
            // Dòng 26.8A -> 1.25 * 26.8 = 33.5A -> Aptomat 40A
            double breaker = ElectricalFormulas.SelectRecommendedBreaker(26.8);
            Assert.AreEqual(40.0, breaker);
        }

        [TestMethod]
        public void CalculateVoltageDrop_ShouldBeProportionalToLength()
        {
            double dropShort = ElectricalFormulas.CalculateVoltageDrop(30, 20, 6.0, true);
            double dropLong = ElectricalFormulas.CalculateVoltageDrop(30, 40, 6.0, true);
            Assert.AreEqual(dropShort * 2.0, dropLong, 1e-4);
        }

        #endregion

        #region 3. Unit Tests Cốt Thép TCVN (RebarFormulas)

        [TestMethod]
        public void CalculateWeightPerMeter_ShouldFollowStandardFormula()
        {
            // Phi 18: W = 18^2 / 162 = 324 / 162 = 2.0 kg/m
            double w18 = RebarFormulas.CalculateWeightPerMeter(18);
            Assert.AreEqual(2.0, w18, 1e-4);

            // Phi 12: W = 144 / 162 ~ 0.888 kg/m
            double w12 = RebarFormulas.CalculateWeightPerMeter(12);
            Assert.IsTrue(Math.Abs(w12 - 0.8888) < 0.001);
        }

        [TestMethod]
        public void CalculateHookLength135_ShouldEnforceMinimumLength()
        {
            // Phi 6: 10d = 60mm -> Tối thiểu 75mm theo tiêu chuẩn TCVN 5574:2018
            double hook6 = RebarFormulas.CalculateHookLength135(6);
            Assert.AreEqual(75.0, hook6);

            // Phi 10: 10d = 100mm -> 100mm
            double hook10 = RebarFormulas.CalculateHookLength135(10);
            Assert.AreEqual(100.0, hook10);
        }

        [TestMethod]
        public void CalculateStirrupLength_ShouldBePositiveAndRealistic()
        {
            // Dầm 300x500, cover 25mm, đai phi 8 (hook135 = 80mm)
            // coreW = 300 - 50 = 250, coreH = 500 - 50 = 450
            // chu vi = 2 * (250 + 450) = 1400. Móc = 2 * 80 = 160. Tổng = 1560 mm
            double len = RebarFormulas.CalculateStirrupLength(300, 500, 25, 8);
            Assert.AreEqual(1560.0, len, 1e-4);
        }

        #endregion

        #region 4. Unit Tests Bảng Kê Khối Lượng (BoqCalculator)

        [TestMethod]
        public void BoqSummary_AddOrAggregate_ShouldSumQuantities()
        {
            var boq = new BoqSummary();
            boq.AddOrAggregate(new BoqItem { Code = "CB01", Name = "MCB 1P 16A", Quantity = 5, UnitPrice = 85000 });
            boq.AddOrAggregate(new BoqItem { Code = "CB01", Name = "MCB 1P 16A", Quantity = 3, UnitPrice = 85000 });

            Assert.AreEqual(1, boq.Items.Count);
            Assert.AreEqual(8.0, boq.Items[0].Quantity);
            Assert.AreEqual(8.0 * 85000, boq.TotalAmount);
        }

        [TestMethod]
        public void BoqSummary_ExportToCsv_ShouldProduceValidHeaderAndRows()
        {
            var boq = new BoqSummary();
            boq.AddOrAggregate(new BoqItem { Code = "SK01", Name = "Ổ Cắm Đôi 16A", Unit = "Bộ", Quantity = 10, UnitPrice = 120000 });

            string csv = boq.ExportToCsv();
            Assert.IsTrue(csv.Contains("STT,Mã Hiệu,Tên Thiết Bị"));
            Assert.IsTrue(csv.Contains("\"Ổ Cắm Đôi 16A\""));
            Assert.IsTrue(csv.Contains("TỔNG CỘNG"));
        }

        #endregion

        #region 5. Unit Tests Bounded LRU Cache (8GB RAM Optimization)

        [TestMethod]
        public void BoundedLruCache_ShouldEvictOldestWhenCapacityExceeded()
        {
            var cache = new BoundedLruCache<string, string>(capacity: 3);
            cache.Set("A", "Apple");
            cache.Set("B", "Banana");
            cache.Set("C", "Cherry");

            Assert.AreEqual(3, cache.Count);

            // Thêm phần tử thứ 4 -> "A" phải bị thu hồi
            cache.Set("D", "Date");

            Assert.AreEqual(3, cache.Count);
            Assert.IsFalse(cache.TryGet("A", out _), "Khóa 'A' phải bị thu hồi");
            Assert.IsTrue(cache.TryGet("B", out var bVal) && bVal == "Banana");
            Assert.IsTrue(cache.TryGet("D", out var dVal) && dVal == "Date");
        }

        [TestMethod]
        public void BoundedLruCache_AccessedItem_ShouldNotBeEvictedEarly()
        {
            var cache = new BoundedLruCache<string, string>(capacity: 3);
            cache.Set("A", "Apple");
            cache.Set("B", "Banana");
            cache.Set("C", "Cherry");

            // Đọc lại "A" để biến "A" thành recently used
            cache.TryGet("A", out _);

            // Thêm "D" -> "B" là phần tử cũ nhất hiện tại, phải bị thu hồi thay vì "A"
            cache.Set("D", "Date");

            Assert.IsTrue(cache.TryGet("A", out _), "Khóa 'A' vừa dùng nên không được bị thu hồi");
            Assert.IsFalse(cache.TryGet("B", out _), "Khóa 'B' ít dùng nhất phải bị thu hồi");
        }

        #endregion

        #region 6. Unit Tests Toàn Vẹn Dữ Liệu Ký Hiệu (symbols_database.json)

        [TestMethod]
        public void SymbolsDatabase_ShouldBeValidAndContainSymbols()
        {
            string jsonPath = ResourceLocator.GetSymbolsDatabasePath();
            Assert.IsTrue(File.Exists(jsonPath), $"Tệp ký hiệu không tồn tại tại: {jsonPath}");

            string content = File.ReadAllText(jsonPath);
            Assert.IsFalse(string.IsNullOrEmpty(content));

            // Kiểm tra không còn dấu phẩy thừa trước ngoặc đóng
            var trailingCommaRegex = new Regex(@",\s*([}\]])");
            Assert.IsFalse(trailingCommaRegex.IsMatch(content), "File JSON chứa dấu phẩy thừa trước } hoặc ]!");

            // Đếm số lượng ký hiệu (id) - Cơ sở dữ liệu mở rộng tối thiểu 58 mục (thực tế 70 mục)
            var idMatches = Regex.Matches(content, @"""id""\s*:\s*""([^""]+)""");
            Assert.IsTrue(idMatches.Count >= 58, $"Số lượng ký hiệu trong database: {idMatches.Count}");
        }

        #endregion

        #region 7. Unit Tests Safe Startup & Diagnostic Logger (DToolsLogger)

        [TestMethod]
        public void SafeStartup_Configuration_ShouldDefaultToTrueAndLazyLoading()
        {
            Assert.IsTrue(DToolsConfiguration.SafeStartupEnabled, "SafeStartupEnabled mặc định phải là TRUE");
            Assert.IsFalse(DToolsConfiguration.IsPaletteInitialized, "Palette không được khởi tạo trước khi người dùng yêu cầu");
            Assert.IsFalse(DToolsConfiguration.IsYqMenuLoaded, "Menu YQ không được nạp tự động khi khởi động");
        }

        [TestMethod]
        public void DToolsLogger_LogStartup_ShouldCreateLogFiles()
        {
            DToolsLogger.LogStartup("AutoCAD 2021 (R24.0)", DToolsConfiguration.Version, safeStartup: true);
            Assert.IsTrue(Directory.Exists(DToolsLogger.LogDirectory), "Thư mục Logs phải được tạo");
            Assert.IsTrue(File.Exists(DToolsLogger.StartupLogPath), "File dtools_startup.log phải tồn tại");

            string startupContent = File.ReadAllText(DToolsLogger.StartupLogPath);
            StringAssert.Contains(startupContent, "DTOOLS STARTUP AUDIT");
            StringAssert.Contains(startupContent, "AutoCAD 2021 (R24.0)");
            StringAssert.Contains(startupContent, "ENABLED (Lazy Loading Active)");
        }

        [TestMethod]
        public void DToolsLogger_LogException_ShouldRecordInnerExceptionAndStackTrace()
        {
            try
            {
                try
                {
                    throw new InvalidOperationException("Inner database connection timeout");
                }
                catch (Exception inner)
                {
                    throw new ApplicationException("Outer task execution failed", inner);
                }
            }
            catch (Exception ex)
            {
                DToolsLogger.LogException("UnitTestTask", ex);
            }

            Assert.IsTrue(File.Exists(DToolsLogger.DailyLogPath), "File daily log phải tồn tại");
            string dailyContent = File.ReadAllText(DToolsLogger.DailyLogPath);
            StringAssert.Contains(dailyContent, "Outer task execution failed");
            StringAssert.Contains(dailyContent, "Inner database connection timeout");
            StringAssert.Contains(dailyContent, "ApplicationException");
            StringAssert.Contains(dailyContent, "InvalidOperationException");
        }

        #endregion
    }
}
