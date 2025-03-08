# 版本更新工具
Write-Host "版本更新工具" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host "请选择更新类型:" -ForegroundColor Cyan
Write-Host "1. 补丁版本更新 (0.2.50 -> 0.2.51)" -ForegroundColor Yellow
Write-Host "2. 次要版本更新 (0.2.50 -> 0.3.0)" -ForegroundColor Yellow
Write-Host "3. 主要版本更新 (0.2.50 -> 1.0.0)" -ForegroundColor Yellow
Write-Host "===============================" -ForegroundColor Cyan

$choice = Read-Host "请输入选择 (1/2/3)"

# 获取当前版本
$versionContent = Get-Content "version.txt" -ErrorAction SilentlyContinue
if (-not $versionContent) {
    $versionContent = "0.2.50"  # 默认版本
    $versionContent | Set-Content "version.txt"
}

$versionContent = $versionContent.Trim()
if ($versionContent -match '(\d+)\.(\d+)\.(\d+)') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $build = [int]$matches[3]
} else {
    Write-Host "无法解析版本号: $versionContent" -ForegroundColor Red
    Write-Host "使用默认版本: 0.2.50" -ForegroundColor Yellow
    $major = 0
    $minor = 2
    $build = 50
}

# 根据选择更新版本
switch ($choice) {
    "1" {
        Write-Host "正在更新补丁版本..." -ForegroundColor Green
        $build++
    }
    "2" {
        Write-Host "正在更新次要版本..." -ForegroundColor Green
        $minor++
        $build = 0
    }
    "3" {
        Write-Host "正在更新主要版本..." -ForegroundColor Green
        $major++
        $minor = 0
        $build = 0
    }
    default {
        Write-Host "无效的选择!" -ForegroundColor Red
        exit
    }
}

# 生成新版本号
$newVersion = "$major.$minor.$build"
Write-Host "新版本: $newVersion" -ForegroundColor Cyan

# 更新 version.txt
$newVersion | Set-Content "version.txt"
Write-Host "已更新 version.txt" -ForegroundColor Green

# 更新 modinfo.json
$modInfoPath = "mod\modinfo.json"
if (Test-Path $modInfoPath) {
    $modInfoContent = Get-Content $modInfoPath -Raw
    $modInfoContent = $modInfoContent -replace '"version"\s*:\s*"[^"]+"', "`"version`": `"$newVersion`""
    $modInfoContent | Set-Content $modInfoPath
    Write-Host "已更新 modinfo.json" -ForegroundColor Green
} else {
    Write-Host "找不到 modinfo.json 文件" -ForegroundColor Red
}

# 更新 workshopdata.json
$workshopDataPath = "mod\workshopdata.json"
if (Test-Path $workshopDataPath) {
    $workshopDataContent = Get-Content $workshopDataPath -Raw
    $workshopDataContent = $workshopDataContent -replace '"Version"\s*:\s*"[^"]+"', "`"Version`": `"$newVersion`""
    $workshopDataContent | Set-Content $workshopDataPath
    Write-Host "已更新 workshopdata.json" -ForegroundColor Green
} else {
    Write-Host "找不到 workshopdata.json 文件" -ForegroundColor Red
}

Write-Host "版本更新完成!" -ForegroundColor Cyan
Write-Host "按任意键继续..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 