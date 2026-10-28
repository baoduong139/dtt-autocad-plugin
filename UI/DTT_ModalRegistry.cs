using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTT_AutoCAD_Plugin.Common;

namespace DTT_AutoCAD_Plugin.UI
{
    public enum PreviewModelType
    {
        Door,
        Column,
        Elevation,
        Wall,
        Window,
        Cabinet,
        Pipe,
        Electrical,
        Maintenance,
        YqQuick,
        General
    }

    /// <summary>
    /// ĐỊNH NGHĨA MỘT TÙY CHỌN TRONG MENU TRUNG TÂM
    /// </summary>
    public class ModalOptionItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public string Badge { get; set; }
        public string BadgeColor { get; set; } = "#0284C7";
        public string CommandTag { get; set; }
        public string CommandArgs { get; set; } = "";
        public PreviewModelType PreviewType { get; set; } = PreviewModelType.General;

        // Tham số kích thước & mô hình
        public double ParamWidth { get; set; } = 900;
        public double ParamHeight { get; set; } = 2200;
        public double ParamLength { get; set; } = 1200;
        public double ParamExtra { get; set; } = 110;
        public bool Flip { get; set; } = false;
        public string VisibilityState { get; set; } = "Mở 45°";

        // Danh sách kích thước tùy chọn nhanh
        public List<string> QuickSizes { get; set; } = new List<string>();
        public string SelectedSize { get; set; } = "900";
        public string Description { get; set; } = "";
        public Action CustomAction { get; set; }

        public override string ToString() => Title;
    }

    /// <summary>
    /// ĐỊNH NGHĨA CẤU HÌNH NHÓM CHỨC NĂNG CỦA MODAL TRUNG TÂM
    /// </summary>
    public class ModalCategoryConfig
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Icon { get; set; } = "⚡";
        public string ThemeColor { get; set; } = "#0284C7";
        public List<ModalOptionItem> Options { get; set; } = new List<ModalOptionItem>();
    }

    /// <summary>
    /// HỆ THỐNG ĐĂNG KÝ DANH MỤC MODAL TRUNG TÂM (GLOBAL MODAL REGISTRY)
    /// Quản lý tập trung toàn bộ cấu hình hiển thị, dữ liệu 2D/3D và xạ lệnh CAD
    /// </summary>
    public static class DTT_ModalRegistry
    {
        private static readonly Dictionary<string, ModalCategoryConfig> _registry = new Dictionary<string, ModalCategoryConfig>(StringComparer.OrdinalIgnoreCase);

        static DTT_ModalRegistry()
        {
            RegisterDynamicBlocks();
            RegisterDoors();
            RegisterColumns();
            RegisterElevations();
            RegisterWalls();
            RegisterWindows();
            RegisterCabinets();
            RegisterElectrical();
            RegisterMep();
            RegisterMaintenance();
            RegisterYqQuick();
        }

        #region 1. Khởi tạo các nhóm chức năng chuẩn

        private static void RegisterDynamicBlocks()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "DYNAMIC_BLOCKS",
                Title = "THƯ VIỆN DYNAMIC BLOCKS NỘI BỘ DTOOLS",
                Subtitle = "Toàn bộ cấu kiện động chuẩn AutoCAD Metric & Imperial, tự động nhận diện file DWG",
                Icon = "📦",
                ThemeColor = "#0284C7"
            };

            // 1. Cửa Đi Động
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_DOOR",
                Title = "Cửa Đi Động (Architectural_Doors)",
                Subtitle = "Dynamic Block • Chỉnh bề rộng 600-1200mm • Lật hướng Hinge/Swing • 5 góc mở",
                Category = "Kiến Trúc",
                Icon = "🚪",
                Badge = "Dynamic Pro",
                BadgeColor = "#0284C7",
                CommandTag = "DT_DOOR",
                CommandArgs = "Architectural_Doors",
                PreviewType = PreviewModelType.Door,
                ParamWidth = 900,
                ParamHeight = 2200,
                QuickSizes = new List<string> { "750", "800", "900", "1000", "1200" },
                SelectedSize = "900",
                Description = "File: Architectural_Doors.dwg. Tự động liên kết bản lề, góc mở 30°, 45°, 90° và bề dày tường."
            });

            // 2. Cột Kiến Trúc Động
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_COLUMN",
                Title = "Cột Kiến Trúc Động (Architectural_Columns)",
                Subtitle = "Dynamic Block • Thay đổi tiết diện WxH • Tham số bản bụng Wt, cánh Ft linh hoạt",
                Category = "Kết Cấu",
                Icon = "🏛️",
                Badge = "Dynamic Pro",
                BadgeColor = "#7C3AED",
                CommandTag = "DT_COLUMN",
                CommandArgs = "Architectural_Columns",
                PreviewType = PreviewModelType.Column,
                ParamWidth = 220,
                ParamHeight = 3000,
                QuickSizes = new List<string> { "200", "220", "300", "400" },
                SelectedSize = "220",
                Description = "File: Architectural_Columns.dwg. Cột vuông/chữ nhật/I-Beam chuẩn tỉ lệ kiến trúc và kết cấu."
            });

            // 3. Ký Hiệu Cao Độ
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ELEVATION",
                Title = "Ký Hiệu Cao Độ (Annotations)",
                Subtitle = "Dynamic Block • Chỉnh chiều dài đường dóng Ref Line • Mũi tên đảo chiều • Thuộc tính cốt",
                Category = "Ký Hiệu",
                Icon = "📐",
                Badge = "Dynamic Pro",
                BadgeColor = "#B45309",
                CommandTag = "DT_ELEVATION",
                CommandArgs = "Annotations",
                PreviewType = PreviewModelType.Elevation,
                ParamWidth = 300,
                ParamHeight = 1200,
                QuickSizes = new List<string> { "+0.000", "+3.600", "+7.200", "-0.450" },
                SelectedSize = "+0.000",
                Description = "File: Annotations.dwg. Ký hiệu mốc cao độ hoàn thiện, mốc kết cấu chuẩn đồ án kiến trúc."
            });

            // 4. Kiến Trúc Chuẩn Metric
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ARCH_METRIC",
                Title = "Kiến Trúc Chuẩn Metric (Architectural - Metric)",
                Subtitle = "Dynamic Block Thư viện • Cửa đi, cửa sổ, vách ngăn chuẩn hệ mm",
                Category = "Kiến Trúc Metric",
                Icon = "🏛️",
                Badge = "AutoCAD Metric",
                BadgeColor = "#0284C7",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Architectural - Metric",
                PreviewType = PreviewModelType.Door,
                ParamWidth = 900,
                ParamHeight = 2200,
                QuickSizes = new List<string> { "800", "900", "1000", "1200" },
                SelectedSize = "900",
                Description = "File: Architectural - Metric.dwg. Cấu kiện kiến trúc metric chuẩn AutoCAD."
            });

            // 5. Kết Cấu Chuẩn Metric
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_STRUCT_METRIC",
                Title = "Kết Cấu Chuẩn Metric (Structural - Metric)",
                Subtitle = "Dynamic Block Thư viện • Dầm, Cột, Thép hình I/H, Bản đế chuẩn metric",
                Category = "Kết Cấu Metric",
                Icon = "🏗️",
                Badge = "AutoCAD Metric",
                BadgeColor = "#7C3AED",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Structural - Metric",
                PreviewType = PreviewModelType.Column,
                ParamWidth = 300,
                ParamHeight = 3500,
                QuickSizes = new List<string> { "200", "250", "300", "400" },
                SelectedSize = "300",
                Description = "File: Structural - Metric.dwg. Cấu kiện kết cấu thép và bê tông cốt thép chuẩn ISO/TCVN."
            });

            // 6. Khí Cụ & Thiết Bị Điện Metric
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ELEC_METRIC",
                Title = "Khí Cụ & Thiết Bị Điện (Electrical - Metric)",
                Subtitle = "Dynamic Block Thư viện • Ổ cắm, công tắc, đèn, tủ điện chuẩn bản vẽ M&E",
                Category = "Cơ Điện MEP",
                Icon = "⚡",
                Badge = "AutoCAD Metric",
                BadgeColor = "#F59E0B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Electrical - Metric",
                PreviewType = PreviewModelType.Electrical,
                Description = "File: Electrical - Metric.dwg. Ký hiệu điện tử và thiết bị điện dân dụng & công nghiệp."
            });

            // 7. Cơ Khí & HVAC Metric
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_MECH_METRIC",
                Title = "Cơ Khí & HVAC (Mechanical - Metric)",
                Subtitle = "Dynamic Block Thư viện • Ống gió, van, cút nối, bu-lông đai ốc chuẩn mm",
                Category = "Cơ Điện MEP",
                Icon = "🔩",
                Badge = "AutoCAD Metric",
                BadgeColor = "#059669",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Mechanical - Metric",
                PreviewType = PreviewModelType.Pipe,
                Description = "File: Mechanical - Metric.dwg. Phụ kiện đường ống và thiết bị cơ khí điều hòa thông gió."
            });

            // 8. Hạ Tầng Giao Thông & San Nền Metric
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_CIVIL_METRIC",
                Title = "Hạ Tầng Giao Thông (Civil - Metric)",
                Subtitle = "Dynamic Block Thư viện • Taluy, trắc dọc, hố ga, rãnh thoát nước, biển báo",
                Category = "Hạ Tầng Civil",
                Icon = "🛣️",
                Badge = "AutoCAD Metric",
                BadgeColor = "#0EA5E9",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Civil - Metric",
                PreviewType = PreviewModelType.General,
                Description = "File: Civil - Metric.dwg. Công trình hạ tầng kỹ thuật đô thị và đường giao thông."
            });

            // 9. Ký Hiệu Chú Thích & Dim Metric
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ANNO_METRIC",
                Title = "Ký Hiệu & Chú Thích (Annotation - Metric)",
                Subtitle = "Dynamic Block Thư viện • Mũi tên hướng bắc, ký hiệu mặt cắt, cao độ kiến trúc",
                Category = "Ký Hiệu",
                Icon = "📐",
                Badge = "AutoCAD Metric",
                BadgeColor = "#D97706",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Annotation - Metric",
                PreviewType = PreviewModelType.Elevation,
                Description = "File: Annotation - Metric.dwg. Bộ ký hiệu bản vẽ thiết kế chuẩn đồ án kiến trúc."
            });

            // 10. Chú Thích Multileader Động
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_MLEADER",
                Title = "Ký Hiệu Multileader Động (Multileader Tools)",
                Subtitle = "Dynamic Block Thư viện • Đường dẫn ghi chú thông minh, bong bóng chi tiết",
                Category = "Ký Hiệu",
                Icon = "🎯",
                Badge = "AutoCAD Tools",
                BadgeColor = "#4338CA",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Multileader Tools",
                PreviewType = PreviewModelType.General,
                Description = "File: Multileader Tools.dwg. Công cụ ghi chú chi tiết kết cấu và kiến trúc chuyên nghiệp."
            });

            // 11. Các Block hệ Imperial (Tùy chọn quốc tế)
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ARCH_IMP",
                Title = "Kiến Trúc Hệ Imperial (Architectural - Imperial)",
                Subtitle = "Dynamic Block • Chuẩn hệ đo Inch / Feet cho đồ án tiêu chuẩn Mỹ/Quốc tế",
                Category = "Hệ Imperial",
                Icon = "🏛️",
                Badge = "Imperial",
                BadgeColor = "#64748B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Architectural - Imperial",
                PreviewType = PreviewModelType.Door,
                Description = "File: Architectural - Imperial.dwg."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_STRUCT_IMP",
                Title = "Kết Cấu Hệ Imperial (Structural - Imperial)",
                Subtitle = "Dynamic Block • Cấu kiện thép hình và BTCT chuẩn hệ Imperial (AISC/ASTM)",
                Category = "Hệ Imperial",
                Icon = "🏗️",
                Badge = "Imperial",
                BadgeColor = "#64748B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Structural - Imperial",
                PreviewType = PreviewModelType.Column,
                Description = "File: Structural - Imperial.dwg."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ELEC_IMP",
                Title = "Điện Hệ Imperial (Electrical - Imperial)",
                Subtitle = "Dynamic Block • Ký hiệu điện chuẩn NEC / NEMA hệ Imperial",
                Category = "Hệ Imperial",
                Icon = "⚡",
                Badge = "Imperial",
                BadgeColor = "#64748B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Electrical - Imperial",
                PreviewType = PreviewModelType.Electrical,
                Description = "File: Electrical - Imperial.dwg."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_MECH_IMP",
                Title = "Cơ Khí Hệ Imperial (Mechanical - Imperial)",
                Subtitle = "Dynamic Block • Thiết bị cơ khí và ống gió chuẩn ASME/ASHRAE",
                Category = "Hệ Imperial",
                Icon = "🔩",
                Badge = "Imperial",
                BadgeColor = "#64748B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Mechanical - Imperial",
                PreviewType = PreviewModelType.Pipe,
                Description = "File: Mechanical - Imperial.dwg."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_CIVIL_IMP",
                Title = "Hạ Tầng Hệ Imperial (Civil - Imperial)",
                Subtitle = "Dynamic Block • Hạ tầng và giao thông chuẩn AASHTO hệ Imperial",
                Category = "Hệ Imperial",
                Icon = "🛣️",
                Badge = "Imperial",
                BadgeColor = "#64748B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Civil - Imperial",
                PreviewType = PreviewModelType.General,
                Description = "File: Civil - Imperial.dwg."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_ANNO_IMP",
                Title = "Chú Thích Hệ Imperial (Annotation - Imperial)",
                Subtitle = "Dynamic Block • Ký hiệu ghi chú cao độ và trục hệ Inch / Feet",
                Category = "Hệ Imperial",
                Icon = "📐",
                Badge = "Imperial",
                BadgeColor = "#64748B",
                CommandTag = "DT_INSERTBLOCK",
                CommandArgs = "Annotation - Imperial",
                PreviewType = PreviewModelType.Elevation,
                Description = "File: Annotation - Imperial.dwg."
            });

            // 12. Tiện ích quản lý
            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_PROPS",
                Title = "Xem & Đổi Thuộc Tính Động (DT_PROPS)",
                Subtitle = "Chọn 1 block trên bản vẽ để tra cứu và thay đổi nhanh các tham số Dynamic",
                Category = "Công Cụ",
                Icon = "⚙️",
                Badge = "Tiện Ích",
                BadgeColor = "#059669",
                CommandTag = "DT_PROPS",
                PreviewType = PreviewModelType.General,
                Description = "Quét thông tin DynamicBlockReferencePropertyCollection trực tiếp trên màn hình CAD."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DYN_OPEN",
                Title = "Mở Thư Mục Kho Blocks (DT_OPENBLOCKS)",
                Subtitle = "Mở nhanh thư mục Dtools/Blocks/ trong Windows Explorer để thêm/xóa file DWG mẫu",
                Category = "Quản Lý",
                Icon = "📂",
                Badge = "Thư Viện",
                BadgeColor = "#475569",
                CommandTag = "DT_OPENBLOCKS",
                PreviewType = PreviewModelType.General,
                Description = "Thư mục lưu trữ tài nguyên block cục bộ đi kèm plugin."
            });

            // Tự động quét thêm các file DWG mới từ thư mục Blocks
            RefreshDynamicBlocksFromFolder(cat);

            _registry["DYNAMIC_BLOCKS"] = cat;
            _registry["DT_INSERTBLOCK"] = cat;
            _registry["DTT_INSERTBLOCK"] = cat;
            _registry["DT_BLOCK"] = cat;
            _registry["DTT_BLOCK"] = cat;
            _registry["DT_MODAL"] = cat;
            _registry["DTT_MODAL"] = cat;
            _registry["DT_CENTER_MODAL"] = cat;
            _registry["DT_POPUP"] = cat;
            _registry["DT_MENU3D"] = cat;
        }

        /// <summary>
        /// Tự động quét toàn bộ file DWG trong thư mục Dtools/Blocks/ và nạp vào danh sách nếu chưa có
        /// </summary>
        public static void RefreshDynamicBlocksFromFolder(ModalCategoryConfig cat)
        {
            try
            {
                string dir = DtoolsBlockService.GetBlocksDirectory();
                if (Directory.Exists(dir))
                {
                    string[] dwgs = Directory.GetFiles(dir, "*.dwg", SearchOption.TopDirectoryOnly);
                    foreach (string file in dwgs)
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        bool exists = cat.Options.Any(o => 
                            string.Equals(o.CommandArgs, name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(o.Id, name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(o.Title, name, StringComparison.OrdinalIgnoreCase) ||
                            (o.Title != null && o.Title.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));

                        if (!exists)
                        {
                            cat.Options.Add(new ModalOptionItem
                            {
                                Id = "BLK_" + name.Replace(" ", "_"),
                                Title = name,
                                Subtitle = $"File: {name}.dwg • Block thư viện tự động nạp",
                                Category = "Thư Viện Tự Động",
                                Icon = "📦",
                                Badge = "DWG Mới",
                                BadgeColor = "#0284C7",
                                CommandTag = "DT_INSERTBLOCK",
                                CommandArgs = name,
                                PreviewType = PreviewModelType.General,
                                Description = $"Đường dẫn: {file}. Click để chèn trực tiếp vào bản vẽ hiện hành."
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private static void RegisterDoors()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "DOORS",
                Title = "THƯ VIỆN CỬA ĐI KIẾN TRÚC & DYNAMIC DOORS",
                Subtitle = "Chọn kiểu cửa, tùy biến kích thước lọt lòng, hướng mở và xem trước mô hình 3D",
                Icon = "🚪",
                ThemeColor = "#0284C7"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DOOR_900",
                Title = "Cửa Đi 1 Cánh 900x2200 (Thông Phòng)",
                Subtitle = "Cửa đi tiêu chuẩn phòng ngủ, phòng làm việc • Rộng 900mm",
                Category = "Cửa Đi 1 Cánh",
                Icon = "🚪",
                Badge = "Tiêu Chuẩn",
                BadgeColor = "#0284C7",
                CommandTag = "DT_DOOR",
                CommandArgs = "900",
                PreviewType = PreviewModelType.Door,
                ParamWidth = 900,
                ParamHeight = 2200,
                QuickSizes = new List<string> { "800", "850", "900", "950" },
                SelectedSize = "900",
                Description = "Kích thước phủ bì: 1000x2250mm, lọt gió phong thủy 810x2140mm."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DOOR_800",
                Title = "Cửa Đi 1 Cánh 800x2200 (Vệ Sinh / Phụ)",
                Subtitle = "Cửa đi khu vệ sinh, kho, ban công nhỏ • Rộng 800mm",
                Category = "Cửa Đi 1 Cánh",
                Icon = "🚪",
                Badge = "Khu Phụ",
                BadgeColor = "#0EA5E9",
                CommandTag = "DT_DOOR",
                CommandArgs = "800",
                PreviewType = PreviewModelType.Door,
                ParamWidth = 800,
                ParamHeight = 2200,
                QuickSizes = new List<string> { "700", "750", "800" },
                SelectedSize = "800",
                Description = "Tiết kiệm diện tích mở cánh, phù hợp vách tường 110mm."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DOOR_1000",
                Title = "Cửa Đi 1 Cánh 1000x2200 (Cửa Chính 1 Cánh)",
                Subtitle = "Cửa đại sảnh căn hộ, cửa văn phòng rộng rãi • Rộng 1000mm",
                Category = "Cửa Đi 1 Cánh",
                Icon = "🚪",
                Badge = "Cửa Chính",
                BadgeColor = "#059669",
                CommandTag = "DT_DOOR",
                CommandArgs = "1000",
                PreviewType = PreviewModelType.Door,
                ParamWidth = 1000,
                ParamHeight = 2200,
                QuickSizes = new List<string> { "1000", "1100", "1200" },
                SelectedSize = "1000",
                Description = "Khung bao gỗ/nhôm xingfa hệ 55, cánh mở 90 độ thoáng đãng."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "DOOR_DOUBLE",
                Title = "Cửa Đi 2 Cánh Mở 1400x2200",
                Subtitle = "Cửa chính nhà phố, cửa ra ban công lớn • 2 cánh đối xứng",
                Category = "Cửa Đi 2 Cánh",
                Icon = "🚪",
                Badge = "2 Cánh",
                BadgeColor = "#7C3AED",
                CommandTag = "AD",
                PreviewType = PreviewModelType.Door,
                ParamWidth = 1400,
                ParamHeight = 2200,
                QuickSizes = new List<string> { "1200", "1400", "1600" },
                SelectedSize = "1400",
                Description = "Sử dụng lệnh mở cửa kiến trúc tự động cắt tường hoàn chỉnh."
            });

            _registry["DOORS"] = cat;
        }

        private static void RegisterColumns()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "COLUMNS",
                Title = "THƯ VIỆN CỘT KIẾN TRÚC & KẾT CẤU BTCT / THÉP",
                Subtitle = "Tùy chọn tiết diện cột vuông, chữ nhật, cột tròn, thép hình I/H với mô phỏng 3D",
                Icon = "🏛️",
                ThemeColor = "#7C3AED"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "COL_220",
                Title = "Cột Vuông 220x220 (Cột Tường Gạch)",
                Subtitle = "Kích thước âm tường gạch 220mm • Phổ biến nhất nhà phố Việt Nam",
                Category = "Cột BTCT",
                Icon = "🏛️",
                Badge = "Nhà Phố",
                BadgeColor = "#7C3AED",
                CommandTag = "DT_COLUMN",
                CommandArgs = "220",
                PreviewType = PreviewModelType.Column,
                ParamWidth = 220,
                ParamHeight = 3300,
                QuickSizes = new List<string> { "200", "220", "250" },
                SelectedSize = "220",
                Description = "Tiết diện 220x220mm liền phẳng mặt tường, không bị lộ góc cạnh trong phòng."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "COL_300",
                Title = "Cột Vuông 300x300 (Chịu Lực Chính)",
                Subtitle = "Cột chịu tải lớn cho nhà 3-5 tầng, sảnh chính • 300x300mm",
                Category = "Cột BTCT",
                Icon = "🏛️",
                Badge = "Chịu Lực",
                BadgeColor = "#0284C7",
                CommandTag = "DT_COLUMN",
                CommandArgs = "300",
                PreviewType = PreviewModelType.Column,
                ParamWidth = 300,
                ParamHeight = 3600,
                QuickSizes = new List<string> { "300", "350", "400" },
                SelectedSize = "300",
                Description = "Hàm lượng thép 1.5 - 2.5%, bố trí 6-8 thanh thép chủ D18-D20."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "COL_ROUND",
                Title = "Cột Tròn D300 (Cột Tròn Tân Cổ Điển)",
                Subtitle = "Cột sảnh tròn, biệt thự sân vườn • Đường kính D300mm",
                Category = "Cột Tròn",
                Icon = "🏛️",
                Badge = "Thẩm Mỹ",
                BadgeColor = "#D97706",
                CommandTag = "DT_COLUMN",
                CommandArgs = "300",
                PreviewType = PreviewModelType.Column,
                ParamWidth = 300,
                ParamHeight = 3500,
                QuickSizes = new List<string> { "250", "300", "350", "400" },
                SelectedSize = "300",
                Description = "Có chân đế và mũ đầu cột phào chỉ gờ chỉ cổ điển thanh thoát."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "COL_STEEL_H",
                Title = "Thép Hình Kết Cấu H-Beam 200x200",
                Subtitle = "Thép hình cán nóng nhà xưởng, kết cấu thép công nghiệp",
                Category = "Thép Hình",
                Icon = "🏗️",
                Badge = "Kết Cấu Thép",
                BadgeColor = "#475569",
                CommandTag = "DTT_STRUCT_MENU",
                PreviewType = PreviewModelType.Column,
                ParamWidth = 200,
                ParamHeight = 4000,
                Description = "Mác thép SS400 / Q345B, bề dày bản cánh và bản bụng theo chuẩn JIS G3192."
            });

            _registry["COLUMNS"] = cat;
        }

        private static void RegisterElevations()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "ELEVATIONS",
                Title = "KÝ HIỆU CAO ĐỘ, TRỤC & CHI TIẾT BẢN VẼ",
                Subtitle = "Chọn mốc cao độ hoàn thiện, ký hiệu trục lưới, mặt cắt kiến trúc chuẩn hồ sơ",
                Icon = "📐",
                ThemeColor = "#B45309"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEV_000",
                Title = "Mốc Cao Độ ±0.000 (Mặt Sàn Tầng 1)",
                Subtitle = "Cốt chuẩn chuẩn mực của công trình • Cao độ sàn hoàn thiện",
                Category = "Cao Độ",
                Icon = "📐",
                Badge = "Cốt Chuẩn",
                BadgeColor = "#10B981",
                CommandTag = "DT_ELEVATION",
                CommandArgs = "±0.000",
                PreviewType = PreviewModelType.Elevation,
                ParamWidth = 250,
                ParamHeight = 1000,
                SelectedSize = "±0.000",
                Description = "Mặt sàn gạch men/gỗ hoàn thiện tầng 1 so với cốt vỉa hè đường."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEV_3600",
                Title = "Mốc Cao Độ +3.600 (Sàn Tầng 2)",
                Subtitle = "Chiều cao tầng điển hình nhà phố hiện đại • Cốt +3.600",
                Category = "Cao Độ",
                Icon = "📐",
                Badge = "Tầng 2",
                BadgeColor = "#0284C7",
                CommandTag = "DT_ELEVATION",
                CommandArgs = "+3.600",
                PreviewType = PreviewModelType.Elevation,
                ParamWidth = 250,
                ParamHeight = 1200,
                SelectedSize = "+3.600",
                Description = "Khoảng tĩnh không lọt lòng tầng 1 đạt 3.3m sau khi đóng trần thạch cao."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEV_7200",
                Title = "Mốc Cao Độ +7.200 (Sàn Tầng 3 / Mái)",
                Subtitle = "Cốt sàn tầng 3 hoặc cao độ dầm mái công trình",
                Category = "Cao Độ",
                Icon = "📐",
                Badge = "Tầng 3",
                BadgeColor = "#7C3AED",
                CommandTag = "DT_ELEVATION",
                CommandArgs = "+7.200",
                PreviewType = PreviewModelType.Elevation,
                ParamWidth = 250,
                ParamHeight = 1200,
                SelectedSize = "+7.200",
                Description = "Định vị vị trí dầm sàn và máng thu nước mái bê tông."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEV_MINUS",
                Title = "Mốc Cao Độ Âm -0.450 (Sân / Vỉa Hè)",
                Subtitle = "Cốt sân ngoài nhà thấp hơn sàn nhà 3 bậc tam cấp chuẩn phong thủy",
                Category = "Cao Độ",
                Icon = "📐",
                Badge = "Sân Vườn",
                BadgeColor = "#EF4444",
                CommandTag = "DT_ELEVATION",
                CommandArgs = "-0.450",
                PreviewType = PreviewModelType.Elevation,
                ParamWidth = 250,
                ParamHeight = 800,
                SelectedSize = "-0.450",
                Description = "Bậc thềm tam cấp 150x300mm chống hắt nước mưa và tạo thế uy nghiêm."
            });

            _registry["ELEVATIONS"] = cat;
        }

        private static void RegisterWalls()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "WALLS",
                Title = "MENU TƯỜNG XÂY & VÁCH NGĂN KIẾN TRÚC",
                Subtitle = "Vẽ tường liên tục, tự động cắt giao, chọn bề dày 220, 110, 100, 75mm",
                Icon = "🧱",
                ThemeColor = "#B45309"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "WALL_220",
                Title = "Tường Bao Che 220mm (2 Lớp Gạch)",
                Subtitle = "Tường ngoại thất, cách âm cách nhiệt tốt • Bề dày 220mm",
                Category = "Tường Xây",
                Icon = "🧱",
                Badge = "Tường Bao",
                BadgeColor = "#B45309",
                CommandTag = "DTT_WALL220",
                PreviewType = PreviewModelType.Wall,
                ParamWidth = 220,
                ParamHeight = 3300,
                ParamLength = 2000,
                Description = "Xây bằng gạch ống hoặc gạch đặc 2 lớp có giằng tường bê tông chống nứt."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "WALL_110",
                Title = "Tường Ngăn Phòng 110mm (1 Lớp Gạch)",
                Subtitle = "Tường phân chia không gian bên trong nhà • Bề dày 110mm",
                Category = "Tường Xây",
                Icon = "🧱",
                Badge = "Tường Ngăn",
                BadgeColor = "#D97706",
                CommandTag = "DTT_WALL110",
                PreviewType = PreviewModelType.Wall,
                ParamWidth = 110,
                ParamHeight = 3300,
                ParamLength = 2000,
                Description = "Tiết kiệm tải trọng bản sàn và tối đa hóa diện tích sử dụng phòng."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "WALL_AUTO_WW",
                Title = "Vẽ Tường Giao Cắt Tự Động (Lệnh WW)",
                Subtitle = "Tự động phát hiện và bo mép góc tường chữ T, chữ L, chữ Thập",
                Category = "YQArch",
                Icon = "⚡",
                Badge = "Auto YQ",
                BadgeColor = "#059669",
                CommandTag = "WW",
                PreviewType = PreviewModelType.Wall,
                ParamWidth = 220,
                ParamHeight = 3000,
                Description = "Lệnh WW huyền thoại của YQArch đã được Việt hóa tối ưu phím tắt."
            });

            _registry["WALLS"] = cat;
        }

        private static void RegisterWindows()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "WINDOWS",
                Title = "THƯ VIỆN CỬA SỔ & VÁCH KÍNH KIẾN TRÚC",
                Subtitle = "Cửa sổ 2 cánh, 4 cánh, cửa sổ lùa, vách kính mặt dựng với preview 2D/3D",
                Icon = "🪟",
                ThemeColor = "#0284C7"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "WIN_2C",
                Title = "Cửa Sổ Mở Quay 2 Cánh 1200x1400",
                Subtitle = "Cửa sổ phòng ngủ, phòng khách thông gió tự nhiên tốt",
                Category = "Cửa Sổ Mở",
                Icon = "🪟",
                Badge = "Phổ Biến",
                BadgeColor = "#0284C7",
                CommandTag = "DTT_WIN_MENU",
                PreviewType = PreviewModelType.Window,
                ParamWidth = 1200,
                ParamHeight = 1400,
                QuickSizes = new List<string> { "1000x1200", "1200x1400", "1400x1600" },
                Description = "Kính an toàn 8.38mm hoặc kính hộp cách âm cách nhiệt tốt."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "WIN_4C",
                Title = "Cửa Sổ 4 Cánh Mặt Tiền 2400x1600",
                Subtitle = "Cửa sổ lớn mặt tiền, tạo view thoáng rộng cho phòng khách",
                Category = "Cửa Sổ Lớn",
                Icon = "🪟",
                Badge = "Mặt Tiền",
                BadgeColor = "#059669",
                CommandTag = "DTT_WIN_MENU",
                PreviewType = PreviewModelType.Window,
                ParamWidth = 2400,
                ParamHeight = 1600,
                Description = "Tối đa hóa ánh sáng ban ngày tự nhiên và luồng gió đối lưu."
            });

            _registry["WINDOWS"] = cat;
        }

        private static void RegisterCabinets()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "CABINETS",
                Title = "BỘ THIẾT KẾ TỦ ĐIỆN VẠN NĂNG 2D/3D & XUẤT BOM",
                Subtitle = "Dựng tủ phân phối DB, tủ tổng MDB, tủ ATS, tủ PLC với xem trước 3D Solid xoay 360°",
                Icon = "📦",
                ThemeColor = "#0284C7"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CAB_DB",
                Title = "Tủ Điện Phân Phối Tầng DB (800x1200x300)",
                Subtitle = "Tủ treo tường / đặt sàn cấp nguồn chiếu sáng và động lực",
                Category = "Tủ Điện",
                Icon = "📦",
                Badge = "Tủ DB",
                BadgeColor = "#0284C7",
                CommandTag = "DTT_ELEC_DB",
                PreviewType = PreviewModelType.Cabinet,
                ParamWidth = 800,
                ParamHeight = 1200,
                ParamLength = 300,
                QuickSizes = new List<string> { "600x800x250", "800x1200x300", "1000x1400x350" },
                Description = "Chứa MCCB tổng 100A-250A và hàng MCB nhánh 1P/3P kèm thanh cái đồng."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CAB_MDB",
                Title = "Tủ Điện Tổng MDB (1200x2000x600)",
                Subtitle = "Tủ điện hạ thế chính đặt phòng máy biến áp / kỹ thuật tòa nhà",
                Category = "Tủ Điện",
                Icon = "⚡",
                Badge = "Tủ MDB",
                BadgeColor = "#EF4444",
                CommandTag = "DTT_ELEC_MDB",
                PreviewType = PreviewModelType.Cabinet,
                ParamWidth = 1200,
                ParamHeight = 2000,
                ParamLength = 600,
                Description = "Chứa ACB 1000A-3200A, rơ le bảo vệ kỹ thuật số, đồng hồ đa năng MFM."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CAB_3D",
                Title = "Dựng Mô Hình 3D Solid Tủ Điện (DTT_CABINET_3D)",
                Subtitle = "Sinh trực tiếp các đối tượng 3D Solid thực trong không gian AutoCAD Model",
                Category = "3D CAD",
                Icon = "🧊",
                Badge = "3D Solid",
                BadgeColor = "#7C3AED",
                CommandTag = "DTT_CABINET_3D",
                PreviewType = PreviewModelType.Cabinet,
                ParamWidth = 800,
                ParamHeight = 1800,
                ParamLength = 400,
                Description = "Mô hình 3D chính xác từng cánh tủ, khóa, đèn báo và thanh DIN rail."
            });

            _registry["CABINETS"] = cat;
        }

        private static void RegisterElectrical()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "ELECTRICAL",
                Title = "HỆ THỐNG CƠ ĐIỆN & TỰ ĐỘNG HÓA CHUYÊN SÂU",
                Subtitle = "Sơ đồ hàng kẹp, đánh số dây, tham chiếu chéo, mạch khởi động động cơ, kiểm lỗi DRC",
                Icon = "⚡",
                ThemeColor = "#0284C7"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEC_TERMINAL",
                Title = "Sơ Đồ Hàng Kẹp Terminal Strip (DTT_TERMINAL)",
                Subtitle = "Tự động sinh bảng hàng kẹp cầu đấu dây tủ điện chi tiết",
                Category = "Ngành Điện",
                Icon = "⛓️",
                Badge = "Terminal",
                BadgeColor = "#0284C7",
                CommandTag = "DTT_TERMINAL_STRIP",
                PreviewType = PreviewModelType.Electrical,
                Description = "Định danh TB1, TB2 với thông số tiết diện dây và điểm nối cáp ngoại vi."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEC_WIRENUM",
                Title = "Đánh Số Dây Tự Động (Smart Wire Numbering)",
                Subtitle = "Quét các đường dây kết nối và gán nhãn dây L1, L2, L3, N, PE tự động",
                Category = "Ngành Điện",
                Icon = "🔢",
                Badge = "Auto Num",
                BadgeColor = "#059669",
                CommandTag = "DTT_SMART_WIRENUM",
                PreviewType = PreviewModelType.Electrical,
                Description = "Không trùng lặp số dây trên toàn bộ bản vẽ schematic."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEC_XREF",
                Title = "Tham Chiếu Chéo Rơ-le (Relay Cross-Reference)",
                Subtitle = "Liên kết cuộn hút cuộn dây (Coil) với các tiếp điểm thường đóng/thường mở NO/NC",
                Category = "Ngành Điện",
                Icon = "🪞",
                Badge = "Cross Ref",
                BadgeColor = "#7C3AED",
                CommandTag = "DTT_CROSS_REF",
                PreviewType = PreviewModelType.Electrical,
                Description = "Ghi số tọa độ cột và trang bản vẽ tương ứng của tiếp điểm phụ."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEC_STARTER",
                Title = "Mạch Khởi Động Động Cơ (DOL / Y-D / VFD)",
                Subtitle = "Chèn mạch động lực và mạch điều khiển khởi động trực tiếp, sao-tam giác, biến tần",
                Category = "Ngành Điện",
                Icon = "⚙️",
                Badge = "Starter",
                BadgeColor = "#D97706",
                CommandTag = "DTT_MOTOR_STARTER",
                PreviewType = PreviewModelType.Electrical,
                Description = "Mạch mẫu tiêu chuẩn bảo vệ quá tải, mất pha và ngắn mạch."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "ELEC_DRC",
                Title = "Kiểm Lỗi Thiết Kế Mạch Điện DRC (Electrical DRC)",
                Subtitle = "Quét phát hiện dây hở mạch, trùng số dây, thiếu cuộn hút rơ-le",
                Category = "Kiểm Tra",
                Icon = "🛡️",
                Badge = "DRC Audit",
                BadgeColor = "#EF4444",
                CommandTag = "DTT_ELEC_DRC",
                PreviewType = PreviewModelType.Electrical,
                Description = "Báo cáo lỗi chi tiết dạng bảng giúp bản vẽ không có sai sót trước khi đấu nối."
            });

            _registry["ELECTRICAL"] = cat;
        }

        private static void RegisterMep()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "MEP",
                Title = "HỆ THỐNG ĐƯỜNG ỐNG CẤP THOÁT NƯỚC & MEP",
                Subtitle = "Ống cấp nước PPR, ống thoát PVC, van khóa, phụ kiện co lơ với mô phỏng 3D",
                Icon = "💧",
                ThemeColor = "#0284C7"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "PIPE_PPR",
                Title = "Đường Ống Cấp Nước Lạnh PPR D32",
                Subtitle = "Ống hàn nhiệt PPR chịu áp lực PN10 / PN16 • Cấp nước sinh hoạt",
                Category = "Cấp Nước",
                Icon = "💧",
                Badge = "PPR",
                BadgeColor = "#0284C7",
                CommandTag = "DTT_MEP_MENU",
                PreviewType = PreviewModelType.Pipe,
                ParamWidth = 32,
                ParamLength = 1500,
                Description = "Tiêu chuẩn DIN 8077/8078, chống ăn mòn hóa chất và bám cặn."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "PIPE_PVC",
                Title = "Đường Ống Thoát Nước Thải PVC D110",
                Subtitle = "Ống thoát xí, thoát sàn đường kính D110mm có độ dốc chuẩn 1.5 - 2%",
                Category = "Thoát Nước",
                Icon = "💧",
                Badge = "PVC D110",
                BadgeColor = "#64748B",
                CommandTag = "DTT_MEP_MENU",
                PreviewType = PreviewModelType.Pipe,
                ParamWidth = 110,
                ParamLength = 1500,
                Description = "Độ dốc tiêu chuẩn đảm bảo dòng chảy tự cọ rửa không bị lắng đọng cặn."
            });

            _registry["MEP"] = cat;
        }

        private static void RegisterMaintenance()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "MAINTENANCE",
                Title = "BẢO DƯỠNG & SỬA LỖI BẢN VẼ DWG TRIỆT ĐỂ",
                Subtitle = "Dọn rác, kiểm tra lỗi Audit, chuẩn hóa layer, đưa về Z=0, sửa font tiếng Việt",
                Icon = "🧹",
                ThemeColor = "#059669"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CLEAN_PURGE",
                Title = "Dọn Rác & Sửa Lỗi DWG Triệt Để (DT_CLEAN)",
                Subtitle = "Tự động chạy PURGE ALL + AUDIT FIX + dọn sạch RegApp làm nhẹ file CAD tới 80%",
                Category = "Bảo Dưỡng",
                Icon = "🧹",
                Badge = "Siêu Dọn Rác",
                BadgeColor = "#059669",
                CommandTag = "DT_CLEAN",
                PreviewType = PreviewModelType.Maintenance,
                Description = "Khắc phục hiện tượng giật lag khi zoom pan trên các bản vẽ nặng hàng chục MB."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CLEAN_LAYER",
                Title = "Phân Loại Layer Chuẩn Hóa (DT_QUICKLAYER)",
                Subtitle = "Tự động gom Text, Dim, Hatch, Trục vào các layer chuyên biệt có màu sắc chuẩn",
                Category = "Layer",
                Icon = "🏷️",
                Badge = "Chuẩn Hóa",
                BadgeColor = "#0284C7",
                CommandTag = "DT_QUICKLAYER",
                PreviewType = PreviewModelType.Maintenance,
                Description = "Giúp kiểm soát bật/tắt (Freeze/Thaw) từng lớp đối tượng nhanh chóng."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CLEAN_FONT",
                Title = "Chuẩn Hóa Font Tiếng Việt (DTT_FIXFONT)",
                Subtitle = "Chuyển các style chữ bị lỗi dấu chấm hỏi '???' sang Arial Unicode chuẩn",
                Category = "Font Chữ",
                Icon = "🔤",
                Badge = "Tiếng Việt",
                BadgeColor = "#7C3AED",
                CommandTag = "DTT_FIXFONT",
                PreviewType = PreviewModelType.Maintenance,
                Description = "Tự động gán font Arial/Tahoma không bao giờ lỗi font trên bất kỳ máy nào."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "CLEAN_FLATTEN",
                Title = "Đưa Toàn Bộ Đối Tượng Về Z=0 (DTT_FLATTEN)",
                Subtitle = "Xóa bỏ triệt để độ lệch cao độ Z vô tình xuất hiện khi vẽ 2D, giúp bắt điểm chính xác",
                Category = "Hình Học",
                Icon = "📐",
                Badge = "Phẳng Z=0",
                BadgeColor = "#B45309",
                CommandTag = "DTT_FLATTEN",
                PreviewType = PreviewModelType.Maintenance,
                Description = "Tránh lỗi không Trim/Extend hoặc không tính được diện tích Hatch."
            });

            _registry["MAINTENANCE"] = cat;
        }

        private static void RegisterYqQuick()
        {
            var cat = new ModalCategoryConfig
            {
                Key = "YQ_QUICK",
                Title = "MENU THAO TÁC NHANH YQARCH TIẾNG VIỆT",
                Subtitle = "Các lệnh vẽ kiến trúc thông minh hàng đầu: Tường, Cửa, Dim, Trục, Thang",
                Icon = "⚡",
                ThemeColor = "#0284C7"
            };

            cat.Options.Add(new ModalOptionItem
            {
                Id = "YQ_WW",
                Title = "Vẽ Tường Cắt Giao Tự Động (WW)",
                Subtitle = "Lệnh vẽ tường đôi thông minh, tự đục mép tại điểm giao cắt",
                Category = "Kiến Trúc",
                Icon = "🧱",
                Badge = "Lệnh WW",
                BadgeColor = "#0284C7",
                CommandTag = "WW",
                PreviewType = PreviewModelType.Wall,
                ParamWidth = 220,
                Description = "Phím tắt chuẩn YQArch Tiếng Việt."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "YQ_AD",
                Title = "Mở Cửa Đi & Cửa Sổ Trên Tường (AD)",
                Subtitle = "Chọn vị trí trên tường để mở cửa, tự động cắt nét tường và tạo hèm cửa",
                Category = "Kiến Trúc",
                Icon = "🚪",
                Badge = "Lệnh AD",
                BadgeColor = "#059669",
                CommandTag = "AD",
                PreviewType = PreviewModelType.Door,
                Description = "Đục tường chính xác theo kích thước lọt lòng."
            });

            cat.Options.Add(new ModalOptionItem
            {
                Id = "YQ_DD",
                Title = "Dim Tự Động Tổng & Chi Tiết (DD)",
                Subtitle = "Đo kích thước mặt bằng nhanh chóng chỉ bằng 1 thao tác quét chuột",
                Category = "Kích Thước",
                Icon = "📏",
                Badge = "Lệnh DD",
                BadgeColor = "#D97706",
                CommandTag = "DD",
                PreviewType = PreviewModelType.General,
                Description = "Tự tạo chuỗi kích thước 3 lớp: Tổng, Tim trục, Chi tiết cửa."
            });

            _registry["YQ_QUICK"] = cat;
        }

        #endregion

        #region 2. API Tra Cứu & Hiển Thị Modal

        /// <summary>
        /// Lấy cấu hình Modal phù hợp với tag lệnh của nút bấm (CHỈ trả về nếu có đăng ký tường minh)
        /// </summary>
        public static ModalCategoryConfig GetCategory(string cmdTag)
        {
            if (string.IsNullOrWhiteSpace(cmdTag)) return null;

            string clean = cmdTag.Trim();

            if (_registry.TryGetValue(clean, out var config))
            {
                if (string.Equals(config.Key, "DYNAMIC_BLOCKS", StringComparison.OrdinalIgnoreCase))
                    RefreshDynamicBlocksFromFolder(config);
                return config;
            }

            // Tuyệt đối KHÔNG matching mờ hoặc fallback bừa bãi, tránh làm đơ AutoCAD
            return null;
        }

        /// <summary>
        /// Mở Modal Pop-up Trung Tâm Màn Hình với cấu hình tương ứng
        /// </summary>
        public static bool ShowCenterModal(string cmdTag)
        {
            try
            {
                ModalCategoryConfig cat = GetCategory(cmdTag);
                if (cat == null) return false;
                DTT_GlobalCenterModal modal = new DTT_GlobalCenterModal(cat);
                return modal.ShowDialog() == true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi hiển thị Modal: " + ex.Message, "DTools Modal Error");
                return false;
            }
        }

        #endregion
    }
}
