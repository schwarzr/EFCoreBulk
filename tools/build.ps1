Import-Module -Name "./Build-Versioning.psm1"


$projects = "..\src\Extensions.EntityFrameworkCore.SqlServer.Bulk\Extensions.EntityFrameworkCore.SqlServer.Bulk.csproj"

New-NugetPackages `
    -Projects $projects `
    -NugetServerUrl "http://www.nuget.org/api/v2" `
    -VersionPackage "Extensions.EntityFrameworkCore.SqlServer.Bulk" `
    -VersionFilePath "..\version-ef1.json" `
    -OutputPath "..\dist\nuget\" `
    -MsBuildParams "EfVersion=1;SourceLinkCreate=true;SignAssembly=true;AssemblyOriginatorKeyFile=..\..\private\signkey.snk"

New-NugetPackages `
    -Projects $projects `
    -NugetServerUrl "http://www.nuget.org/api/v2" `
    -VersionPackage "Extensions.EntityFrameworkCore.SqlServer.Bulk" `
    -VersionFilePath "..\version-ef2.json" `
    -OutputPath "..\dist\nuget\" `
    -MsBuildParams "EfVersion=2;SourceLinkCreate=true;SignAssembly=true;AssemblyOriginatorKeyFile=..\..\private\signkey.snk" `
    -DoNotCleanOutput