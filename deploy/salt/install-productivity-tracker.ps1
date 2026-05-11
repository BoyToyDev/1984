param(
    [Parameter(Mandatory = $true)]
    [string]$SourceExe,

    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\1984",

    [string]$ConfigPath,

    [string]$ExitPassword
)

$targetExe = Join-Path $InstallDir '1984.exe'
$targetConfig = Join-Path $InstallDir '1984.config.json'

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Force -Path $SourceExe -Destination $targetExe

if ($ConfigPath) {
    Copy-Item -Force -Path $ConfigPath -Destination $targetConfig
}
elseif ($ExitPassword) {
    $iterations = 100000
    $salt = New-Object byte[] 16
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($salt)
    $deriveBytes = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
        $ExitPassword,
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

    $config | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -Path $targetConfig
}
elseif (-not (Test-Path $targetConfig)) {
    Copy-Item -Force -Path (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '1984.config.example.json') -Destination $targetConfig
}

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Set-ItemProperty -Path $runKey -Name '1984' -Value "`"$targetExe`""

Start-Process -FilePath $targetExe
Write-Host "1984 installed to $targetExe"
