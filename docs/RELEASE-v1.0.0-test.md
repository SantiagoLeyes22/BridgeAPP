# Bridge 1.0.0 — versión de prueba / test release

## Español

Bridge traduce texto seleccionado directamente sobre cualquier aplicación de
Windows mediante una ventana flotante. El motor estándar procesa las
traducciones de forma local después de descargar los modelos. TranslateGemma
es opcional y requiere instalar Ollama por separado.

### Descarga

Se recomienda descargar `Bridge_1.0.0.0_x64_test-bundle.zip`, que contiene:

- `Bridge_1.0.0.0_x64.msix`
- `Bridge-TestCertificate.cer`
- `Install-Bridge.ps1`
- `INSTALL.md`

### Instalación sin PowerShell

1. Abrí `Bridge-TestCertificate.cer`.
2. Elegí **Instalar certificado > Equipo local**.
3. Colocalo en el almacén **Personas de confianza**.
4. Abrí `Bridge_1.0.0.0_x64.msix` y seleccioná **Instalar**.

El certificado es autofirmado y sirve únicamente para esta versión de prueba.
Instalalo sólo si descargaste los archivos desde este repositorio oficial.

### Verificación

- Huella SHA-1 del certificado: `394C9680C86513514DE4F97B69F4CD40A888370D`
- SHA-256 del MSIX: `1CFB7D87529641B5E4838709144900C43929DB45480F27263C131F689A8250E9`
- SHA-256 del certificado: `7BED89231ED358526D8CBFEA0A5155D38172006DE4856FD9A3DD0D85F795D814`
- SHA-256 del ZIP: `D5BDC0D2E421AB39753AB5F0CF4BADA7BC0EF45A81FACF4F3C1C187B8CCF9A03`

## English

Bridge translates selected text directly over any Windows application through
a compact overlay. The standard engine processes translations locally after
its models are downloaded. TranslateGemma is optional and requires Ollama to
be installed separately.

### Download

The recommended download is `Bridge_1.0.0.0_x64_test-bundle.zip`, containing:

- `Bridge_1.0.0.0_x64.msix`
- `Bridge-TestCertificate.cer`
- `Install-Bridge.ps1`
- `INSTALL.md`

### Installation without PowerShell

1. Open `Bridge-TestCertificate.cer`.
2. Select **Install Certificate > Local Machine**.
3. Place it in the **Trusted People** certificate store.
4. Open `Bridge_1.0.0.0_x64.msix` and select **Install**.

The certificate is self-signed and intended only for this test build. Install
it only when the files were downloaded from this official repository.

### Verification

- Certificate SHA-1 thumbprint: `394C9680C86513514DE4F97B69F4CD40A888370D`
- MSIX SHA-256: `1CFB7D87529641B5E4838709144900C43929DB45480F27263C131F689A8250E9`
- Certificate SHA-256: `7BED89231ED358526D8CBFEA0A5155D38172006DE4856FD9A3DD0D85F795D814`
- ZIP SHA-256: `D5BDC0D2E421AB39753AB5F0CF4BADA7BC0EF45A81FACF4F3C1C187B8CCF9A03`
