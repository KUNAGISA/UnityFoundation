#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet run --project ../Tools/GameplayTags.Generator/GameplayTags.Generator.csproj -c Release -- --input GameplayTags.xml --template Template/gameplay-tags/catalog.sbn --output ../Assets/Gameplay/Tables/Runtime/Tags/Generated/GameplayTags.cs --template Template/gameplay-tags/constalias.sbn --output Defines/gameplaytags.xml
