param(
    [Parameter(Mandatory = $true)]
    [string]$Password,

    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) '1984.config.json')
)

$ErrorActionPreference = 'Stop'
$iterations = 100000
$salt = New-Object byte[] 16
[System.Security.Cryptography.RandomNumberGenerator]::Fill($salt)
$deriveBytes = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
    $Password,
    $salt,
    $iterations,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256
)
$hash = $deriveBytes.GetBytes(32)
$deriveBytes.Dispose()

$config = [ordered]@{
    productName = '1984'
    companyName = 'Organization'
    dataDirectory = '%APPDATA%\\1984'
    activeWindowPollIntervalSeconds = 2
    screenshotIntervalSeconds = 300
    idleThresholdSeconds = 180
    browserReceiverPort = 39877
    retentionDays = 90
    allowExitWithoutPassword = $false
    exitPasswordHashBase64 = [Convert]::ToBase64String($hash)
    exitPasswordSaltBase64 = [Convert]::ToBase64String($salt)
    exitPasswordIterations = $iterations
}

$config | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $OutputPath
Write-Host "Config written to $OutputPath"
