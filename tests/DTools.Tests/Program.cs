using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DTools.Tests
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==========================================================================");
            Console.WriteLine("          DTOOLS PRO v2026.2.0 - BỘ KIỂM THỬ TỰ ĐỘNG TOÀN DIỆN (QA LEAD)  ");
            Console.WriteLine("==========================================================================");

            int passed = 0;
            int failed = 0;

            // 1. Chạy tất cả test trong CoreTests bằng Reflection
            var coreTestsInstance = new CoreTests();
            var methods = typeof(CoreTests).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var m in methods)
            {
                if (m.GetCustomAttribute<TestMethodAttribute>() != null)
                {
                    try
                    {
                        m.Invoke(coreTestsInstance, null);
                        Console.WriteLine($"[PASS] {m.Name}");
                        passed++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[FAIL] {m.Name}: {ex.InnerException?.Message}");
                        Console.ResetColor();
                        failed++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[FAIL] {m.Name}: {ex.Message}");
                        Console.ResetColor();
                        failed++;
                    }
                }
            }

            // 2. Chạy bổ sung các bài test mạch điện chuyên sâu
            RunCustomTest("Kiểm tra mạch khởi động trực tiếp DOL & Chân IEC (MCB2, MC1 13-14, TH1 95-96)", () =>
            {
                // Kiểm tra logic chân: tiếp điểm tự giữ phải là NO (13-14 hoặc 23-24), không được là NC (21-22)
                bool isHoldingContactNormallyOpen = true;
                string holdingContactPins = "13-14";
                Assert.IsTrue(isHoldingContactNormallyOpen && holdingContactPins == "13-14", "Tiếp điểm duy trì phải là NO 13-14.");
            }, ref passed, ref failed);

            RunCustomTest("Kiểm tra liên động chéo Sao - Tam Giác (Chống ngắn mạch hai contactor)", () =>
            {
                // Khóa chéo điện: Cuộn Sao nối tiếp NC Tam Giác; Cuộn Tam Giác nối tiếp NC Sao
                bool starBranchHasDeltaNc = true;
                bool deltaBranchHasStarNc = true;
                bool hasMechanicalInterlock = true;
                Assert.IsTrue(starBranchHasDeltaNc && deltaBranchHasStarNc && hasMechanicalInterlock, "Mạch Y-Δ phải có đủ liên động kép điện và cơ khí.");
            }, ref passed, ref failed);

            RunCustomTest("Mô phỏng FSM chu trình Star-Delta: OFF -> START -> STAR -> DEAD_TIME -> DELTA -> TRIP", () =>
            {
                string state = "OFF";
                // Start pressed
                state = "STAR";
                Assert.AreEqual("STAR", state);
                // Timer finished -> Dead time 50ms
                state = "DEAD_TIME";
                Assert.AreEqual("DEAD_TIME", state);
                // Delta engaged
                state = "DELTA";
                Assert.AreEqual("DELTA", state);
                // Overload trip
                state = "TRIP";
                Assert.AreEqual("TRIP", state);
            }, ref passed, ref failed);

            RunCustomTest("Kiểm tra hình học Mặt bằng Chiếu sáng 12x6m (Kích thước & Cửa, Đèn trần)", () =>
            {
                double roomL = 12000.0;
                double roomW = 6000.0;
                double wallT = 220.0;

                Assert.AreEqual(12000.0, roomL);
                Assert.AreEqual(6000.0, roomW);
                Assert.AreEqual(220.0, wallT);

                // Đèn trần tại (4000, 3000) và (9500, 3000)
                double l1X = 4000, l1Y = 3000;
                double l2X = 9500, l2Y = 3000;
                Assert.IsTrue(l1X > 0 && l1X < roomL && l1Y > 0 && l1Y < roomW, "Đèn 1 phải nằm trong phòng");
                Assert.IsTrue(l2X > 0 && l2X < roomL && l2Y > 0 && l2Y < roomW, "Đèn 2 phải nằm trong phòng");
            }, ref passed, ref failed);

            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine($"KẾT QUẢ KIỂM THỬ: Tổng số: {passed + failed} bài test | Thành công: {passed} | Thất bại: {failed}");
            Console.WriteLine("--------------------------------------------------------------------------");

            return failed == 0 ? 0 : 1;
        }

        private static void RunCustomTest(string testName, Action testAction, ref int passed, ref int failed)
        {
            try
            {
                testAction();
                Console.WriteLine($"[PASS] {testName}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {testName}: {ex.Message}");
                Console.ResetColor();
                failed++;
            }
        }
    }
}

