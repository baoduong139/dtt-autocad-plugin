$ErrorActionPreference = "Stop"

$gitExe = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
if (-not (Test-Path $gitExe)) {
    throw "Khong tim thay git.exe!"
}

Write-Host "1. Khoi tao Git repository..." -ForegroundColor Yellow
if (-not (Test-Path ".git")) {
    & $gitExe init
}

Write-Host "2. Cau hinh Git user va remote origin..." -ForegroundColor Yellow
& $gitExe config user.name "baoduong139"
& $gitExe config user.email "baoduong139@users.noreply.github.com"

$remoteUrl = "https://github.com/baoduong139/dtt-autocad-plugin.git"
$remotes = & $gitExe remote
if ($remotes -contains "origin") {
    & $gitExe remote set-url origin $remoteUrl
} else {
    & $gitExe remote add origin $remoteUrl
}
& $gitExe remote -v

Write-Host "3. Chuyen nhanh sang main..." -ForegroundColor Yellow
& $gitExe branch -M main

Write-Host "4. Kiem tra cac tep tin truoc khi add..." -ForegroundColor Yellow
& $gitExe status --short

Write-Host "5. Git add . ..." -ForegroundColor Yellow
& $gitExe add .

Write-Host "6. Git commit..." -ForegroundColor Yellow
$msg = "feat: initial commit with autocad 2021 libs and ci/cd workflow"
& $gitExe commit -m $msg

Write-Host "7. Trang thai Git sau khi commit:" -ForegroundColor Green
& $gitExe status
