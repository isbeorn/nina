#!/bin/bash
# Install System.Windows.dll stub into .NET runtime
# This replaces the framework System.Windows.dll with our OpenCV-backed implementation

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Allow override via env vars
DOTNET_ROOT="${DOTNET_ROOT:-/usr/lib/dotnet}"
COMPAT_DIR="${COMPAT_DIR:-$SCRIPT_DIR/System.Windows.Compat}"
BUILD_CONFIG="Release"

# Detect latest .NET version installed
DOTNET_VERSION=$(ls "$DOTNET_ROOT/shared/Microsoft.NETCore.App" | sort -V | tail -n1)
FRAMEWORK_DIR="$DOTNET_ROOT/shared/Microsoft.NETCore.App/$DOTNET_VERSION"

# Dynamically detect target framework from System.Windows.Compat.csproj
TARGET_FRAMEWORK=$(grep -oP '(?<=<TargetFramework>)[^<]+' "$COMPAT_DIR/System.Windows.Compat.csproj" | head -n1)
if [ -z "$TARGET_FRAMEWORK" ]; then
    echo "Error: Could not detect target framework from $COMPAT_DIR/System.Windows.Compat.csproj"
    exit 1
fi

STUB_DLL="$COMPAT_DIR/bin/Release/$TARGET_FRAMEWORK/System.Windows.dll"
if [ ! -f "$STUB_DLL" ]; then
    echo "Stub DLL not found in Release. Building System.Windows.Compat in Release mode..."
    dotnet build "$COMPAT_DIR/System.Windows.Compat.csproj" -c Release
fi

if [ ! -f "$STUB_DLL" ]; then
    echo "Error: System.Windows.dll stub not found in Release build."
    exit 1
fi

if [ ! -f "$FRAMEWORK_DIR/System.Windows.dll" ]; then
    echo "Error: Framework System.Windows.dll not found at $FRAMEWORK_DIR"
    exit 1
fi

echo "Backing up original System.Windows.dll..."
sudo cp "$FRAMEWORK_DIR/System.Windows.dll" "$FRAMEWORK_DIR/System.Windows.dll.backup" 2>/dev/null || true

echo "Installing System.Windows.dll stub..."
sudo cp "$STUB_DLL" "$FRAMEWORK_DIR/System.Windows.dll"

echo "✓ System.Windows.dll stub installed successfully"
echo ""
echo "To restore the original:"
echo "  sudo cp $FRAMEWORK_DIR/System.Windows.dll.backup $FRAMEWORK_DIR/System.Windows.dll"
