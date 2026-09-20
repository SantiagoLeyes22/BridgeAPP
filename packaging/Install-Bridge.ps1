[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdministrator) {
    Write-Host 'Administrator approval is required to trust the MSIX test certificate temporarily.'
    $process = Start-Process powershell.exe `
        -Verb RunAs `
        -Wait `
        -PassThru `
        -ArgumentList @(
            '-NoProfile',
            '-ExecutionPolicy',
            'Bypass',
            '-File',
            ('"' + $MyInvocation.MyCommand.Path + '"'))
    exit $process.ExitCode
}

$packageDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$certificatePath = Join-Path $packageDirectory 'Bridge-TestCertificate.cer'
$packages = @(Get-ChildItem -LiteralPath $packageDirectory -Filter 'Bridge_*_x64.msix' -File)

if (-not (Test-Path -LiteralPath $certificatePath)) {
    throw "Bridge-TestCertificate.cer was not found next to this script."
}

if ($packages.Count -ne 1) {
    throw "Expected exactly one Bridge_*_x64.msix package next to this script."
}

$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
Write-Host "Certificate thumbprint: $($certificate.Thumbprint)"
$trustedCertificatePath = "Cert:\LocalMachine\TrustedPeople\$($certificate.Thumbprint)"
$addedTemporaryTrust = -not (Test-Path -LiteralPath $trustedCertificatePath)

try {
    if ($addedTemporaryTrust) {
        Write-Host 'Temporarily trusting the Bridge test certificate in Local Machine > Trusted People.'
        Import-Certificate -FilePath $certificatePath -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
    }

    Write-Host "Installing $($packages[0].Name)..."
    Add-AppxPackage -Path $packages[0].FullName
    Write-Host 'Bridge was installed. Open it from the Windows Start menu.'
}
finally {
    if ($addedTemporaryTrust -and (Test-Path -LiteralPath $trustedCertificatePath)) {
        Remove-Item -LiteralPath $trustedCertificatePath -Force
        Write-Host 'The temporary certificate trust was removed.'
    }
}
