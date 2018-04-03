nuget push ..\dist\nuget\*.symbols.nupkg %MYGETAPIKEY% -Source https://www.myget.org/F/codeworx/api/v2/package

del ..\dist\nuget\*.symbols.nupkg

nuget push ..\dist\nuget\*.nupkg %NUGETAPIKEY% -Source https://www.nuget.org/api/v2/package

del ..\dist\nuget\*.nupkg