# DTT TOOLS - DANH MỤC VẤN ĐỀ ĐÃ BIẾT & GIỚI HẠN KỸ THUẬT (KNOWN ISSUES)

Tài liệu ghi nhận các hiện tượng kỹ thuật, ràng buộc phần cứng và giải pháp phòng ngừa trong môi trường AutoCAD.

---

## 1. PHẦN CỨNG & BỘ NHỚ RAM 8GB (LAPTOP DELL VOSTRO 5620)
- **Hiện tượng**: Khi nạp nhiều bản vẽ DWG lớn hoặc liên tục mở các hộp thoại WPF có hình ảnh thumbnail, bộ nhớ RAM có thể bị chiếm dụng bởi đối tượng GDI Handle chưa được giải phóng.
- **Giải pháp**:
  - Mọi hàm đọc thumbnail DWG từ file ngoại vi (`CadUtils.GetDwgThumbnail`) đều sử dụng cache `_thumbnailCache` với phương thức `bi.Freeze()`.
  - Toàn bộ thao tác với `Database` phụ đều bọc chặt chẽ trong khối `using (Database extDb = new Database(false, true))` để tự động giải phóng Unmanaged Memory ngay khi kết thúc.

---

## 2. KHÓA TÀI LIỆU AUTOCAD (DOCUMENT LOCKING)
- **Hiện tượng**: Khi gọi lệnh vẽ hoặc chèn Block từ giao diện WPF/Palette không đồng bộ, nếu không khóa tài liệu thì AutoCAD sẽ ném ngoại lệ `Autodesk.AutoCAD.Runtime.Exception: eLockViolation`.
- **Giải pháp**:
  - Luôn kiểm tra `doc != null` trước khi thao tác.
  - Luôn bao bọc các thao tác chỉnh sửa Database CAD trong khối:
    ```csharp
    using (DocumentLock docLock = doc.LockDocument())
    {
        // Thao tác chỉnh sửa Block / Entity / Layer
    }
    ```

---

## 3. TRÌNH TỰ ĐỘNG CẬP NHẬT (UPDATER) VÀ FILE ĐANG KHÓA
- **Hiện tượng**: Khi AutoCAD đang mở, file `DTT_AutoCAD_Plugin.dll` bị Windows khóa quyền ghi (`Access Denied`), không thể cập nhật trực tiếp.
- **Giải pháp**:
  - `DTT_Updater.exe` là tiến trình chạy độc lập bên ngoài.
  - Khi phát hiện AutoCAD đang chạy, Updater nhắc người dùng đóng AutoCAD hoặc sao lưu file tạm và kích hoạt ghi đè khi AutoCAD kết thúc.

---

## 4. ĐƠN VỊ BẢN VẼ VÀ TỶ LỆ SCALE SYMBOL (INSUNITS)
- **Hiện tượng**: Bản vẽ kiến trúc có thể dùng đơn vị Millimeter (4), Meter (6) hoặc Không xác định (0). Nếu chèn symbol với scale cố định 1.0 thì ký hiệu có thể quá nhỏ hoặc quá to.
- **Giải pháp**:
  - Hàm `CadUtils.CalculateAutoSymbolScale` tự động đọc biến hệ thống `INSUNITS`, `DIMSCALE` và tỷ lệ khung nhìn `CANOSCALE` để điều chỉnh hệ số scale hiệu dụng tự động.

