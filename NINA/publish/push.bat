cd /D "%~dp0"
for /r %%i in (*.nupkg) do nuget push %%i %1 -Source https://api.nuget.org/v3/index.json