namespace DTools.Core
{
    /// <summary>
    /// Cấu hình hệ thống DTools Pro tối ưu hóa cho môi trường thực tế
    /// </summary>
    public static class DToolsConfiguration
    {
        public const string Version = "2026.2.0";
        public const string ProductName = "DTools Pro";
        public const string CompanyName = "DTT CAD Software Corporation";

        /// <summary>
        /// Chế độ Lite Mode (Tối ưu cho laptop RAM 8 GB như Dell Vostro 5620)
        /// Mặc định: BẬT (True) để tránh quá tải bộ nhớ và giật lag hệ thống
        /// </summary>
        public static bool IsLiteMode { get; set; } = true;

        /// <summary>
        /// Kích hoạt dựng hình 3D Solid Mesh mặc định (Mặc định: TẮT để tiết kiệm RAM)
        /// </summary>
        public static bool Enable3DByDefault { get; set; } = false;

        /// <summary>
        /// Kích hoạt trí tuệ nhân tạo Ollama Vision mặc định (Mặc định: TẮT, chỉ bật khi người dùng yêu cầu)
        /// </summary>
        public static bool EnableAiVisionByDefault { get; set; } = false;

        /// <summary>
        /// Dung lượng tối đa của bộ nhớ đệm preview ảnh (20 mục)
        /// </summary>
        public static int MaxPreviewCacheItems { get; set; } = 20;

        /// <summary>
        /// Thời gian trễ tìm kiếm (Debounce milliseconds) tránh vẽ lại UI liên tục
        /// </summary>
        public static int SearchDebounceMs { get; set; } = 200;

        /// <summary>
        /// Chủ đề giao diện mặc định (Neutral, Dark, Light)
        /// </summary>
        public static string Theme { get; set; } = "Neutral";

        /// <summary>
        /// Chế độ Safe Startup: Ngăn chặn toàn bộ thao tác nặng (WPF, I/O, LISP) khi AutoCAD khởi động
        /// </summary>
        public static bool SafeStartupEnabled { get; set; } = true;

        /// <summary>
        /// Trạng thái khởi tạo Palette: Chỉ khởi tạo khi người dùng gọi lệnh DTT / DTOOLS
        /// </summary>
        public static bool IsPaletteInitialized { get; set; } = false;

        /// <summary>
        /// Trạng thái nạp YQArch Menu: Chỉ nạp khi người dùng yêu cầu hoặc chạy lệnh DTT_LOAD_YQ
        /// </summary>
        public static bool IsYqMenuLoaded { get; set; } = false;
    }
}
