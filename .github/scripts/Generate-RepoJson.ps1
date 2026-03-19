param(
    [Parameter(Mandatory = $true)]
    [string]$TemplatePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$AssetName,

    [Parameter(Mandatory = $false)]
    [string]$Changelog = "Release build"
)

$repoEntries = Get-Content $TemplatePath -Raw | ConvertFrom-Json
if ($repoEntries.Count -lt 1) {
    throw "Template repository JSON does not contain any entries."
}

$entry = $repoEntries[0]
$repoUrl = "https://github.com/$Repository"
$downloadUrl = "$repoUrl/releases/download/$Tag/$AssetName"
$unixTimestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

$entry.RepoUrl = $repoUrl
$entry.AssemblyVersion = $Version
$entry.TestingAssemblyVersion = $Version
$entry.LastUpdate = $unixTimestamp
$entry.DownloadLinkInstall = $downloadUrl
$entry.DownloadLinkTesting = $downloadUrl
$entry.DownloadLinkUpdate = $downloadUrl
$entry.Changelog = $Changelog

$parentDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parentDirectory)) {
    New-Item -ItemType Directory -Force -Path $parentDirectory | Out-Null
}

@($entry) | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding utf8
