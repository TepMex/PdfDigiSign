$cert = New-SelfSignedCertificate -certstorelocation cert:\localmachine\my -dnsname tepmex.github.io
$pwd = ConvertTo-SecureString -String ‘password1234’ -Force -AsPlainText

$path = ‘cert:\localMachine\my\’ + $cert.thumbprint

Export-PfxCertificate -cert $path -FilePath cert.pfx -Password $pwd