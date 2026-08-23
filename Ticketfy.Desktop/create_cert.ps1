$Subject = "CN=Valcore Enterprise Systems, O=Valcore, C=MX"
$Cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $Subject -KeyUsage DigitalSignature -FriendlyName "Valcore Code Signing Certificate" -NotAfter (Get-Date).AddYears(5) -CertStoreLocation "Cert:\CurrentUser\My"

$Pwd = ConvertTo-SecureString -String "Valcore2026!" -Force -AsPlainText
$PfxPath = Join-Path $PSScriptRoot "TicketfyCodeSigning.pfx"

Export-PfxCertificate -Cert $Cert -FilePath $PfxPath -Password $Pwd -Force
Import-PfxCertificate -FilePath $PfxPath -CertStoreLocation "Cert:\CurrentUser\Root" -Password $Pwd -ErrorAction SilentlyContinue

Write-Host "Certificado de Firma de Código oficial de VALCORE generado y exportado a: $PfxPath" -ForegroundColor Green
