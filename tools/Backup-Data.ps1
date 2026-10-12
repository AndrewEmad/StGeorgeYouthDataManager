param([Parameter(Mandatory)][string]$Server,[Parameter(Mandatory)][string]$Database,[Parameter(Mandatory)][string]$PhotoDirectory,[Parameter(Mandatory)][string]$BackupDirectory)
$ErrorActionPreference='Stop'
if($Database -notmatch '^[A-Za-z0-9_.-]{1,128}$'){throw 'Invalid database name.'}
$photoRoot=(Resolve-Path -LiteralPath $PhotoDirectory).Path
$backupRoot=[IO.Path]::GetFullPath($BackupDirectory)
if($backupRoot.StartsWith($photoRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $backupRoot -eq $photoRoot){throw 'Backup destination must be outside the photo directory.'}
if(Get-ChildItem -LiteralPath $photoRoot -Recurse -Attributes ReparsePoint){throw 'Photo directory contains links; review them before backup.'}
$bundle=Join-Path $backupRoot ('ydm-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $bundle -Force | Out-Null
$databaseFile=Join-Path $bundle 'database.bak'
$sqlFile=Join-Path $bundle 'backup.sql'
$literal=$databaseFile.Replace("'","''")
@"
BACKUP DATABASE [$Database] TO DISK=N'$literal' WITH COPY_ONLY, CHECKSUM;
RESTORE VERIFYONLY FROM DISK=N'$literal' WITH CHECKSUM;
"@ | Set-Content -LiteralPath $sqlFile -Encoding UTF8
& sqlcmd -S $Server -E -b -r 1 -i $sqlFile
if($LASTEXITCODE -ne 0){throw 'SQL backup or verification failed. Photo backup has not started.'}
$photoCopy=Join-Path $bundle 'photos'
New-Item -ItemType Directory -Path $photoCopy | Out-Null
$files=@()
foreach($file in Get-ChildItem -LiteralPath $photoRoot -File -Recurse){
    $relative=$file.FullName.Substring($photoRoot.TrimEnd('\').Length+1)
    $destination=Join-Path $photoCopy $relative
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
    $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if($hash -ne (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash){throw "Photo changed during backup: $relative"}
    $files+=@{Path=$relative;Sha256=$hash}
}
@{CapturedUtc=[datetime]::UtcNow.ToString('o');Database=$Database;DatabaseSha256=(Get-FileHash -LiteralPath $databaseFile -Algorithm SHA256).Hash;Photos=$files;VerifiedSqlChecksum=$true;RestorationVerified=$false} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $bundle 'manifest.json') -Encoding UTF8
Write-Output "Database and photo backup created: $bundle"
