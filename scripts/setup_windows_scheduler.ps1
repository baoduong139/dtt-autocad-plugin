<#
.SYNOPSIS
    Thiết lập Windows Task Scheduler chạy tự động lúc 08:00 hằng ngày cho DTT Tools
.DESCRIPTION
    Đăng ký Scheduled Task chạy script daily_autonomous_run.ps1 lúc 08:00 AM (Asia/Ho_Chi_Minh).
    Hỗ trợ cờ StartWhenAvailable: nếu máy tính tắt lúc 8h sáng, Windows sẽ tự động kích hoạt
    ngay khi bật máy lại để không bỏ sót phiên làm việc.
#>

$taskName = "DTT_Daily_Autonomous_Dev"
$scriptPath = "$PSScriptRoot\daily_autonomous_run.ps1"
$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""

# Kích hoạt hằng ngày lúc 08:00 AM
$trigger = New-ScheduledTaskTrigger -Daily -At "08:00AM"

# Thiết lập: Tự chạy bù nếu bị lỡ (StartWhenAvailable), dừng sau 2 giờ nếu treo
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 2) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

try {
    # Kiểm tra xem Task đã tồn tại chưa
    $existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($existing) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "Đã hủy đăng ký Task cũ: $taskName"
    }

    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Description "DTT CAD MASTER AI Daily Autonomous Development System (08:00 AM Asia/Ho_Chi_Minh)"
    Write-Host "✅ Đã đăng ký thành công Windows Scheduled Task: $taskName"
    Write-Host "⏰ Thời gian chạy: 08:00 AM hằng ngày (Múi giờ Asia/Ho_Chi_Minh)"
    Write-Host "🔄 Chế độ chạy bù (StartWhenAvailable): BẬT"
} catch {
    Write-Host "Lỗi khi đăng ký Scheduled Task: $_"
    Write-Host "Gợi ý: Hãy mở PowerShell với quyền Administrator để đăng ký Task Scheduler."
}

