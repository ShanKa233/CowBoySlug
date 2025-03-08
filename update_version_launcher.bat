@echo off
chcp 65001 > nul
echo 版本更新工具启动器
echo ===============================
echo 请选择要使用的脚本:
echo 1. Batch 脚本 (update_version.bat)
echo 2. PowerShell 脚本 (update_version.ps1) - 推荐
echo ===============================
set /p choice=请输入选择 (1/2): 

if "%choice%"=="1" (
    echo 正在启动 Batch 脚本...
    call update_version.bat
) else if "%choice%"=="2" (
    echo 正在启动 PowerShell 脚本...
    powershell -ExecutionPolicy Bypass -File update_version.ps1
) else (
    echo 无效的选择!
    pause
    exit /b
)

exit /b 