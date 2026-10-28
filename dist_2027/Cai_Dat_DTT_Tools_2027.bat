@echo off
chcp 65001 >nul
title Cài Đặt DTT Tools - AutoCAD Electrical 2027
echo ======================================================================
echo ⚡ CÀI ĐẶT DTT TOOLS CHO AUTOCAD ELECTRICAL 2027 (.NET 8 X64)
echo ======================================================================
echo.
echo Đang tự động nạp DTT_Tools_2027.bundle vào Autodesk ApplicationPlugins...

set "DEST_ROOT=%APPDATA%\Autodesk\ApplicationPlugins"
set "TARGET_DIR=%DEST_ROOT%\DTT_Tools_2027.bundle"

if not exist "%DEST_ROOT%" (
    mkdir "%DEST_ROOT%"
)

if exist "%~dp0DTT_Tools_2027.bundle" (
    xcopy /E /I /Y "%~dp0DTT_Tools_2027.bundle" "%TARGET_DIR%" >nul
) else if exist "%~dp0Contents" (
    xcopy /E /I /Y "%~dp0" "%TARGET_DIR%" >nul
) else (
    echo [LỖI] Không tìm thấy thư mục DTT_Tools_2027.bundle nguồn!
    pause
    exit /b 1
)

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ======================================================================
    echo [THÀNH CÔNG 100%] ĐÃ CÀI ĐẶT XONG DTT TOOLS CHO AUTOCAD 2027!
    echo ======================================================================
    echo.
    echo 👉 HƯỚNG DẪN SỬ DỤNG TRÊN MÁY BẠN:
    echo 1. Khởi động AutoCAD Electrical 2027 lên.
    echo 2. Gõ lệnh: DTT (hoặc DTT_MENU) rồi nhấn Enter.
    echo 3. Toàn bộ Menu tổng hợp và Bảng công cụ sẽ bật lên ngay lập tức!
    echo.
    echo (Lưu ý: Máy bạn KHÔNG cần cài Python hay bất kỳ phần mềm phụ nào khác!)
    echo.
) else (
    echo.
    echo [LỖI] Quá trình cài đặt gặp sự cố. Vui lòng thử Chạy bằng quyền Run as Administrator!
)

pause
