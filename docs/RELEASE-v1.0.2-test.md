# Bridge 1.0.2 — versión de prueba / test release / versão de teste

## Español

Esta versión corrige la separación entre los idiomas de lectura y escritura:

- `Ctrl + Shift + T` siempre traduce al idioma principal configurado. Si la
  configuración no contiene un idioma válido, utiliza español.
- `Ctrl + Shift + Enter` conserva el último idioma de destino elegido para
  escribir respuestas, incluso después de reiniciar Bridge.
- Cambiar el idioma de una traducción recibida ya no modifica la preferencia
  utilizada para escribir respuestas.
- Mientras la ventana flotante permanece abierta, las nuevas selecciones
  conservan el idioma elegido durante esa sesión.

### Descarga y actualización

Se recomienda descargar `Bridge_1.0.2.0_x64_test-bundle.zip`. Cerrá Bridge,
extraé todo el ZIP y ejecutá `Install-Bridge.ps1` con PowerShell. El script
solicita permiso de administrador, confía temporalmente en el nuevo certificado
de prueba, instala la actualización y luego retira esa confianza.

También podés instalar el certificado manualmente en **Equipo local >
Personas de confianza** y abrir `Bridge_1.0.2.0_x64.msix`. El paquete conserva
la identidad `Bridge.LocalTranslator` y aumenta su versión a `1.0.2.0`.

### Verificación

- Huella SHA-1 del certificado: `59FA278167BEA33A76688F94DE62D7A054D1A398`
- SHA-256 del MSIX: `E3F03FB92631E63C3D4C244FC5ECD8C29A31472E358493DE68368F11D0B9A720`
- SHA-256 del certificado: `C009077C5FE0C4221826D6FE96D8957C2CAA660BB6428E42FA7BA5E42A2EEFFC`
- SHA-256 del ZIP: `631DADF8199F7562A1E658F0078CD748FAA1DC5FF4BE6CBCDA12A7221906BD7F`

## English

This release separates the reading and writing language preferences:

- `Ctrl + Shift + T` always translates into the configured primary language.
  Spanish is used if the setting does not contain a valid language.
- `Ctrl + Shift + Enter` remembers the last target language selected for
  writing responses, including after Bridge restarts.
- Changing the language of an incoming translation no longer changes the
  preference used for writing responses.
- While the overlay remains open, new selections keep the language chosen for
  that session.

### Download and update

The recommended download is `Bridge_1.0.2.0_x64_test-bundle.zip`. Close Bridge,
extract the entire ZIP, and run `Install-Bridge.ps1` with PowerShell. The script
requests administrator permission, temporarily trusts the new test certificate,
installs the update, and then removes that trust.

You can also install the certificate manually under **Local Machine > Trusted
People** and open `Bridge_1.0.2.0_x64.msix`. The package keeps the
`Bridge.LocalTranslator` identity and increases its version to `1.0.2.0`.

### Verification

- Certificate SHA-1 thumbprint: `59FA278167BEA33A76688F94DE62D7A054D1A398`
- MSIX SHA-256: `E3F03FB92631E63C3D4C244FC5ECD8C29A31472E358493DE68368F11D0B9A720`
- Certificate SHA-256: `C009077C5FE0C4221826D6FE96D8957C2CAA660BB6428E42FA7BA5E42A2EEFFC`
- ZIP SHA-256: `631DADF8199F7562A1E658F0078CD748FAA1DC5FF4BE6CBCDA12A7221906BD7F`

## Português (Brasil)

Esta versão separa as preferências de idioma de leitura e escrita:

- `Ctrl + Shift + T` sempre traduz para o idioma principal configurado. O
  espanhol é usado se a configuração não contiver um idioma válido.
- `Ctrl + Shift + Enter` mantém o último idioma de destino escolhido para
  escrever respostas, inclusive depois de reiniciar o Bridge.
- Alterar o idioma de uma tradução recebida não modifica mais a preferência
  usada para escrever respostas.
- Enquanto a janela flutuante permanecer aberta, as novas seleções mantêm o
  idioma escolhido para essa sessão.

### Download e atualização

Recomendamos baixar `Bridge_1.0.2.0_x64_test-bundle.zip`. Feche o Bridge,
extraia todo o conteúdo do ZIP e execute `Install-Bridge.ps1` com o PowerShell.
O script solicita permissão de administrador, confia temporariamente no novo
certificado de teste, instala a atualização e depois remove essa confiança.

Você também pode instalar o certificado manualmente em **Computador Local >
Pessoas Confiáveis** e abrir `Bridge_1.0.2.0_x64.msix`. O pacote mantém a
identidade `Bridge.LocalTranslator` e aumenta sua versão para `1.0.2.0`.

### Verificação

- Impressão digital SHA-1 do certificado: `59FA278167BEA33A76688F94DE62D7A054D1A398`
- SHA-256 do MSIX: `E3F03FB92631E63C3D4C244FC5ECD8C29A31472E358493DE68368F11D0B9A720`
- SHA-256 do certificado: `C009077C5FE0C4221826D6FE96D8957C2CAA660BB6428E42FA7BA5E42A2EEFFC`
- SHA-256 do ZIP: `631DADF8199F7562A1E658F0078CD748FAA1DC5FF4BE6CBCDA12A7221906BD7F`
