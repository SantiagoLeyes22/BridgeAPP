[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0.0',

    [string]$Publisher = 'CN=Bridge Test Publisher'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts\msix'))
$layoutPath = [System.IO.Path]::GetFullPath((Join-Path $artifactRoot 'layout'))
$projectPath = Join-Path $repoRoot 'Bridge\Bridge.csproj'
$manifestTemplate = Join-Path $repoRoot 'packaging\AppxManifest.xml'
$installTemplate = Join-Path $repoRoot 'packaging\INSTALL.template.md'
$sourceIcon = Join-Path $repoRoot 'Bridge\Assets\Bridge.png'
$packagePath = Join-Path $artifactRoot "Bridge_${Version}_x64.msix"
$certificatePath = Join-Path $artifactRoot 'Bridge-TestCertificate.cer'
$installerScriptPath = Join-Path $artifactRoot 'Install-Bridge.ps1'
$installReadmePath = Join-Path $artifactRoot 'INSTALL.md'
$transferBundlePath = Join-Path $artifactRoot "Bridge_${Version}_x64_test-bundle.zip"
$temporaryPfxPath = Join-Path ([System.IO.Path]::GetTempPath()) ("Bridge-Msix-" + [Guid]::NewGuid().ToString('N') + '.pfx')
$temporaryPassword = [Guid]::NewGuid().ToString('N')

if (-not $layoutPath.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean an MSIX layout outside the artifact directory: $layoutPath"
}

$windowsKitBin = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [Version]$_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'x64' } |
    Where-Object { (Test-Path (Join-Path $_ 'makeappx.exe')) -and (Test-Path (Join-Path $_ 'signtool.exe')) } |
    Select-Object -First 1

if (-not $windowsKitBin) {
    throw 'The Windows SDK tools makeappx.exe and signtool.exe were not found.'
}

$makeAppx = Join-Path $windowsKitBin 'makeappx.exe'
$signTool = Join-Path $windowsKitBin 'signtool.exe'

[System.IO.Directory]::CreateDirectory($artifactRoot) | Out-Null
if (Test-Path -LiteralPath $layoutPath) {
    Remove-Item -LiteralPath $layoutPath -Recurse -Force
}
[System.IO.Directory]::CreateDirectory($layoutPath) | Out-Null

try {
    Write-Host 'Publishing Bridge as a self-contained x64 application...'
    & dotnet publish $projectPath `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $layoutPath `
        -p:Platform=x64 `
        -p:PublishSingleFile=false `
        -p:DebugSymbols=false `
        -p:DebugType=None
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $manifest = [System.IO.File]::ReadAllText($manifestTemplate)
    $manifest = $manifest.Replace('@@VERSION@@', $Version).Replace('@@PUBLISHER@@', $Publisher)
    [System.IO.File]::WriteAllText(
        (Join-Path $layoutPath 'AppxManifest.xml'),
        $manifest,
        [System.Text.UTF8Encoding]::new($false))

    $assetPath = Join-Path $layoutPath 'Assets'
    & (Join-Path $PSScriptRoot 'Create-MsixAssets.ps1') -SourcePath $sourceIcon -OutputDirectory $assetPath

    if (Test-Path -LiteralPath $packagePath) {
        Remove-Item -LiteralPath $packagePath -Force
    }

    Write-Host 'Packing the MSIX...'
    & $makeAppx pack /d $layoutPath /p $packagePath /o
    if ($LASTEXITCODE -ne 0) {
        throw "makeappx failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Creating an isolated test-signing certificate...'
    $rsa = [System.Security.Cryptography.RSA]::Create(3072)
    try {
        $distinguishedName = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new($Publisher)
        $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
            $distinguishedName,
            $rsa,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256,
            [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

        $enhancedKeyUsages = [System.Security.Cryptography.OidCollection]::new()
        [void]$enhancedKeyUsages.Add([System.Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3'))
        $request.CertificateExtensions.Add(
            [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
                $false,
                $false,
                0,
                $true))
        $request.CertificateExtensions.Add(
            [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($enhancedKeyUsages, $true))
        $request.CertificateExtensions.Add(
            [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,
                $true))

        $certificate = $request.CreateSelfSigned(
            [DateTimeOffset]::UtcNow.AddMinutes(-5),
            [DateTimeOffset]::UtcNow.AddYears(3))
        try {
            [System.IO.File]::WriteAllBytes(
                $temporaryPfxPath,
                $certificate.Export(
                    [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx,
                    $temporaryPassword))
            [System.IO.File]::WriteAllBytes(
                $certificatePath,
                $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))

            Write-Host 'Signing the MSIX...'
            & $signTool sign /fd SHA256 /f $temporaryPfxPath /p $temporaryPassword $packagePath
            if ($LASTEXITCODE -ne 0) {
                throw "signtool failed with exit code $LASTEXITCODE."
            }

            Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging\Install-Bridge.ps1') -Destination $installerScriptPath -Force
            $installReadme = [System.IO.File]::ReadAllText($installTemplate)
            $installReadme = $installReadme.Replace('@@VERSION@@', $Version).Replace('@@THUMBPRINT@@', $certificate.Thumbprint)
            [System.IO.File]::WriteAllText($installReadmePath, $installReadme, [System.Text.UTF8Encoding]::new($false))

            if (Test-Path -LiteralPath $transferBundlePath) {
                Remove-Item -LiteralPath $transferBundlePath -Force
            }
            Compress-Archive -LiteralPath @(
                $packagePath,
                $certificatePath,
                $installerScriptPath,
                $installReadmePath) -DestinationPath $transferBundlePath -CompressionLevel Optimal

            Write-Host "MSIX: $packagePath"
            Write-Host "Transfer bundle: $transferBundlePath"
            Write-Host "Certificate: $certificatePath"
            Write-Host "Certificate thumbprint: $($certificate.Thumbprint)"
        }
        finally {
            $certificate.Dispose()
        }
    }
    finally {
        $rsa.Dispose()
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryPfxPath) {
        Remove-Item -LiteralPath $temporaryPfxPath -Force
    }
}
