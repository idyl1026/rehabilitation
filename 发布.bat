@echo off
chcp 65001 >nul
echo ========================================
echo  病程辅助书写系统 - 发布脚本
echo ========================================
echo.

cd /d "%~dp0src\MedicalProgress.App"

echo [1/3] 正在恢复依赖...
dotnet restore
if %errorlevel% neq 0 (
    echo 错误：依赖恢复失败
    pause
    exit /b 1
)

echo.
echo [2/3] 正在编译项目...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo 错误：编译失败
    pause
    exit /b 1
)

echo.
echo [3/3] 正在发布为自包含 Windows 程序...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0publish"
if %errorlevel% neq 0 (
    echo 错误：发布失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo  发布成功
echo ========================================
echo.
echo exe 文件位置：%~dp0publish\MedicalProgress.App.exe
echo.
echo 首次运行时会自动创建数据库文件。
echo 数据库位置：%%LOCALAPPDATA%%\MedicalProgress\
echo.
pause
