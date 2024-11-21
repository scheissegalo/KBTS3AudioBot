@echo off
setlocal enabledelayedexpansion

REM Set the plugin folder (the current directory)
set PLUGINFOLDER=%cd%

REM Create a temporary folder to store the DLLs
set TEMP_FOLDER=%PLUGINFOLDER%\temp_publish
if exist %TEMP_FOLDER% rd /s /q %TEMP_FOLDER%
mkdir %TEMP_FOLDER%

REM Loop through each subdirectory in the plugin folder
for /d %%D in (%PLUGINFOLDER%\*) do (
    REM Check if the subdirectory contains a .csproj file
    if exist "%%D\*.csproj" (
        echo Found .csproj in %%D, publishing...
        
        REM Get the plugin folder name (this will be used as the DLL name)
        set PLUGIN_NAME=%%~nxD
        
        REM Navigate to the subdirectory
        pushd %%D
        
        REM Execute dotnet publish for each .csproj found
        dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
        
        REM Check if the output folder exists
        if exist "bin\Release\net6.0\win-x64" (
            REM Copy the DLL file with the plugin folder name into the temporary folder
            echo Copying DLL from bin\Release\net6.0\win-x64 to the temporary folder...
            copy "bin\Release\net6.0\win-x64\!PLUGIN_NAME!.dll" "%TEMP_FOLDER%\"
        )
        
        REM Return to the plugin folder
        popd
    ) else (
        echo No .csproj found in %%D
    )
)

REM Now zip all the DLL files in the temporary folder
echo Zipping all DLL files...
powershell -Command "Compress-Archive -Path '%TEMP_FOLDER%\*.dll' -DestinationPath '%PLUGINFOLDER%\published_plugins.zip'"

REM Clean up the temporary folder
echo Cleaning up...
rd /s /q %TEMP_FOLDER%

echo All DLL files have been published and zipped into published_plugins.zip.
