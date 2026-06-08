@echo off
chcp 65001 >nul
set "ROOT=%~dp0.."

dotnet publish "%ROOT%\src\MedicalProgress.App\MedicalProgress.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%ROOT%\publish"

echo.
echo Done. Press any key to exit...
pause >nul
