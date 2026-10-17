param(
    [ValidateSet('x64', 'arm64', 'all')][string]$Architecture = 'all',
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version = '1.0.0',
    [ValidatePattern('^3\.13\.\d+$')][string]$PythonVersion = '3.13.13'
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $root 'artifacts/cache'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $cache, $dist -Force | Out-Null

function Download([string]$Url, [string]$Destination) {
    if (Test-Path -LiteralPath $Destination) { return }
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Invoke-WebRequest $Url -OutFile ($Destination + '.download') -MaximumRetryCount 4 -RetryIntervalSec 2
            break
        }
        catch { if ($attempt -eq 5) { throw }; Start-Sleep -Seconds 2 }
    }
    Move-Item -LiteralPath ($Destination + '.download') -Destination $Destination
}
function Check-Exit([string]$Action) {
    if ($LASTEXITCODE -ne 0) { throw "$Action failed (exit $LASTEXITCODE)." }
}

$nsis = Join-Path $root 'artifacts/tools/nsis-3.12'
if (-not (Test-Path (Join-Path $nsis 'makensis.exe'))) {
    $archive = Join-Path $cache 'nsis-3.12.zip'
    Download 'https://netix.dl.sourceforge.net/project/nsis/NSIS%203/3.12/nsis-3.12.zip' $archive
    Expand-Archive -LiteralPath $archive -DestinationPath (Split-Path $nsis) -Force
}
$targets = if ($Architecture -eq 'all') { @('x64', 'arm64') } else { @($Architecture) }
foreach ($arch in $targets) {
    $rid = "win-$arch"
    $platform = if ($arch -eq 'arm64') { 'ARM64' } else { 'x64' }
    $payload = Join-Path $root "artifacts/publish/$rid"
    & dotnet publish (Join-Path $root 'UI/UI.csproj') -c Release -r $rid "-p:Platform=$platform" `
        -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true `
        -p:PublishTrimmed=false -p:DebugType=None "-p:Version=$Version" -o $payload -v:minimal
    Check-Exit "Publish $rid"

    $pythonArch = if ($arch -eq 'arm64') { 'arm64' } else { 'amd64' }
    $pythonArchive = Join-Path $cache "python-$PythonVersion-$pythonArch.zip"
    Download "https://www.python.org/ftp/python/$PythonVersion/python-$PythonVersion-embed-$pythonArch.zip" $pythonArchive
    $python = Join-Path $payload 'Runtime/Python'
    Expand-Archive -LiteralPath $pythonArchive -DestinationPath $python -Force
    # Explicit isolated search paths: no system Python, PATH or developer site-packages.
    Set-Content -LiteralPath (Join-Path $python 'python313._pth') -Encoding ascii -Value @(
        'python313.zip', '.', 'Lib/site-packages', 'import site'
    )
    $wheelPlatform = if ($arch -eq 'arm64') { 'win_arm64' } else { 'win_amd64' }
    & python -m pip install --upgrade --target (Join-Path $python 'Lib/site-packages') `
        --platform $wheelPlatform --python-version 3.13 --implementation cp --abi cp313 `
        --only-binary=:all: --no-compile --disable-pip-version-check -r (Join-Path $PSScriptRoot 'requirements.txt')
    Check-Exit "Python dependencies $rid"
    foreach ($required in @('Tomography.exe', 'coreclr.dll', 'Microsoft.UI.Xaml.dll',
        'Assets/AppIcon.ico', 'Assets/BrandAppIcon.png', 'Runtime/Python/python313.dll', 'Runtime/Python/python313.zip')) {
        if (-not (Test-Path -LiteralPath (Join-Path $payload $required))) { throw "Missing distribution file: $required" }
    }

    $redist = Join-Path $cache "vc_redist-v14.$arch.exe"
    Download "https://aka.ms/vc14/vc_redist.$arch.exe" $redist
    $signature = Get-AuthenticodeSignature -LiteralPath $redist
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
        throw 'Visual C++ Runtime signature validation failed.'
    }

    # Delete only installed files on uninstall; preserve projects/configuration and unrelated user files.
    $manifest = Join-Path $root "artifacts/uninstall-$arch.nsh"
    $files = Get-ChildItem -LiteralPath $payload -Recurse -File | Where-Object { $_.FullName -notmatch '\\(tests|__pycache__)\\' }
    $lines = @($files | ForEach-Object {
        $relative = $_.FullName.Substring($payload.Length + 1).Replace('$', '$$')
        'Delete "$INSTDIR\' + $relative + '"'
    })
    $lines += Get-ChildItem -LiteralPath $payload -Recurse -Directory | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
        'RMDir "$INSTDIR\' + $_.FullName.Substring($payload.Length + 1).Replace('$', '$$') + '"'
    }
    Set-Content -LiteralPath $manifest -Value $lines -Encoding utf8
    $output = Join-Path $dist "FieldSync-$Version-$arch-Setup.exe"
    $vcVersion = (Get-Item -LiteralPath $redist).VersionInfo.ProductVersion
    & (Join-Path $nsis 'makensis.exe') /V2 "/DARCH=$arch" "/DVERSION=$Version" "/DPAYLOAD=$payload" `
        "/DVCREDIST=$redist" "/DVCVERSION=$vcVersion" "/DUNINSTALL_FILES=$manifest" "/DOUTPUT=$output" (Join-Path $PSScriptRoot 'FieldSync.nsi')
    Check-Exit "Installer $arch"
    $hash = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath ($output + '.sha256') -Value "$hash  $([IO.Path]::GetFileName($output))" -Encoding ascii
    Write-Host "Created: $output"
}
