#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
bash gen-tags.sh

WORKSPACE=..
LUBAN_DLL=$WORKSPACE/Tools/Luban/Luban.dll
CONF_ROOT=.

echo "."
echo "Validate Datas"
dotnet "$LUBAN_DLL" \
    -t all \
    -f \
    --validationFailAsError \
    --conf "$CONF_ROOT/Datas/luban.conf"

echo "."
echo "Generate Datas"

dotnet "$LUBAN_DLL" \
    -t all \
    -c cs-bin \
    -d bin \
    --validationFailAsError \
    --customTemplateDir Template \
    --conf "$CONF_ROOT/Datas/luban.conf" \
    -x outputDataDir="$WORKSPACE/Assets/Gameplay/Tables/Data/Config" \
    -x outputCodeDir="$WORKSPACE/Assets/Gameplay/Tables/Generated" \
    -x tableImporter.tableNameFormat={0}Table \
    -x tableImporter.valueTypeNameFormat={0}Entry \
    -x namingConvention.field=camel

generate_localization() {
    echo "."
    echo "Generate Localization $1"
    dotnet "$LUBAN_DLL" \
        -t all \
        -d bin \
        --validationFailAsError \
        --conf "$CONF_ROOT/Localizations/luban.conf" \
        -x outputDataDir="$WORKSPACE/Assets/Gameplay/Tables/Data/Localization/$1" \
        --variant TextEntry.text="$1"
}

generate_localization zh
generate_localization en
