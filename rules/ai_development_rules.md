# BỘ QUY TẮC AI AUTONOMOUS DEVELOPMENT - DTT TOOLS PRO

Tài liệu quy định các nguyên tắc bắt buộc cho AI Agent khi thực hiện nghiên cứu, lập trình, kiểm thử và phát hành mã nguồn dự án DTT CAD MASTER / DTools Pro.

---

## 1. VÒNG LẶP PHÁT TRIỂN HẰNG NGÀY BẮT BUỘC (DAILY CYCLE)
Mỗi phiên làm việc (kích hoạt lúc 08:00 hằng ngày) phải tuân thủ nghiêm ngặt 8 bước:
1. **PHÂN TÍCH (ANALYZE)**: Đọc source code, commit history, issue tồn đọng và báo cáo ngày trước đó trong `docs/ai-development/`.
2. **ĐỀ XUẤT (PROPOSE)**: Đưa ra 3 - 5 cải tiến thiết thực, đánh giá lợi ích thực tế, độ ưu tiên, độ phức tạp và rủi ro.
3. **LỰA CHỌN (SELECT)**: Chọn đúng 1 hạng mục có phạm vi cụ thể, khả thi và có thể kiểm thử tự động trong ngày.
4. **LẬP TRÌNH (CODE)**: Viết mã nguồn sạch, tái sử dụng API chính thức của AutoCAD, bảo tồn các hàm hiện có.
5. **KIỂM THỬ (TEST)**: Viết/chạy unit test, integration test; loại bỏ 100% lỗi biên dịch C#/XAML; không giả mạo kết quả PASS nếu chưa test.
6. **BUILD PHÁT HÀNH (BUILD)**: Biên dịch Release Any CPU qua MSBuild; kiểm tra tính toàn vẹn của các file DLL và EXE.
7. **CẬP NHẬT GITHUB (PUSH)**: Commit rõ ràng theo chuẩn Conventional Commits, push lên branch `main` của repository GitHub.
8. **BÁO CÁO (REPORT)**: Tạo báo cáo tiếng Việt đầy đủ theo cấu trúc chuẩn A - B - C - D - E.

---

## 2. NGUYÊN TẮC AN TOÀN AUTOCAD & BỘ NHỚ RAM 8GB
- **Khóa tài liệu (DocumentLock)**: Bắt buộc dùng `using (DocumentLock docLock = doc.LockDocument())` khi can thiệp vào bản vẽ từ luồng WPF/UI hoặc modeless palette.
- **Giao dịch bản vẽ (Transaction)**: Mọi thao tác đọc/ghi Database AutoCAD phải bọc trong `using (Transaction tr = db.TransactionManager.StartTransaction())` và gọi `tr.Commit()` khi thành công.
- **Giải phóng tài nguyên (Memory Safety)**:
  - Tất cả đối tượng `Database` tạm phải bọc trong `using (Database extDb = new Database(...))`.
  - Toàn bộ thumbnail `BitmapImage` phải gọi `.Freeze()` trước khi lưu vào cache tránh rò rỉ GDI Handle.
- **Quy tắc chèn Ký hiệu Điện**: Mặc định chỉ chèn đúng 1 `BlockReference` tại 1 điểm click rồi dừng. Tuyệt đối không để vòng lặp `while(true)` vô tận gây phiền toái cho người dùng.

---

## 3. NGUYÊN TẮC KIỂM THỬ & MINH BẠCH
- **Không bao giờ phát hành khi có test thất bại**.
- Nếu môi trường chạy kiểm thử không mở được giao diện AutoCAD thực tế, bài kiểm thử tương tác màn hình phải ghi nhận rõ ràng là **NOT TESTED (Headless)**.
- Mọi tính toán hình học, điện, kết cấu, bóc tách dự toán đều phải có bài Unit Test kiểm tra biên trong `tests/DTools.Tests/`.
