[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param()

$ErrorActionPreference = 'Stop'
$workspaceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\')
$workspacePrefix = $workspaceRoot + '\'
if (-not (Test-Path -LiteralPath (Join-Path $workspaceRoot 'YouthDataManager.slnx'))) {
    throw 'Run this script from the YouthDataManager repository.'
}
if (-not (Test-Path -LiteralPath (Join-Path $workspaceRoot 'docs/verification-results.json'))) {
    throw 'Retain the final verification record before removing generated evidence.'
}

# This allowlist deliberately excludes current dependencies, appsettings, publish
# profiles, credentials, persistent application photos, source and test baselines.
$relativeTargets = @(
    'artifacts'
    '5.FrontEnd/AdminApp/.angular'
    '5.FrontEnd/ServantApp/.angular'
    '5.FrontEnd/AdminApp/dist'
    '5.FrontEnd/ServantApp/dist'
    '5.FrontEnd/pages'
    '5.FrontEnd/playwright-report'
    '5.FrontEnd/test-results'
    '5.FrontEnd/node_modules.pre-workspace'
    '5.FrontEnd/AdminApp/node_modules.pre-workspace'
    '5.FrontEnd/ServantApp/node_modules.pre-workspace'
    '5.FrontEnd/package-lock.pre-workspace.json'
)
$projectDirectories = @(
    '1.Domain/YouthDataManager.Domain'
    '2.Infrastructure/YouthDataManager.Infrastructure'
    '3.Services/YouthDataManager.Services.Abstractions'
    '3.Services/YouthDataManager.Services.DependencyInjection'
    '3.Services/YouthDataManager.Services.Implementations'
    '4.WebApis/YouthDataManager.WebApi'
    'tests/YouthDataManager.IntegrationTests'
    'tools/YouthDataManager.Benchmarks'
    'tools/YouthDataManager.RecoveryVerification'
)
foreach ($projectDirectory in $projectDirectories) {
    $relativeTargets += Join-Path $projectDirectory 'bin'
    $relativeTargets += Join-Path $projectDirectory 'obj'
}
$emptyTargets = @('pages', '5.FrontEnd/AdminApp/node_modules', '5.FrontEnd/ServantApp/node_modules')

function Get-CheckedPath([string] $relative) {
    $absolute = [IO.Path]::GetFullPath((Join-Path $workspaceRoot $relative))
    if (-not $absolute.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Target escapes the workspace: $absolute"
    }
    if (Test-Path -LiteralPath $absolute) {
        $resolved = (Resolve-Path -LiteralPath $absolute).ProviderPath
        if (-not $resolved.Equals($absolute, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unexpected resolved path: $absolute"
        }
        $ancestor = Split-Path -Parent $absolute
        while ($ancestor -ne $workspaceRoot) {
            $parentItem = Get-Item -LiteralPath $ancestor -Force
            if (($parentItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Linked parent directory is not allowed: $ancestor"
            }
            $ancestor = Split-Path -Parent $ancestor
        }
        $item = Get-Item -LiteralPath $absolute -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Linked cleanup root is not allowed: $absolute"
        }
        return $item
    }
}

$targets = @($relativeTargets | ForEach-Object { Get-CheckedPath $_ })
$emptyDirectories = @($emptyTargets | ForEach-Object { Get-CheckedPath $_ })
$trackedPaths = @(git -C $workspaceRoot ls-files --full-name)
if ($LASTEXITCODE -ne 0) { throw 'Cannot validate the tracked-file inventory.' }
foreach ($target in @($targets) + @($emptyDirectories)) {
    $gitRelative = $target.FullName.Substring($workspacePrefix.Length).Replace('\', '/')
    foreach ($tracked in $trackedPaths) {
        if ($tracked -eq $gitRelative -or $tracked.StartsWith($gitRelative + '/', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Tracked file found inside a cleanup target: $tracked"
        }
    }
}

$allowedLinks = @{}
$allowedLinks[[IO.Path]::GetFullPath((Join-Path $workspaceRoot '5.FrontEnd/node_modules.pre-workspace/admin-app'))] = Join-Path $workspaceRoot '5.FrontEnd/AdminApp'
$allowedLinks[[IO.Path]::GetFullPath((Join-Path $workspaceRoot '5.FrontEnd/node_modules.pre-workspace/servant-app'))] = Join-Path $workspaceRoot '5.FrontEnd/ServantApp'
$allowedLinks[[IO.Path]::GetFullPath((Join-Path $workspaceRoot '5.FrontEnd/node_modules.pre-workspace/@ydm/shared'))] = Join-Path $workspaceRoot '5.FrontEnd/shared'

# Walk explicitly without following links. Validate every link before any deletion.
$linksByRoot = @{}
foreach ($target in $targets | Where-Object { $_.PSIsContainer }) {
    $links = @()
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push($target.FullName)
    while ($pending.Count -gt 0) {
        foreach ($child in Get-ChildItem -LiteralPath ($pending.Pop()) -Force) {
            if (($child.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                if (-not $allowedLinks.ContainsKey($child.FullName) -or $child.LinkType -ne 'Junction') {
                    throw "Unexpected nested link: $($child.FullName)"
                }
                $destinations = @($child.Target)
                if ($destinations.Count -ne 1 -or
                    -not [IO.Path]::GetFullPath($destinations[0]).Equals(
                        [IO.Path]::GetFullPath($allowedLinks[$child.FullName]),
                        [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Unexpected junction destination: $($child.FullName)"
                }
                $links += $child.FullName
            } elseif ($child.PSIsContainer) {
                $pending.Push($child.FullName)
            }
        }
    }
    $linksByRoot[$target.FullName] = $links
}

Write-Output "Validated $($targets.Count) generated targets. Stop running builds/tests before cleanup."
$removed = 0
foreach ($target in $targets) {
    if ($PSCmdlet.ShouldProcess($target.FullName, 'Remove generated files or obsolete dependency backup')) {
        if ($target.PSIsContainer) {
            foreach ($junction in @($linksByRoot[$target.FullName])) {
                # Non-recursive Directory.Delete removes the junction itself, never its source target.
                [IO.Directory]::Delete($junction)
            }
        }
        Remove-Item -LiteralPath $target.FullName -Recurse -Force
        $removed++
    }
}
foreach ($directory in $emptyDirectories) {
    if (@(Get-ChildItem -LiteralPath $directory.FullName -Force).Count -eq 0 -and
        $PSCmdlet.ShouldProcess($directory.FullName, 'Remove empty obsolete directory')) {
        Remove-Item -LiteralPath $directory.FullName -Force
        $removed++
    }
}
Write-Output "Removed $removed generated/obsolete paths. Current workspace dependencies and source remain."
