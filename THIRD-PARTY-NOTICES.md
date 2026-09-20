# Third-party notices

Bridge does not bundle translation model weights. Offline model files are downloaded only after the user chooses an engine.

## Offline standard engine

- **BergamotTranslatorSharp** — MPL-2.0. C# wrapper and native Bergamot binaries. Source: https://github.com/freesia/BergamotTranslatorSharp
- **Bergamot Translator / Marian** — MPL-2.0 / MIT components used by the wrapper. Sources: https://github.com/browsermt/bergamot-translator and https://github.com/marian-nmt/marian
- **Mozilla Firefox Translations models** — MPL-2.0. Downloaded from Mozilla's public model registry and stored in the current user's local application data. Source and model information: https://github.com/mozilla/translations
- **SearchPioneer.Lingua** — Apache-2.0. Offline language identification. Source: https://github.com/searchpioneer/lingua-dotnet

The application preserves these notices and does not modify the downloaded Mozilla model files. The covered source is available from the linked upstream repositories.

## Optional TranslateGemma engines

- **Ollama** — optional external local runtime. Source: https://github.com/ollama/ollama
- **TranslateGemma** — downloaded by Ollama only after the user accepts Google's Gemma Terms of Use: https://ai.google.dev/gemma/terms

Bridge does not redistribute Ollama or TranslateGemma. Their respective terms apply independently.

## Microsoft components

The application also uses Microsoft Windows App SDK, MSAL.NET, dependency injection, HTTP, and related .NET components under their respective Microsoft/NuGet license terms. Microsoft 365 Copilot is an optional service and is not required by either offline engine.

This notice is informational and is not legal advice. Organizations should review the exact dependency and model versions used in a release as part of their normal software-compliance process.
