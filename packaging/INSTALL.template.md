# Bridge @@VERSION@@ — instalación de prueba / test installation / instalação de teste

Este paquete contiene Bridge para Windows x64 y sus runtimes. No incluye Ollama
ni ningún modelo TranslateGemma.

This package contains Bridge for Windows x64 and its runtimes. It does not
include Ollama or any TranslateGemma model.

## Español — instalación gráfica

1. Confirmá que `Bridge_@@VERSION@@_x64.msix` y `Bridge-TestCertificate.cer`
   provienen de la misma Release oficial.
2. Abrí `Bridge-TestCertificate.cer` y elegí **Instalar certificado**.
3. Seleccioná **Equipo local** y aceptá el permiso de administrador.
4. Elegí **Colocar todos los certificados en el siguiente almacén**.
5. Seleccioná **Personas de confianza** y terminá el asistente.
6. Abrí el archivo `.msix`, seleccioná **Instalar** y luego abrí Bridge desde
   el menú Inicio.

Huella SHA-1 esperada del certificado:

`@@THUMBPRINT@@`

Como alternativa, hacé clic derecho en `Install-Bridge.ps1`, seleccioná
**Ejecutar con PowerShell** y aceptá el permiso de administrador. El script
confía temporalmente en el certificado, instala Bridge y retira esa confianza.

El certificado es solamente para pruebas. Instalalo únicamente si descargaste
estos archivos del repositorio oficial de Bridge. Una versión pública deberá
usar la firma de Microsoft Store o un certificado público de firma de código.

Si elegís TranslateGemma, Bridge abre la página oficial de Ollama. Ollama se
instala por separado y el modelo se descarga después de aceptar sus términos.
El motor offline estándar no requiere Ollama.

## English — graphical installation

1. Confirm that `Bridge_@@VERSION@@_x64.msix` and `Bridge-TestCertificate.cer`
   came from the same official Release.
2. Open `Bridge-TestCertificate.cer` and select **Install Certificate**.
3. Select **Local Machine** and approve the administrator prompt.
4. Select **Place all certificates in the following store**.
5. Choose **Trusted People** and finish the wizard.
6. Open the `.msix`, select **Install**, and then launch Bridge from the Start
   menu.

Expected certificate SHA-1 thumbprint:

`@@THUMBPRINT@@`

Alternatively, right-click `Install-Bridge.ps1`, select **Run with PowerShell**,
and approve the administrator prompt. The script temporarily trusts the
certificate, installs Bridge, and removes that trust.

This certificate is for testing only. Install it only when these files were
downloaded from the official Bridge repository. A public release must use
Microsoft Store signing or a publicly trusted code-signing certificate.

If you select TranslateGemma, Bridge opens the official Ollama page. Ollama is
installed separately, and the model is downloaded after accepting its terms.
The standard offline engine does not require Ollama.

## Português (Brasil) — instalação pela interface gráfica

1. Confirme que `Bridge_@@VERSION@@_x64.msix` e `Bridge-TestCertificate.cer`
   vieram da mesma versão publicada oficialmente.
2. Abra `Bridge-TestCertificate.cer` e selecione **Instalar Certificado**.
3. Selecione **Computador Local** e autorize a solicitação de administrador.
4. Selecione **Colocar todos os certificados no repositório a seguir**.
5. Escolha **Pessoas Confiáveis** e conclua o assistente.
6. Abra o arquivo `.msix`, selecione **Instalar** e inicie o Bridge pelo menu
   Iniciar.

Impressão digital SHA-1 esperada do certificado:

`@@THUMBPRINT@@`

Como alternativa, clique com o botão direito em `Install-Bridge.ps1`, selecione
**Executar com PowerShell** e autorize a solicitação de administrador. O script
confia temporariamente no certificado, instala o Bridge e depois remove essa
confiança.

Este certificado se destina apenas a testes. Instale-o somente se os arquivos
foram baixados do repositório oficial do Bridge. Uma versão pública deverá usar
a assinatura da Microsoft Store ou um certificado público de assinatura de
código.

Se você escolher TranslateGemma, o Bridge abrirá a página oficial do Ollama. O
Ollama é instalado separadamente e o modelo é baixado depois que seus termos
são aceitos. O mecanismo offline padrão não exige Ollama.
