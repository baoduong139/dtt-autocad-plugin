# DTT TOOLS - NHẬT KÝ KIỂM THỬ TỰ ĐỘNG (TEST HISTORY)

Theo dõi lịch sử kiểm thử tự động của dự án DTT CAD MASTER / DTools Pro.

---

## 1. PHƯƠNG PHÁP & NGUYÊN TẮC KIỂM THỬ
1. **Tuyệt đối không phát hành khi có lỗi chưa được xử lý**: Mọi commit chính thức trên nhánh `main` phải đạt 100% PASS trên bộ unit test.
2. **Minh bạch trạng thái**:
   - `PASS`: Đã kiểm thử tự động và cho kết quả chính xác 100%.
   - `FAIL`: Thất bại; yêu cầu dừng phát hành, đọc log và sửa lỗi ngay.
   - `NOT TESTED`: Chưa thể chạy thử nghiệm thực tế (ví dụ: môi trường headless thiếu GUI AutoCAD). Tuyệt đối không giả mạo kết quả PASS.

---

## 2. BẢNG TỔNG HỢP CÁC LẦN CHẠY TEST

| Ngày / Giờ | Commit | Loại Kiểm Thử | Số Bài Test | Đạt (PASS) | Hỏng (FAIL) | Trạng Thái AutoCAD | Ghi Chú |
|:---|:---|:---|:---:|:---:|:---:|:---:|:---|
| 2026-10-29 05:56 | `2a649c7` | Regression & Architecture | 22 | 22 | 0 | NOT TESTED (Headless) | Kiểm thử toàn diện 22 bài test Core/App/Elec/Boq |
| 2026-10-29 06:15 | `pending` | Parametric Grid & Unit Tests | 23 | 23 | 0 | NOT TESTED (Headless) | Thêm test kiểm tra lưới trục đa nhịp DTT_GRID_SYSTEM |

---

## 3. CHI TIẾT DANH MỤC 23 BÀI UNIT TEST ĐÃ ĐẠT CHUẨN

1. `Point2D_Distance_ShouldBeAccurate`: Kiểm tra khoảng cách Euclide 2D.
2. `Line2D_Offset_ShouldCreateParallelLines`: Kiểm tra phép dời song song đoạn thẳng.
3. `Polyline2D_Area_ShouldCalculateRectangleArea`: Kiểm tra diện tích đa giác khép kín.
4. `Calculate3PhaseCurrent_ShouldMatchIecStandard`: Kiểm tra dòng điện định mức 3 pha chuẩn IEC.
5. `SelectRecommendedCable_ShouldSelectAdequateCrossSection`: Chọn tiết diện cáp đồng Cu/PVC/XLPE.
6. `SelectRecommendedBreaker_ShouldSelectStandardAptomat`: Chọn dòng định mức Aptomat bảo vệ.
7. `CalculateVoltageDrop_ShouldBeProportionalToLength`: Tính sụt áp trên đường dây dài.
8. `CalculateWeightPerMeter_ShouldFollowStandardFormula`: Tính trọng lượng riêng thép tròn xây dựng.
9. `CalculateHookLength135_ShouldEnforceMinimumLength`: Tính chiều dài móc thép 135 độ chịu động đất.
10. `CalculateStirrupLength_ShouldBePositiveAndRealistic`: Tính chu vi đai cột, đai dầm.
11. `BoqSummary_AddOrAggregate_ShouldSumQuantities`: Tổng hợp khối lượng dự toán vật tư.
12. `BoqSummary_ExportToCsv_ShouldProduceValidHeaderAndRows`: Xuất file CSV UTF-8 bảng khối lượng.
13. `BoundedLruCache_ShouldEvictOldestWhenCapacityExceeded`: Kiểm tra thuật toán thu hồi bộ nhớ LRU cho RAM 8GB.
14. `BoundedLruCache_AccessedItem_ShouldNotBeEvictedEarly`: Kiểm tra giữ lại phần tử thường dùng trong cache.
15. `SymbolsDatabase_ShouldBeValidAndContainSymbols`: Xác thực cơ sở dữ liệu ký hiệu điện và schema JSON.
16. `SafeStartup_Configuration_ShouldDefaultToTrueAndLazyLoading`: Kiểm tra cơ chế khởi động an toàn và lazy loading.
17. `DToolsLogger_LogStartup_ShouldCreateLogFiles`: Kiểm tra ghi nhật ký chẩn đoán lỗi.
18. `DToolsLogger_LogException_ShouldRecordInnerExceptionAndStackTrace`: Kiểm tra bắt ngoại lệ chi tiết.
19. `Kiểm tra mạch khởi động trực tiếp DOL & Chân IEC`: MCB2, Contactor MC1 (13-14), Rơle nhiệt TH1 (95-96).
20. `Kiểm tra liên động chéo Sao - Tam Giác`: Chống ngắn mạch tiếp điểm KM2 và KM3.
21. `Mô phỏng FSM chu trình Star-Delta`: Trạng thái OFF -> START -> STAR -> DEAD_TIME -> DELTA -> TRIP.
22. `Kiểm tra hình học Mặt bằng Chiếu sáng 12x6m`: Tọa độ đèn trần và cửa đi.
23. `GridSystem_SpanCoordinates_ShouldBeAccurate`: Tính toán tọa độ tim trục và nhịp liên tiếp phương X & Y.

