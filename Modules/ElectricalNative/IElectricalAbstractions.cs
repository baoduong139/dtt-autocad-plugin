using System;
using System.Collections.Generic;

namespace DTT_AutoCAD_Plugin.Modules.ElectricalNative
{
    public enum ElectricalHostMode
    {
        AutoCadElectricalNative,  // Host có ACADE thật: Tận dụng symbol IEC, wire layers, commands
        CadGeometryFallback,      // Host là AutoCAD thường: Vẽ thực thể CAD chuẩn + attributes
        InteractiveDialogRequired // Lệnh native mở hộp thoại tương tác của Autodesk
    }

    public enum CircuitValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public class CircuitValidationMessage
    {
        public CircuitValidationSeverity Severity { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class CircuitValidationReport
    {
        public bool IsValid => Errors.Count == 0;
        public List<CircuitValidationMessage> Messages { get; } = new List<CircuitValidationMessage>();
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Corrections { get; } = new List<string>();

        public string GetSummary()
        {
            if (Errors.Count > 0) return string.Join("; ", Errors);
            if (Warnings.Count > 0) return string.Join("; ", Warnings);
            if (Corrections.Count > 0) return string.Join("; ", Corrections);
            return "Hợp lệ theo tiêu chuẩn kỹ thuật.";
        }
    }

    /// <summary>
    /// Nhà cung cấp năng lực hệ thống AutoCAD Electrical trên máy thực tế
    /// </summary>
    public interface IElectricalCapabilityProvider
    {
        bool IsAcadeInstalled { get; }
        string HostProduct { get; }
        string HostVersion { get; }
        string SymbolLibraryPath { get; }
        string LibraryStandard { get; } // IEC-60617, JIC, NFPA
        bool HasCircuitBuilder { get; }
        bool HasWireNumbering { get; }
        bool HasCrossReference { get; }
        bool HasElectricalAudit { get; }
        ElectricalHostMode DetectHostMode();
        string GetCapabilityReport();
    }

    /// <summary>
    /// Bộ chuyển đổi adapter cho một lệnh gốc AutoCAD Electrical
    /// </summary>
    public interface IHostCommandAdapter
    {
        string CommandName { get; }
        string DisplayName { get; }
        string Description { get; }
        bool IsSupported { get; }
        bool InteractiveRequired { get; }
        bool Execute(object context = null);
        string GetDiagnostics();
    }

    /// <summary>
    /// Dịch vụ tạo và kiểm tra mạch điện thông minh
    /// </summary>
    public interface IElectricalCircuitService
    {
        CircuitValidationReport ValidateDolStarter(bool isStandardMode);
        CircuitValidationReport ValidateStarDelta(bool isStandardMode);
        List<string> SimulateStarDeltaFSM(bool simulateTrip = false);
    }

    /// <summary>
    /// Dịch vụ quản lý kho ký hiệu và catalog
    /// </summary>
    public interface IAssetCatalogService
    {
        string GetSymbolBlockPath(string symbolFamily);
        bool VerifyCatalogDatabase();
    }
}
