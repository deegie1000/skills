@echo off
dotnet build ..\FS.Plugins\FS.Plugins.csproj -c Debug
dotnet tool run spkl plugins ..\FS.Plugins\spkl.json %*
