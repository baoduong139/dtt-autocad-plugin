using System;

namespace DTT.Updater
{
    public class UpdateManifest
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string Version { get; set; } = "2026.2.0";
        public string Channel { get; set; } = "stable"; // stable | beta
        public string PublishedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string ReleaseNotesUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string MinAutoCadVersion { get; set; } = "2020";
        public string MaxAutoCadVersion { get; set; } = "2026";
        public string DownloadUrl { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public long PackageSize { get; set; } = 0;
        public string Publisher { get; set; } = "DTT CAD Software Corporation";
        public bool Mandatory { get; set; } = false;
    }

    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public UpdateManifest Manifest { get; set; }
        public string Message { get; set; }
    }
}

