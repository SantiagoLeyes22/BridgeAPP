# Bridge for Windows

[Español](#español) · [English](#english) · [Português (Brasil)](#português-brasil) · [Build and development](#build-and-development)

## Español

Bridge es una aplicación nativa para Windows 10/11 que traduce el texto seleccionado sin obligarte a salir de la aplicación en la que estás trabajando. Utiliza atajos globales, el portapapeles de Windows y una ventana flotante compacta construida con WinUI 3.

La aplicación puede trabajar de forma local y privada. No guarda un historial de traducciones, no realiza capturas de pantalla y solamente lee el portapapeles al activar un atajo, una acción desde el icono de la bandeja o al seleccionar texto mientras la ventana flotante está abierta.

### Descargar la versión de prueba

La compilación actual es una versión de prueba x64 firmada con un certificado autofirmado:

- [Descargar el paquete completo recomendado](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge_1.0.1.0_x64_test-bundle.zip)
- [Descargar solamente el MSIX](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge_1.0.1.0_x64.msix)
- [Descargar el certificado de prueba](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge-TestCertificate.cer)

> **Importante:** este paquete todavía no utiliza un certificado público. Windows necesita que el certificado de prueba se instale manualmente antes de abrir el MSIX. Instalalo únicamente si descargaste los archivos desde este repositorio oficial.

### Instalación gráfica, sin PowerShell

1. Descargá `Bridge_1.0.1.0_x64.msix` y `Bridge-TestCertificate.cer` desde la misma Release.
2. Abrí `Bridge-TestCertificate.cer` y elegí **Instalar certificado**.
3. Seleccioná **Equipo local** y aceptá el permiso de administrador.
4. Elegí **Colocar todos los certificados en el siguiente almacén**.
5. Seleccioná **Personas de confianza** y terminá el asistente.
6. Abrí `Bridge_1.0.1.0_x64.msix` y seleccioná **Instalar**.
7. Buscá **Bridge** en el menú Inicio.

El ZIP también incluye `Install-Bridge.ps1` como alternativa automática. El script solicita permisos de administrador, confía temporalmente en el certificado, instala Bridge y luego retira esa confianza.

### Qué hace Bridge

- `Ctrl + Shift + T`: captura el texto seleccionado y lo traduce al idioma principal configurado o al último idioma de destino elegido en la ventana flotante. Las salidas en portugués usan portugués de Brasil (`pt-BR`).
- `Ctrl + Shift + Enter`: traduce una respuesta al inglés inicialmente o al último idioma de destino elegido en la ventana flotante. La elección se comparte con `Ctrl + Shift + T` y se conserva al reiniciar Bridge.
- La ventana flotante permite **Copiar**, **Reemplazar** o **Cerrar** el resultado.
- Mientras la ventana flotante está abierta, seleccionar otro texto con el mouse o el teclado actualiza la traducción en el mismo modo e idioma. Al cerrarla, la detección automática se detiene.
- Bridge nunca presiona Enviar, Enter, Responder ni Publicar por el usuario.
- El motor estándar usa modelos de Mozilla Firefox Translations con Bergamot y funciona sin conexión después de descargar los idiomas.
- Los motores TranslateGemma son opcionales y funcionan localmente mediante Ollama.

### Ollama y los modelos de IA

Ollama no está incluido en el MSIX porque su instalador y los modelos son considerablemente más grandes que Bridge. Si elegís TranslateGemma, Bridge te dirige a la [descarga oficial de Ollama](https://ollama.com/download/windows). Después de instalar y abrir Ollama, Bridge puede descargar el modelo seleccionado.

El motor offline estándar no necesita Ollama, una cuenta, una clave de API ni una licencia de Microsoft 365.

### Privacidad y requisitos

- Windows 10 versión 2004 o posterior, o Windows 11, x64.
- Los motores offline procesan el texto seleccionado completamente en la PC.
- Bridge no contiene telemetría ni analítica y no almacena el texto traducido.
- Los diagnósticos se guardan sin el texto seleccionado o traducido en `%LOCALAPPDATA%\Bridge\Logs\diagnostic.log`.
- TranslateGemma se comunica solamente con Ollama en `127.0.0.1:11434`.
- La primera descarga de un modelo requiere conexión a Internet.

Ningún sistema de traducción automática garantiza resultados perfectos. Los textos legales, médicos, financieros o relacionados con la seguridad deben ser revisados por una persona.

---

## English

Bridge is a native Windows 10/11 application that translates selected text without making you leave the application in which you are working. It uses global shortcuts, the Windows clipboard, and a compact WinUI 3 overlay.

The application can operate locally and privately. It does not store translation history or take screenshots. It reads the clipboard when the user activates a shortcut or tray action, or selects text while the overlay is open.

### Download the test build

The current x64 test build is signed with a self-signed certificate:

- [Download the recommended complete bundle](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge_1.0.1.0_x64_test-bundle.zip)
- [Download only the MSIX](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge_1.0.1.0_x64.msix)
- [Download the test certificate](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge-TestCertificate.cer)

> **Important:** this package does not yet use a publicly trusted certificate. Windows requires the test certificate to be installed manually before opening the MSIX. Install it only when both files were downloaded from this official repository.

### Graphical installation without PowerShell

1. Download `Bridge_1.0.1.0_x64.msix` and `Bridge-TestCertificate.cer` from the same Release.
2. Open `Bridge-TestCertificate.cer` and select **Install Certificate**.
3. Select **Local Machine** and approve the administrator prompt.
4. Select **Place all certificates in the following store**.
5. Choose **Trusted People** and finish the wizard.
6. Open `Bridge_1.0.1.0_x64.msix` and select **Install**.
7. Open **Bridge** from the Start menu.

The ZIP also contains `Install-Bridge.ps1` as an automated alternative. The script requests administrator permission, temporarily trusts the certificate, installs Bridge, and then removes that trust.

### What Bridge does

- `Ctrl + Shift + T`: captures selected text and translates it into the configured primary language or the most recently chosen target language in the overlay. Portuguese output targets Brazilian Portuguese (`pt-BR`).
- `Ctrl + Shift + Enter`: translates a selected response into English initially or the most recently chosen target language in the overlay. The choice is shared with `Ctrl + Shift + T` and persists after restarting Bridge.
- The overlay provides **Copy**, **Replace**, and **Close** actions.
- While the overlay is open, selecting more text with the mouse or keyboard updates the translation in the same mode and language. Closing the overlay stops automatic detection.
- Bridge never presses Send, Enter, Reply, or Submit for the user.
- The standard engine uses Mozilla Firefox Translations models with Bergamot and works offline after the language models are downloaded.
- TranslateGemma engines are optional and run locally through Ollama.

### Ollama and AI models

Ollama is not included in the MSIX because its installer and models are considerably larger than Bridge. If you select TranslateGemma, Bridge directs you to the [official Ollama download](https://ollama.com/download/windows). After installing and starting Ollama, Bridge can download the selected model.

The standard offline engine does not require Ollama, an account, an API key, or a Microsoft 365 license.

### Privacy and requirements

- Windows 10 version 2004 or later, or Windows 11, x64.
- Offline engines process selected text entirely on the PC.
- Bridge contains no telemetry or analytics and does not store translated text.
- Diagnostics are written without selected or translated text to `%LOCALAPPDATA%\Bridge\Logs\diagnostic.log`.
- TranslateGemma communicates only with Ollama on `127.0.0.1:11434`.
- The first model download requires an Internet connection.

No automatic translation system guarantees perfect output. Legal, medical, financial, or safety-related text should be reviewed by a person.

---

## Português (Brasil)

Bridge é um aplicativo nativo para Windows 10/11 que traduz o texto selecionado sem que você precise sair do aplicativo em que está trabalhando. Ele usa atalhos globais, a área de transferência do Windows e uma janela flutuante compacta criada com WinUI 3.

O aplicativo pode funcionar localmente e de forma privada. Ele não armazena um histórico de traduções nem faz capturas de tela. A área de transferência é lida quando o usuário aciona um atalho ou uma ação pelo ícone na bandeja do sistema, ou seleciona texto enquanto a janela flutuante está aberta.

### Baixar a versão de teste

A versão de teste atual para x64 é assinada com um certificado autoassinado:

- [Baixar o pacote completo recomendado](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge_1.0.1.0_x64_test-bundle.zip)
- [Baixar apenas o MSIX](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge_1.0.1.0_x64.msix)
- [Baixar o certificado de teste](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.1-test/Bridge-TestCertificate.cer)

> **Importante:** este pacote ainda não usa um certificado de uma autoridade confiável. O Windows exige a instalação manual do certificado de teste antes de abrir o MSIX. Instale-o somente se você baixou os arquivos deste repositório oficial.

### Instalação pela interface gráfica, sem PowerShell

1. Baixe `Bridge_1.0.1.0_x64.msix` e `Bridge-TestCertificate.cer` da mesma versão publicada.
2. Abra `Bridge-TestCertificate.cer` e selecione **Instalar Certificado**.
3. Selecione **Computador Local** e autorize a solicitação de administrador.
4. Selecione **Colocar todos os certificados no repositório a seguir**.
5. Escolha **Pessoas Confiáveis** e conclua o assistente.
6. Abra `Bridge_1.0.1.0_x64.msix` e selecione **Instalar**.
7. Procure **Bridge** no menu Iniciar.

O ZIP também inclui `Install-Bridge.ps1` como alternativa automática. O script solicita permissão de administrador, confia temporariamente no certificado, instala o Bridge e depois remove essa confiança.

### O que o Bridge faz

- `Ctrl + Shift + T`: captura o texto selecionado e o traduz para o idioma principal configurado ou para o último idioma de destino escolhido na janela flutuante. As traduções para português usam o português do Brasil (`pt-BR`).
- `Ctrl + Shift + Enter`: traduz uma resposta inicialmente para inglês ou para o último idioma de destino escolhido na janela flutuante. A escolha é compartilhada com `Ctrl + Shift + T` e permanece após reiniciar o Bridge.
- A janela flutuante permite **Copiar**, **Substituir** ou **Fechar** o resultado.
- Enquanto a janela flutuante estiver aberta, selecionar outro texto com o mouse ou o teclado atualizará a tradução no mesmo modo e idioma. Ao fechá-la, a detecção automática para.
- O Bridge nunca pressiona Enviar, Enter, Responder nem Publicar pelo usuário.
- O mecanismo padrão usa modelos do Mozilla Firefox Translations com Bergamot e funciona sem conexão após o download dos idiomas.
- Os mecanismos TranslateGemma são opcionais e funcionam localmente por meio do Ollama.

### Ollama e modelos de IA

O Ollama não está incluído no MSIX porque seu instalador e seus modelos são consideravelmente maiores que o Bridge. Se você escolher TranslateGemma, o Bridge direcionará você para o [download oficial do Ollama](https://ollama.com/download/windows). Depois de instalar e iniciar o Ollama, o Bridge poderá baixar o modelo selecionado.

O mecanismo offline padrão não exige Ollama, conta, chave de API nem licença do Microsoft 365.

### Privacidade e requisitos

- Windows 10 versão 2004 ou posterior, ou Windows 11, x64.
- Os mecanismos offline processam o texto selecionado inteiramente no computador.
- O Bridge não inclui telemetria nem análise de uso e não armazena o texto traduzido.
- Os diagnósticos são salvos sem o texto selecionado ou traduzido em `%LOCALAPPDATA%\Bridge\Logs\diagnostic.log`.
- O TranslateGemma se comunica apenas com o Ollama em `127.0.0.1:11434`.
- O primeiro download de um modelo exige conexão com a Internet.

Nenhum sistema de tradução automática garante resultados perfeitos. Textos jurídicos, médicos, financeiros ou relacionados à segurança devem ser revisados por uma pessoa.

## Build and development

### Translation engines

| Engine | Download | Recommended hardware | Account | Offline after setup |
| --- | ---: | --- | --- | --- |
| Offline standard | Approximately 150–250 MB per core language pack | 4 GB RAM, 2 logical cores, 1 GB free | None | Yes |
| TranslateGemma 4B | Approximately 3.3 GB plus Ollama | 12 GB RAM, 4 logical cores, 6 GB free; 4 GB VRAM optional | None | Yes |
| TranslateGemma 12B | Approximately 8.1 GB plus Ollama | 24 GB RAM, 8 logical cores, 12 GB free; 10 GB VRAM recommended | None | Yes |
| TranslateGemma 27B | Approximately 17 GB plus Ollama | 32 GB RAM, 12 logical cores, 24 GB free; 20 GB VRAM recommended | None | Yes |
| Microsoft 365 Copilot | None locally | 4 GB RAM | Work account, tenant approval, and Copilot license | No |

Spanish-to-Brazilian-Portuguese and Portuguese-to-Spanish translations use English as a pivot in the standard engine. Mozilla's model identifiers remain the generic `pt`, while Bridge presents and requests Brazilian Portuguese (`pt-BR`). TranslateGemma handles all supported directions in one model. Microsoft 365 Copilot remains an optional engine for organizations that have already configured, approved, and licensed it.

### Technology

- C# / .NET 8
- Windows App SDK / WinUI 3
- BergamotTranslatorSharp and Mozilla Firefox Translations models
- SearchPioneer.Lingua for offline language detection
- Ollama local API for optional TranslateGemma models
- MSAL.NET/WAM and Microsoft Graph beta for the optional Copilot mode

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) before redistribution.

### Build and test from source

```powershell
dotnet restore .\Bridge.sln
dotnet build .\Bridge.sln -c Release -p:Platform=x64
dotnet run --project .\Bridge.Tests\Bridge.Tests.csproj -c Release -p:Platform=x64
```

### Build a test MSIX

```powershell
.\tools\Build-Msix.ps1 -Version 1.0.1.0
```

The script publishes a self-contained x64 build, creates the MSIX assets, packages the application, and signs it with an isolated test certificate. Output is written to `artifacts\msix`. Public releases must replace this generated certificate with Microsoft Store signing or a publicly trusted code-signing certificate.

### Optional Copilot configuration

Copilot is not required for onboarding or offline translation. For a controlled organizational pilot, configure `MicrosoftIdentity.ClientId` in `Bridge/appsettings.json`, register the WAM desktop redirect URI, grant the required delegated Microsoft Graph permissions and tenant consent, and assign a Copilot license to each user.

The Copilot Chat API currently used by this project is a Microsoft Graph `/beta` API. The implementation creates a fresh conversation for every translation and disables web grounding on each request.
