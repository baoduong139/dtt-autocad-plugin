# BẢNG ĐÁNH GIÁ RỦI RO & BIỆN PHÁP KIỂM SOÁT (RISK REGISTER)

DTools Pro tuân thủ quy chuẩn quản lý rủi ro kỹ thuật nhằm đảm bảo an toàn tuyệt đối cho tệp bản vẽ DWG, sự ổn định của AutoCAD và hiệu năng hệ thống trên máy tính **Dell Vostro 5620 (RAM 8 GB)**.

---

## 1. Phân Loại Mức Độ Rủi Ro
- **P0 (Critical)**: Gây crash AutoCAD, hỏng/mất dữ liệu DWG, xung đột luồng treo hệ thống.
- **P1 (High)**: Giật đơ giao diện, tràn bộ nhớ (OOM) trên máy RAM 8GB, sai lệch hình học/tỷ lệ bản vẽ.
- **P2 (Medium)**: Lỗi gõ phím tiếng Việt, xung đột cấu hình bên thứ ba, log lỗi thiếu thông tin.

---

## 2. Bảng Ma Trận Rủi Ro Chi Tiết

| Mã | Mức Độ | Nguy Cơ Kỹ Thuật (Risk Event) | Nguyên Nhân Cốt Lõi | Hậu Quả | Biện Pháp Kiểm Soát & Khắc Phục (Mitigation) | Trạng Thái |
| :--- | :---: | :--- | :--- | :--- | :--- | :---: |
| **R-01** | **P0** | Crash AutoCAD (`eLockViolation` / `AccessViolation`) khi click chèn đối tượng từ Palette. | Gọi `ed.GetPoint()` hoặc sửa DB trực tiếp trong UI event handler mà không có DocumentLock / CommandContext. | AutoCAD văng đột ngột, mất dữ liệu chưa lưu của người dùng. | Tách biệt 100%: Sự kiện WPF chỉ lưu context và gửi lệnh CAD qua `SendStringToExecute`. Mọi thao tác ghi DB bọc trong `using (doc.LockDocument())` và `Transaction`. | **ĐÃ GIẢI QUYẾT** |
| **R-02** | **P0** | Hỏng dữ liệu bản vẽ khi lệnh thực thi bị lỗi giữa chừng (`Transaction Abort`). | Không có cơ chế rollback hoặc Undo Group bị đứt đoạn. | Đối tượng vẽ dở dang nằm rải rác trên ModelSpace, phá hỏng bản vẽ kỹ thuật. | Bọc toàn bộ quy trình vẽ trong `using (Transaction tr = db.TransactionManager.StartTransaction())`. Chỉ `tr.Commit()` khi hoàn tất 100%; nếu gặp Exception tự động rollback. | **ĐÃ GIẢI QUYẾT** |
| **R-03** | **P1** | Tràn bộ nhớ (OutOfMemory) trên máy Dell Vostro 5620 (RAM 8 GB). | AutoCAD bản thân chiếm 3.5–5 GB RAM. Nạp sẵn mô hình 3D, gọi Ollama AI ngầm, cache ảnh không giới hạn. | Hệ thống lag giật, Windows đưa vào swap disk khiến máy đơ hoàn toàn. | **Chế độ Lite Mode mặc định**: Tắt toàn bộ AI & 3D khi khởi động; chỉ nạp khi người dùng bấm mở; áp dụng `BoundedLruCache` (tối đa 20 preview); giải phóng Bitmap sau khi render. | **ĐÃ THIẾT KẾ** |
| **R-04** | **P1** | Lỗi đường dẫn không tồn tại khi cài đặt sang máy người dùng khác (Dev Path Hardcoding). | Hardcode đường dẫn tuyệt đối dạng `C:\Users\Duong\...` trong mã nguồn. | Không nạp được thư viện ký hiệu `symbols_database.json` hoặc thư viện Blocks. | Thiết lập `ResourceLocator` tự động dò tìm theo Assembly Location, Bundle Contents hoặc `%APPDATA%`. Tuyệt đối không hardcode đường dẫn dev. | **ĐÃ GIẢI QUYẾT** |
| **R-05** | **P2** | Bộ gõ tiếng Việt (Unikey/EVKey) nuốt phím sinh ra lệnh lạ (`=T_FAST_PLACE`, `=T_DOOR`). | Gửi chuỗi `\x03\x03` qua `SendStringToExecute` bị bộ gõ unikey hiểu nhầm là tổ hợp phím gõ tắt. | Báo lỗi `Unknown command "=T_..."` trên thanh command AutoCAD. | Bỏ hoàn toàn tiền tố `\x03\x03`. Đăng ký alias chuyển tiếp trong `MIGRATION_MAP.csv` và kiểm soát chuỗi lệnh sạch `\n`. | **ĐÃ GIẢI QUYẾT** |
| **R-06** | **P2** | Xung đột cấu hình CUIX hoặc SupportPath của AutoCAD và ứng dụng bên thứ ba (YQArch). | Plugin tự ý chèn đường dẫn vào SupportPath hoặc ghi đè file menu CUI của người dùng. | Làm mất thanh công cụ quen thuộc của người dùng, vi phạm bảo mật Autodesk. | Tách YQArch thành Optional Adapter độc lập; không tự động sửa `ACAD.CUIX` hoặc SupportPath mà không có sự xác nhận của người dùng. | **ĐÃ KIỂM SOÁT** |
| **R-07** | **P1** | Sai lệch tỷ lệ hình học khi bản vẽ dùng đơn vị Inch hoặc hệ trục tọa độ người dùng (UCS). | Mặc định mọi bản vẽ đều ở hệ WCS và đơn vị Millimeter (INSUNITS = 4). | Ký hiệu hoặc cửa/tường bị phóng to 25.4 lần hoặc chèn nghiêng lệch góc. | Kiểm tra `INSUNITS`, ma trận biến đổi `ed.CurrentUserCoordinateSystem` (UCS sang WCS) trước khi AppendEntity vào BlockTableRecord. | **ĐÃ KIỂM SOÁT** |
| **R-08** | **P2** | Lỗi hiển thị tiếng Việt bị vỡ font (`?`) trên các bản vẽ CAD không có font Unicode. | Sử dụng font SHX cũ (vn_vni, vntime) hoặc text style mặc định không hỗ trợ UTF-8. | Bản vẽ kỹ thuật bị lỗi chữ tiếng Việt, không thể in ấn hoặc nộp hồ sơ. | Tự động tạo TextStyle chuẩn `DTT_UNICODE` sử dụng font `Arial` hoặc `Segoe UI` hỗ trợ 100% tiếng Việt có dấu. | **ĐÃ KIỂM SOÁT** |

---

## 3. Quy Trình Ứng Phó Sự Cố (Incident Response)
1. **Ghi Log Cục Bộ**: Mọi lỗi không xử lý được (Unhandled Exception) được ghi vào `%LOCALAPPDATA%\DToolsPro\Logs\dtools_error.log` kèm Correlation ID, thời gian và Call Stack đầy đủ.
2. **Thông Báo Thân Thiện**: Người dùng nhận hộp thoại thông báo bằng tiếng Việt rõ ràng, giải thích nguyên nhân và hướng dẫn thao tác tiếp theo, không hiện mã lỗi khó hiểu hay làm sập AutoCAD.
3. **An Toàn Bản Vẽ**: Trước bất kỳ thao tác dọn dẹp quy mô lớn (`DT_CLEAN`), hệ thống luôn yêu cầu xác nhận và tạo bản sao lưu snapshot tự động.
