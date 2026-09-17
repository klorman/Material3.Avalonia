#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
work_directory="$(mktemp -d)"
trap 'rm -rf -- "$work_directory"' EXIT

package_directory="$work_directory/packages"
global_packages="$work_directory/global-packages"
version="0.0.0-symbol-test"

assert_font_symbols() {
    local font="$1"
    local present="$2"
    local absent="$3"
    python3 - "$repository_root/third_party/google/material-symbols/catalog.tsv" \
        "$font" "$present" "$absent" <<'PY'
import sys
from fontTools.ttLib import TTFont

catalog_path, font_path, present_names, absent_names = sys.argv[1:]
with open(catalog_path, encoding="utf-8") as catalog_file:
    catalog = {name: int(codepoint, 16) for name, codepoint in
               (line.rstrip("\n").split("\t") for line in catalog_file)}
codepoints = set(TTFont(font_path).getBestCmap())
for name in filter(None, present_names.split(",")):
    assert catalog[name] in codepoints, f"{name} is missing from {font_path}"
for name in filter(None, absent_names.split(",")):
    assert catalog[name] not in codepoints, f"{name} unexpectedly exists in {font_path}"
PY
}

dotnet pack "$repository_root/Material3.Avalonia/Material3.Avalonia.csproj" \
    -c Release -m:1 -p:PackageVersion="$version" -o "$package_directory" -v minimal

material_package="$package_directory/Material3.Avalonia.$version.nupkg"
for framework in net8.0 net10.0; do
    unzip -p "$material_package" \
        "buildTransitive/Material3.Avalonia.$framework.symbols.manifest" | grep -Fx 'ContentCopy|7'
done
unzip -l "$material_package" | grep -F 'tools/net472/x64/Material3.Avalonia.Build.Tasks.dll'
unzip -l "$material_package" | grep -F 'tools/net472/x86/Material3.Avalonia.Build.Tasks.dll'
unzip -l "$material_package" | grep -F 'tools/net472/arm64/Material3.Avalonia.Build.Tasks.dll'
unzip -l "$material_package" | grep -F 'tools/net472/x64/libHarfBuzzSharp.dll'
unzip -l "$material_package" | grep -F 'tools/net472/x86/libHarfBuzzSharp.dll'
unzip -l "$material_package" | grep -F 'tools/net472/arm64/libHarfBuzzSharp.dll'

create_program() {
    local directory="$1"
    mkdir -p "$directory"
    printf '%s\n' \
        'namespace SymbolPackageTest;' \
        'internal static class Program { public static void Main() { } }' \
        > "$directory/Program.cs"
}

component_directory="$work_directory/component"
create_program "$component_directory"
cat > "$component_directory/SymbolComponent.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
        <PackageId>SymbolComponent</PackageId>
        <Version>$version</Version>
        <IsPackable>true</IsPackable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Material3.Avalonia" Version="$version" />
    </ItemGroup>
</Project>
EOF
cat > "$component_directory/Icons.axaml" <<'EOF'
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:symbols="clr-namespace:Material3.Avalonia.Symbols;assembly=Material3.Avalonia">
    <Style Selector="Button.component-symbol">
        <Setter Property="Tag" Value="{symbols:Symbol Settings, Families=Sharp}" />
    </Style>
</Styles>
EOF

NUGET_PACKAGES="$global_packages" dotnet restore "$component_directory/SymbolComponent.csproj" \
    --source "$package_directory" --source https://api.nuget.org/v3/index.json -v minimal
NUGET_PACKAGES="$global_packages" dotnet pack "$component_directory/SymbolComponent.csproj" \
    --no-restore -c Release -m:1 -o "$package_directory" -v minimal
for framework in net8.0 net10.0; do
    unzip -p "$package_directory/SymbolComponent.$version.nupkg" \
        "buildTransitive/SymbolComponent.$framework.symbols.manifest" | grep -Fx 'Settings|4'
done

conflict_directory="$work_directory/conflict-component"
create_program "$conflict_directory"
mkdir -p "$conflict_directory/buildTransitive"
cat > "$conflict_directory/buildTransitive/ConflictComponent.targets" <<'EOF'
<Project />
EOF
cat > "$conflict_directory/ConflictComponent.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <PackageId>ConflictComponent</PackageId>
        <Version>$version</Version>
        <IsPackable>true</IsPackable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Material3.Avalonia" Version="$version" />
        <None Include="buildTransitive/ConflictComponent.targets"
              Pack="true" PackagePath="buildTransitive/" />
    </ItemGroup>
</Project>
EOF
NUGET_PACKAGES="$global_packages" dotnet restore "$conflict_directory/ConflictComponent.csproj" \
    --source "$package_directory" --source https://api.nuget.org/v3/index.json -v minimal
if NUGET_PACKAGES="$global_packages" dotnet pack "$conflict_directory/ConflictComponent.csproj" \
    --no-restore -c Release -m:1 -o "$package_directory" -v minimal \
    > "$conflict_directory/pack.log" 2>&1; then
    echo "Expected conflicting buildTransitive target packaging to fail."
    exit 1
fi
grep -F 'Material3GenerateSymbolManifestTargets=false' "$conflict_directory/pack.log"

create_consumer() {
    local directory="$1"
    local reference="$2"
    create_program "$directory"
    cat > "$directory/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    </PropertyGroup>
    <ItemGroup>
        $reference
    </ItemGroup>
</Project>
EOF
    cat > "$directory/App.axaml" <<'EOF'
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:symbols="clr-namespace:Material3.Avalonia.Symbols;assembly=Material3.Avalonia">
    <Application.Resources>
        <Button x:Key="Home1" Tag="{symbols:Symbol Home}" />
        <Button x:Key="Home2" Tag="{symbols:Symbol Home}" />
        <Button x:Key="TuneRounded" Tag="{symbols:Symbol Tune, Families=Rounded}" />
        <Button x:Key="TuneSharp" Tag="{symbols:Symbol Tune, Families=Sharp}" />
        <Button x:Key="Search" Tag="{x:Static symbols:MaterialSymbol.Search}" />
        <symbols:MaterialSymbolsInclude x:Key="Player" Symbols="PlayArrow, Pause" Families="Rounded" />
    </Application.Resources>
</Application>
EOF
}

package_consumer="$work_directory/package-consumer"
create_consumer "$package_consumer" \
    "<PackageReference Include=\"SymbolComponent\" Version=\"$version\" />"
NUGET_PACKAGES="$global_packages" dotnet restore "$package_consumer/Consumer.csproj" \
    --source "$package_directory" --source https://api.nuget.org/v3/index.json -v minimal
NUGET_PACKAGES="$global_packages" dotnet build "$package_consumer/Consumer.csproj" \
    --no-restore -c Release -m:1 -v minimal

dotnet_8_sdk="$(dotnet --list-sdks | awk '$1 ~ /^8\./ { print $1; exit }')"
test -n "$dotnet_8_sdk"
sdk_8_consumer="$work_directory/sdk-8-consumer"
create_program "$sdk_8_consumer"
cp "$package_consumer/App.axaml" "$sdk_8_consumer/App.axaml"
cat > "$sdk_8_consumer/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="SymbolComponent" Version="$version" />
    </ItemGroup>
</Project>
EOF
cat > "$sdk_8_consumer/global.json" <<EOF
{
    "sdk": {
        "version": "$dotnet_8_sdk"
    }
}
EOF
pushd "$sdk_8_consumer" > /dev/null
NUGET_PACKAGES="$global_packages" dotnet restore Consumer.csproj \
    --source "$package_directory" --source https://api.nuget.org/v3/index.json -v minimal
NUGET_PACKAGES="$global_packages" dotnet build Consumer.csproj \
    --no-restore -c Release -m:1 -v minimal
popd > /dev/null

project_consumer="$work_directory/project-consumer"
create_consumer "$project_consumer" \
    "<ProjectReference Include=\"$component_directory/SymbolComponent.csproj\" />"
NUGET_PACKAGES="$global_packages" dotnet restore "$project_consumer/Consumer.csproj" \
    --source "$package_directory" --source https://api.nuget.org/v3/index.json -v minimal
NUGET_PACKAGES="$global_packages" dotnet build "$project_consumer/Consumer.csproj" \
    --no-restore -c Release -m:1 -v minimal

for framework in net8.0 net10.0; do
    package_manifest="$package_consumer/obj/Release/$framework/material-symbols/requests.manifest"
    project_manifest="$project_consumer/obj/Release/$framework/material-symbols/requests.manifest"
    diff -u "$package_manifest" "$project_manifest"
    grep -Fx 'Home|7' "$package_manifest"
    grep -Fx 'Pause|2' "$package_manifest"
    grep -Fx 'PlayArrow|2' "$package_manifest"
    grep -Fx 'Search|7' "$package_manifest"
    grep -Fx 'Settings|4' "$package_manifest"
    grep -Fx 'Tune|6' "$package_manifest"
    fonts="$package_consumer/obj/Release/$framework/material-symbols/fonts"
    test "$(find "$fonts" -name '*.ttf' | wc -l)" -eq 3
    assert_font_symbols "$fonts/Outlined.ttf" "ContentCopy,Home,Search" "Pause,PlayArrow,Settings,Tune"
    assert_font_symbols "$fonts/Rounded.ttf" "ContentCopy,Home,Pause,PlayArrow,Search,Tune" "Settings"
    assert_font_symbols "$fonts/Sharp.ttf" "ContentCopy,Home,Search,Settings,Tune" "Pause,PlayArrow"
done

all_consumer="$work_directory/all-consumer"
create_program "$all_consumer"
cat > "$all_consumer/AllConsumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Material3.Avalonia" Version="$version" />
    </ItemGroup>
</Project>
EOF
cat > "$all_consumer/App.axaml" <<'EOF'
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:symbols="clr-namespace:Material3.Avalonia.Symbols;assembly=Material3.Avalonia">
    <Application.Resources>
        <symbols:MaterialSymbolsInclude x:Key="AllRounded" Symbols="All" Families="Rounded" />
    </Application.Resources>
</Application>
EOF
NUGET_PACKAGES="$global_packages" dotnet restore "$all_consumer/AllConsumer.csproj" \
    --source "$package_directory" --source https://api.nuget.org/v3/index.json -v minimal
NUGET_PACKAGES="$global_packages" dotnet build "$all_consumer/AllConsumer.csproj" \
    --no-restore -m:1 -v minimal
grep -Fx '*|2' "$all_consumer/obj/Debug/net8.0/material-symbols/requests.manifest"
cmp "$repository_root/third_party/google/material-symbols/fonts/Rounded.ttf" \
    "$all_consumer/obj/Debug/net8.0/material-symbols/fonts/Rounded.ttf"

echo "Material Symbols package integration tests passed."
