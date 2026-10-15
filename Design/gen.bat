echo off

cd /d "%~dp0"
call gen-tags.bat
if errorlevel 1 exit /b %errorlevel%

set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%/Tools/Luban/Luban.dll
set CONF_ROOT=.

echo .
echo Validate Datas
dotnet %LUBAN_DLL% ^
    -t all ^
    -f ^
    --validationFailAsError ^
    --conf %CONF_ROOT%/Datas/luban.conf
if errorlevel 1 exit /b %errorlevel%

echo .
echo Generate Datas

dotnet %LUBAN_DLL% ^
    -t all ^
    -c cs-bin ^
    -d bin ^
    --validationFailAsError ^
    --customTemplateDir Template ^
    --conf %CONF_ROOT%/Datas/luban.conf ^
    -x outputDataDir=%WORKSPACE%/Assets/Gameplay/Tables/Data/Config ^
    -x outputCodeDir=%WORKSPACE%/Assets/Gameplay/Tables/Generated ^
    -x tableImporter.tableNameFormat={0}Table ^
    -x tableImporter.valueTypeNameFormat={0}Entry ^
    -x namingConvention.field=camel
if errorlevel 1 exit /b %errorlevel%

call :GenerateLocalization zh
call :GenerateLocalization en

pause
goto :eof

:GenerateLocalization
echo .
echo Generate Localization %~1
dotnet %LUBAN_DLL% ^
    -t all ^
    -d bin ^
    --validationFailAsError ^
    --conf %CONF_ROOT%/Localizations/luban.conf ^
    -x outputDataDir=%WORKSPACE%/Assets/Gameplay/Tables/Data/Localization/%~1 ^
    --variant TextEntry.text=%~1
if errorlevel 1 exit /b %errorlevel%
goto :eof
