[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $KeyStore,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_.-]+$')]
    [string] $KeyAlias,
    [Parameter(Mandatory = $true)]
    [string] $StorePasswordFile,
    [string] $KeyPasswordFile,
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'CoffeeNap/CoffeeNap.csproj'
[xml] $project = Get-Content -LiteralPath $projectPath -Raw
$version = $project.SelectSingleNode('/Project/PropertyGroup/ApplicationDisplayVersion').InnerText
$applicationId = $project.SelectSingleNode('/Project/PropertyGroup/ApplicationId').InnerText
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts/android/$version"
}
if (-not $KeyPasswordFile) {
    $KeyPasswordFile = $StorePasswordFile
}

foreach ($inputFile in @($KeyStore, $StorePasswordFile, $KeyPasswordFile)) {
    if (-not (Test-Path -LiteralPath $inputFile -PathType Leaf)) {
        throw "Required signing file does not exist: $inputFile"
    }
}
$KeyStore = (Resolve-Path -LiteralPath $KeyStore).Path
$StorePasswordFile = (Resolve-Path -LiteralPath $StorePasswordFile).Path
$KeyPasswordFile = (Resolve-Path -LiteralPath $KeyPasswordFile).Path
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)

# Runs a .NET command and stops the release process on failure.
function Invoke-DotNet {
    param([string[]] $CommandArguments)
    & dotnet @CommandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$signedApk = Join-Path $OutputDirectory "$applicationId-Signed.apk"
if (Test-Path -LiteralPath $signedApk) {
    throw "An APK already exists in $OutputDirectory. Select a new output directory to preserve it."
}

# Re-sign the build output even after an earlier failed signing attempt.
$cachedSignedApk = Join-Path $repositoryRoot "CoffeeNap/bin/Release/net10.0-android/$applicationId-Signed.apk"
if (Test-Path -LiteralPath $cachedSignedApk -PathType Leaf) {
    Remove-Item -LiteralPath $cachedSignedApk
}

# Separate environment values avoid apksigner consuming the same password file twice.
$previousStorePassword = $env:COFFEENAP_RELEASE_STORE_PASSWORD
$previousKeyPassword = $env:COFFEENAP_RELEASE_KEY_PASSWORD
try {
    $env:COFFEENAP_RELEASE_STORE_PASSWORD = [IO.File]::ReadAllText($StorePasswordFile).TrimEnd("`r", "`n")
    $env:COFFEENAP_RELEASE_KEY_PASSWORD = [IO.File]::ReadAllText($KeyPasswordFile).TrimEnd("`r", "`n")
    if (-not $env:COFFEENAP_RELEASE_STORE_PASSWORD -or -not $env:COFFEENAP_RELEASE_KEY_PASSWORD) {
        throw 'Signing password files must not be empty.'
    }
    Invoke-DotNet -CommandArguments @(
        'publish', $projectPath, '-f', 'net10.0-android', '-c', 'Release',
        '-p:TargetFrameworks=net10.0-android', '-p:AndroidPackageFormats=apk',
        '-p:AndroidKeyStore=true', "-p:AndroidSigningKeyStore=$KeyStore",
        "-p:AndroidSigningKeyAlias=$KeyAlias", '-p:AndroidSigningStorePass=env:COFFEENAP_RELEASE_STORE_PASSWORD',
        '-p:AndroidSigningKeyPass=env:COFFEENAP_RELEASE_KEY_PASSWORD', '-o', $OutputDirectory
    )
}
finally {
    $env:COFFEENAP_RELEASE_STORE_PASSWORD = $previousStorePassword
    $env:COFFEENAP_RELEASE_KEY_PASSWORD = $previousKeyPassword
}

if (-not (Test-Path -LiteralPath $signedApk -PathType Leaf)) {
    throw "Publishing did not produce the expected signed APK: $signedApk"
}

# Verify the actual APK signature before presenting the release as successful.
$sdkJson = & dotnet msbuild $projectPath '-p:TargetFramework=net10.0-android' `
    '-t:_ResolveSdks' '-getProperty:JavaSdkDirectory,AndroidSdkDirectory' '-v:quiet'
if ($LASTEXITCODE -ne 0) { throw 'Could not resolve Android signing verification tools.' }
$sdkPaths = ($sdkJson -join "`n" | ConvertFrom-Json).Properties
$java = Join-Path $sdkPaths.JavaSdkDirectory 'bin/java.exe'
$signer = Get-ChildItem -LiteralPath (Join-Path $sdkPaths.AndroidSdkDirectory 'build-tools') -Directory |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'lib/apksigner.jar' } |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1
if (-not $signer) { throw 'apksigner.jar was not found in the Android SDK.' }
& $java -jar $signer verify --verbose --print-certs $signedApk
if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed; do not distribute this artifact.' }
$hash = (Get-FileHash -LiteralPath $signedApk -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($signedApk))" |
    Set-Content -LiteralPath "$signedApk.sha256" -Encoding Ascii
Write-Host "Signed APK: $signedApk"
Write-Host "SHA-256: $hash"
