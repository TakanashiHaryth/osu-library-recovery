@echo off
echo ========================================================
echo Building and Publishing Osu_Library_Recovery Application
echo ========================================================
dotnet publish src/Recovery.App/Recovery.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "./publish/Osu_Library_Recovery"
echo.
echo ========================================================
echo Build complete! Executable is located at:
echo   publish\Osu_Library_Recovery\Osu_Library_Recovery.exe
echo ========================================================
pause
