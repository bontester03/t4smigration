$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot

$env:DOTNET_CLI_HOME = (Resolve-Path ".\.dotnet").Path
$env:NUGET_PACKAGES = (Resolve-Path ".\.nuget").Path
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:ASPNETCORE_URLS = "http://localhost:5206"

dotnet run
