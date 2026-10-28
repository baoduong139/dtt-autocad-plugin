using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using DTools.Core;

namespace DTT_AutoCAD_Plugin.Modules.ElectricalNative
{
    public static class HostCommandInvoker
    {
        private static readonly Dictionary<string, IHostCommandAdapter> _adapters = new Dictionary<string, IHostCommandAdapter>(StringComparer.OrdinalIgnoreCase);

        static HostCommandInvoker()
        {
            Register(new CircBuilderAdapter());
            Register(new SavedCircuitAdapter());
            Register(new SaveCircuitAdapter());
            Register(new WbCircuitAdapter());
            Register(new ComponentAdapter());
            Register(new WireAdapter());
            Register(new ConnectCompAdapter());
            Register(new MultiBusAdapter());
            Register(new LadderAdapter());
            Register(new WireNoAdapter());
            Register(new XrefAdapter());
            Register(new AuditAdapter());
            Register(new ProjUpdateAdapter());
        }

        private static void Register(IHostCommandAdapter adapter)
        {
            _adapters[adapter.CommandName] = adapter;
        }

        public static IHostCommandAdapter GetAdapter(string commandName)
        {
            if (_adapters.TryGetValue(commandName, out var a)) return a;
            return null;
        }

        public static IEnumerable<IHostCommandAdapter> GetAllAdapters() => _adapters.Values;

        public static bool SendCommandSafe(string cmdStr, bool echo = true)
        {
            try
            {
                Document doc = Application.DocumentManager?.MdiActiveDocument;
                if (doc == null)
                {
                    DToolsLogger.LogWarning("CommandInvoker", $"Không thể gửi lệnh '{cmdStr}': Không có bản vẽ nào đang mở.");
                    return false;
                }

                DToolsLogger.LogInfo("CommandInvoker", $"Gửi lệnh gốc AutoCAD Electrical: {cmdStr}");
                doc.SendStringToExecute(cmdStr + "\n", true, false, echo);
                return true;
            }
            catch (Exception ex)
            {
                DToolsLogger.LogError("CommandInvoker", $"Lỗi khi thực thi lệnh '{cmdStr}': {ex.Message}", ex);
                return false;
            }
        }
    }

    #region Adapters

    public class CircBuilderAdapter : IHostCommandAdapter
    {
        public string CommandName => "AECIRCBUILDER";
        public string DisplayName => "Trình dựng mạch 3 pha (Circuit Builder)";
        public string Description => "Khởi chạy công cụ dựng mạch động cơ chuẩn FVNR của AutoCAD Electrical.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.HasCircuitBuilder;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AECIRCBUILDER");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class SavedCircuitAdapter : IHostCommandAdapter
    {
        public string CommandName => "AESAVEDCIRCUIT";
        public string DisplayName => "Chèn mạch lưu sẵn (Insert Saved Circuit)";
        public string Description => "Chèn sơ đồ mạch điện từ kho mẫu DWG của AutoCAD Electrical.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AESAVEDCIRCUIT");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class SaveCircuitAdapter : IHostCommandAdapter
    {
        public string CommandName => "AESAVECIRCUIT";
        public string DisplayName => "Lưu mạch điện mẫu (Save Circuit)";
        public string Description => "Lưu mạch vừa thiết kế vào thư viện Saved Circuits.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AESAVECIRCUIT");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class WbCircuitAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEWBCIRCUIT";
        public string DisplayName => "Xuất mạch ra WBLOCK (Wblock Circuit)";
        public string Description => "Xuất khối mạch điện thành file DWG tái sử dụng.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEWBCIRCUIT");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class ComponentAdapter : IHostCommandAdapter
    {
        public string CommandName => "AECOMPONENT";
        public string DisplayName => "Chèn linh kiện thông minh (Insert Component)";
        public string Description => "Chèn schematic symbol có thuộc tính TAG1/TAG2 và cổng snap dây.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AECOMPONENT");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class WireAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEWIRE";
        public string DisplayName => "Vẽ dây điện thông minh (Insert Wire)";
        public string Description => "Vẽ dây trên wire layer chuyên dụng và tự động tạo Junction Dot.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEWIRE");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class ConnectCompAdapter : IHostCommandAdapter
    {
        public string CommandName => "AECONNECTCOMP";
        public string DisplayName => "Nối linh kiện thẳng hàng (Connect Components)";
        public string Description => "Tự động nối dây giữa các linh kiện có chân thẳng hàng.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AECONNECTCOMP");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class MultiBusAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEMULTIBUS";
        public string DisplayName => "Vẽ thanh cái đa pha (Multiple Bus)";
        public string Description => "Rải thanh cái song song cấp nguồn L1, L2, L3, N.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEMULTIBUS");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class LadderAdapter : IHostCommandAdapter
    {
        public string CommandName => "AELADDER";
        public string DisplayName => "Tạo khung thang sơ đồ điều khiển (Insert Ladder)";
        public string Description => "Tạo thang sơ đồ mạch điều khiển bậc thang có đánh số rung.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AELADDER");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class WireNoAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEWIRENO";
        public string DisplayName => "Đánh số dây tự động (Wire Numbers)";
        public string Description => "Tự động đánh số dây điện toàn bản vẽ theo chuẩn dự án.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.HasWireNumbering;
        public bool InteractiveRequired => false;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEWIRENO");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class XrefAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEXREF";
        public string DisplayName => "Cập nhật tham chiếu chéo (Cross-Reference)";
        public string Description => "Đồng bộ liên kết giữa cuộn hút và các tiếp điểm phụ NO/NC.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.HasCrossReference;
        public bool InteractiveRequired => false;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEXREF");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class AuditAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEAUDIT";
        public string DisplayName => "Kiểm định bản vẽ điện (Electrical Audit)";
        public string Description => "Rà soát lỗi dây hở, trùng số dây và xung đột tiếp điểm.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.HasElectricalAudit;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEAUDIT");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    public class ProjUpdateAdapter : IHostCommandAdapter
    {
        public string CommandName => "AEPROJUPDATE";
        public string DisplayName => "Cập nhật toàn dự án (Project Update)";
        public string Description => "Đồng bộ hóa nhãn, số dây và tham chiếu trên toàn bộ dự án WDP.";
        public bool IsSupported => AcadeCapabilityDetector.Instance.IsAcadeInstalled;
        public bool InteractiveRequired => true;
        public bool Execute(object context = null) => HostCommandInvoker.SendCommandSafe("AEPROJUPDATE");
        public string GetDiagnostics() => $"Lệnh: {CommandName} | Khả dụng: {IsSupported}";
    }

    #endregion
}
