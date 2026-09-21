#!/usr/bin/env bash
set -e

echo "========================================================"
echo "Building and Publishing Osu_Library_Recovery for Linux"
echo "========================================================"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

dotnet publish src/Recovery.App/Recovery.App.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o "./publish/Osu_Library_Recovery_Linux"

chmod +x "./publish/Osu_Library_Recovery_Linux/Osu_Library_Recovery"

echo ""
echo "========================================================"
echo "Build complete! Executable is located at:"
echo "  publish/Osu_Library_Recovery_Linux/Osu_Library_Recovery"
echo ""
echo "To run:"
echo "  ./publish/Osu_Library_Recovery_Linux/Osu_Library_Recovery"
echo "========================================================"
