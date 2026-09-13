param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9.-]+$')]
    [string] $CertificateHost,

    [string] $OutputDirectory = 'secrets/nginx-local'
)

$ErrorActionPreference = 'Stop'

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::Combine($PSScriptRoot, '..', $OutputDirectory))
[System.IO.Directory]::CreateDirectory($resolvedOutputDirectory) | Out-Null

$rsa = [System.Security.Cryptography.RSA]::Create(2048)

try {
    $distinguishedName = [System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new(
        "CN=$CertificateHost")
    $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        $distinguishedName,
        $rsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

    $request.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
            $false,
            $false,
            0,
            $true))
    $request.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature -bor
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment,
            $true))

    $serverAuthentication = [System.Security.Cryptography.OidCollection]::new()
    $serverAuthentication.Add([System.Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1')) | Out-Null
    $request.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new(
            $serverAuthentication,
            $false))

    $subjectAlternativeName = [System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
    $ipAddress = $null

    if ([System.Net.IPAddress]::TryParse($CertificateHost, [ref] $ipAddress)) {
        $subjectAlternativeName.AddIpAddress($ipAddress)
    }
    else {
        $subjectAlternativeName.AddDnsName($CertificateHost)
    }

    $request.CertificateExtensions.Add($subjectAlternativeName.Build())
    $request.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new(
            $request.PublicKey,
            $false))

    $notBefore = [System.DateTimeOffset]::UtcNow.AddMinutes(-5)
    $notAfter = $notBefore.AddYears(1)
    $certificate = $request.CreateSelfSigned($notBefore, $notAfter)

    try {
        $certificatePath = [System.IO.Path]::Combine($resolvedOutputDirectory, 'tls.crt')
        $privateKeyPath = [System.IO.Path]::Combine($resolvedOutputDirectory, 'tls.key')
        $certificatePem = [System.Security.Cryptography.PemEncoding]::WriteString(
            'CERTIFICATE',
            $certificate.RawData)
        $privateKeyPem = [System.Security.Cryptography.PemEncoding]::WriteString(
            'PRIVATE KEY',
            $rsa.ExportPkcs8PrivateKey())

        [System.IO.File]::WriteAllText($certificatePath, $certificatePem)
        [System.IO.File]::WriteAllText($privateKeyPath, $privateKeyPem)

        Write-Output "Created $certificatePath and $privateKeyPath for $CertificateHost."
    }
    finally {
        $certificate.Dispose()
    }
}
finally {
    $rsa.Dispose()
}
