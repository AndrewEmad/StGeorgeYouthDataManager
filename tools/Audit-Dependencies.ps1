[CmdletBinding()]
param([string]$Project = '4.WebApis/YouthDataManager.WebApi', [string]$Output = 'artifacts/nuget-audit-final.json')

$ErrorActionPreference = 'Stop'
$auditText = (& dotnet list $Project package --vulnerable --include-transitive --no-restore --format json) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'NuGet dependency audit failed.' }
$audit = $auditText | ConvertFrom-Json
$outputPath = [System.IO.Path]::GetFullPath($Output)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
[System.IO.File]::WriteAllText($outputPath, $auditText, [System.Text.UTF8Encoding]::new($false))
$unresolved = @()
foreach ($projectResult in $audit.projects) {
    foreach ($framework in $projectResult.frameworks) {
        foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
            if ($null -eq $package -or @($package.vulnerabilities).Count -eq 0) { continue }
            if ($package.id -eq 'SixLabors.ImageSharp' -and $package.resolvedVersion -eq '3.1.5') {
                Write-Warning 'ImageSharp 3.1.5 remains explicitly pinned by the user; its upstream advisories remain unresolved and visible.'
                continue
            }
            $unresolved += "$($package.id) $($package.resolvedVersion)"
        }
    }
}
if ($unresolved.Count -gt 0) { throw ('Unresolved dependency advisories: ' + (($unresolved | Sort-Object -Unique) -join ', ')) }
Write-Output 'Dependency audit passed with only the explicit ImageSharp 3.1.5 exception.'
