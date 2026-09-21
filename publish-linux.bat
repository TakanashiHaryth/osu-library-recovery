@echo off
echo ========================================================
echo Cross-Compiling Osu_Library_Recovery for Linux (linux-x64)
echo ========================================================
dotnet publish src/Recovery.App/Recovery.App.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o "./publish/Osu_Library_Recovery_Linux"
echo.
echo ========================================================
echo Linux build complete! Executable is located at:
echo   publish\Osu_Library_Recovery_Linux\Osu_Library_Recovery
echo ========================================================
pause
