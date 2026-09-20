# Bridge for Windows

[Español](#español) · [English](#english) · [Build and development](#build-and-development)

## Español

Bridge es una aplicación nativa para Windows 10/11 que traduce el texto seleccionado sin obligarte a salir de la aplicación en la que estás trabajando. Utiliza atajos globales, el portapapeles de Windows y una ventana flotante compacta construida con WinUI 3.

La aplicación puede trabajar de forma local y privada. No guarda un historial de traducciones, no realiza capturas de pantalla y solamente lee el portapapeles cuando el usuario activa un atajo o una acción desde el icono de la bandeja.

### Descargar la versión de prueba

La compilación actual es una versión de prueba x64 firmada con un certificado autofirmado:

- [Descargar el paquete completo recomendado](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.0-test/Bridge_1.0.0.0_x64_test-bundle.zip)
- [Descargar solamente el MSIX](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.0-test/Bridge_1.0.0.0_x64.msix)
- [Descargar el certificado de prueba](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.0-test/Bridge-TestCertificate.cer)

> **Importante:** este paquete todavía no utiliza un certificado público. Windows necesita que el certificado de prueba se instale manualmente antes de abrir el MSIX. Instalalo únicamente si descargaste los archivos desde este repositorio oficial.

### Instalación gráfica, sin PowerShell

1. Descargá `Bridge_1.0.0.0_x64.msix` y `Bridge-TestCertificate.cer` desde la misma Release.
2. Abrí `Bridge-TestCertificate.cer` y elegí **Instalar certificado**.
3. Seleccioná **Equipo local** y aceptá el permiso de administrador.
4. Elegí **Colocar todos los certificados en el siguiente almacén**.
5. Seleccioná **Personas de confianza** y terminá el asistente.
6. Abrí `Bridge_1.0.0.0_x64.msix` y seleccioná **Instalar**.
7. Buscá **Bridge** en el menú Inicio.

El ZIP también incluye `Install-Bridge.ps1` como alternativa automática. El script solicita permisos de administrador, confía temporalmente en el certificado, instala Bridge y luego retira esa confianza.

### Qué hace Bridge

- `Ctrl + Shift + T`: captura el texto seleccionado, detecta localmente si está en inglés, español o portugués y lo traduce al idioma principal configurado.
- `Ctrl + Shift + Enter`: traduce una respuesta al último idioma extranjero detectado.
- La ventana flotante permite **Copiar**, **Reemplazar** o **Cerrar** el resultado.
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

The application can operate locally and privately. It does not store translation history, take screenshots, or read the clipboard unless the user activates a shortcut or tray action.

### Download the test build

The current x64 test build is signed with a self-signed certificate:

- [Download the recommended complete bundle](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.0-test/Bridge_1.0.0.0_x64_test-bundle.zip)
- [Download only the MSIX](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.0-test/Bridge_1.0.0.0_x64.msix)
- [Download the test certificate](https://github.com/SantiagoLeyes22/BridgeAPP/releases/download/v1.0.0-test/Bridge-TestCertificate.cer)

> **Important:** this package does not yet use a publicly trusted certificate. Windows requires the test certificate to be installed manually before opening the MSIX. Install it only when both files were downloaded from this official repository.

### Graphical installation without PowerShell

1. Download `Bridge_1.0.0.0_x64.msix` and `Bridge-TestCertificate.cer` from the same Release.
2. Open `Bridge-TestCertificate.cer` and select **Install Certificate**.
3. Select **Local Machine** and approve the administrator prompt.
4. Select **Place all certificates in the following store**.
5. Choose **Trusted People** and finish the wizard.
6. Open `Bridge_1.0.0.0_x64.msix` and select **Install**.
7. Open **Bridge** from the Start menu.

The ZIP also contains `Install-Bridge.ps1` as an automated alternative. The script requests administrator permission, temporarily trusts the certificate, installs Bridge, and then removes that trust.

### What Bridge does

- `Ctrl + Shift + T`: captures the selected text, locally detects English, Spanish, or Portuguese, and translates it into the configured primary language.
- `Ctrl + Shift + Enter`: translates a selected response into the most recently detected foreign language.
- The overlay provides **Copy**, **Replace**, and **Close** actions.
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

## Build and development

### Translation engines

| Engine | Download | Recommended hardware | Account | Offline after setup |
| --- | ---: | --- | --- | --- |
| Offline standard | Approximately 150–250 MB per core language pack | 4 GB RAM, 2 logical cores, 1 GB free | None | Yes |
| TranslateGemma 4B | Approximately 3.3 GB plus Ollama | 12 GB RAM, 4 logical cores, 6 GB free; 4 GB VRAM optional | None | Yes |
| TranslateGemma 12B | Approximately 8.1 GB plus Ollama | 24 GB RAM, 8 logical cores, 12 GB free; 10 GB VRAM recommended | None | Yes |
| TranslateGemma 27B | Approximately 17 GB plus Ollama | 32 GB RAM, 12 logical cores, 24 GB free; 20 GB VRAM recommended | None | Yes |
| Microsoft 365 Copilot | None locally | 4 GB RAM | Work account, tenant approval, and Copilot license | No |

Spanish-to-Portuguese and Portuguese-to-Spanish translations use English as a pivot in the standard engine. TranslateGemma handles all supported directions in one model. Microsoft 365 Copilot remains an optional engine for organizations that have already configured, approved, and licensed it.

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
.\tools\Build-Msix.ps1 -Version 1.0.0.0
```

The script publishes a self-contained x64 build, creates the MSIX assets, packages the application, and signs it with an isolated test certificate. Output is written to `artifacts\msix`. Public releases must replace this generated certificate with Microsoft Store signing or a publicly trusted code-signing certificate.

### Optional Copilot configuration

Copilot is not required for onboarding or offline translation. For a controlled organizational pilot, configure `MicrosoftIdentity.ClientId` in `Bridge/appsettings.json`, register the WAM desktop redirect URI, grant the required delegated Microsoft Graph permissions and tenant consent, and assign a Copilot license to each user.

The Copilot Chat API currently used by this project is a Microsoft Graph `/beta` API. The implementation creates a fresh conversation for every translation and disables web grounding on each request.
