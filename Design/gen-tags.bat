@echo off
setlocal
pushd "%~dp0"
dotnet run --project "../Tools/GameplayTags.Generator/GameplayTags.Generator.csproj" -c Release -- --input "GameplayTags.xml" --template "Template/gameplay-tags/catalog.sbn" --output "../Assets/Gameplay/Tables/Runtime/Tags/Generated/GameplayTags.cs" --template "Template/gameplay-tags/constalias.sbn" --output "Defines/gameplaytags.xml"
set TAG_RESULT=%ERRORLEVEL%
popd
exit /b %TAG_RESULT%
