@echo off
chcp 65001 > nul
echo 版本更新工具
echo ===============================
echo 请选择更新类型:
echo 1. 补丁版本更新 (0.2.50 -> 0.2.51)
echo 2. 次要版本更新 (0.2.50 -> 0.3.0)
echo 3. 主要版本更新 (0.2.50 -> 1.0.0)
echo ===============================
set /p choice=请输入选择 (1/2/3): 

if "%choice%"=="1" (
    echo 正在更新补丁版本...
    powershell -Command "(Get-Content \"version.txt\").Trim() -match '(\d+)\.(\d+)\.(\d+)' | Out-Null; $major = [int]$matches[1]; $minor = [int]$matches[2]; $build = [int]$matches[3] + 1; \"$major.$minor.$build\" | Set-Content \"version.txt\""
    set update_type=0
) else if "%choice%"=="2" (
    echo 正在更新次要版本...
    powershell -Command "(Get-Content \"version.txt\").Trim() -match '(\d+)\.(\d+)\.(\d+)' | Out-Null; $major = [int]$matches[1]; $minor = [int]$matches[2] + 1; \"$major.$minor.0\" | Set-Content \"version.txt\""
    set update_type=1
) else if "%choice%"=="3" (
    echo 正在更新主要版本...
    powershell -Command "(Get-Content \"version.txt\").Trim() -match '(\d+)\.(\d+)\.(\d+)' | Out-Null; $major = [int]$matches[1] + 1; \"$major.0.0\" | Set-Content \"version.txt\""
    set update_type=2
) else (
    echo 无效的选择!
    pause
    exit /b
)

set /p version=<version.txt
echo 新版本: %version%

echo 正在更新 modinfo.json...
powershell -Command "(Get-Content \"mod\modinfo.json\") -replace '\"version\":\s*\"[^\"]+\"', '\"version\": \"%version%\"' | Set-Content \"mod\modinfo.json\""

echo 正在更新 workshopdata.json...
powershell -Command "(Get-Content \"mod\workshopdata.json\") -replace '\"Version\":\s*\"[^\"]+\"', '\"Version\": \"%version%\"' | Set-Content \"mod\workshopdata.json\""

echo 版本更新完成!
pause 