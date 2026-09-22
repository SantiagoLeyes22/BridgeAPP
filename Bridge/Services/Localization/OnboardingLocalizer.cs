using Bridge.Models;

namespace Bridge.Services.Localization;

public static class OnboardingLocalizer
{
    public static string Text(string languageCode, string key)
    {
        var language = Normalize(languageCode);
        return Strings.TryGetValue(language, out var values) && values.TryGetValue(key, out var value)
            ? value
            : Strings["en"][key];
    }

    public static string EngineName(string languageCode, TranslationEngineDefinition engine) =>
        (Normalize(languageCode), engine.Id) switch
        {
            ("es", TranslationEngineDefinition.OfflineStandardId) => "Estándar sin conexión — recomendado",
            ("es", TranslationEngineDefinition.TranslateGemma4BId) => "Calidad mejorada sin conexión — TranslateGemma 4B",
            ("es", TranslationEngineDefinition.TranslateGemma12BId) => "Alta calidad sin conexión — TranslateGemma 12B",
            ("es", TranslationEngineDefinition.TranslateGemma27BId) => "Máxima calidad sin conexión — TranslateGemma 27B",
            ("es", TranslationEngineDefinition.CopilotId) => "Microsoft 365 Copilot — opcional",
            ("pt", TranslationEngineDefinition.OfflineStandardId) => "Padrão offline — recomendado",
            ("pt", TranslationEngineDefinition.TranslateGemma4BId) => "Qualidade aprimorada offline — TranslateGemma 4B",
            ("pt", TranslationEngineDefinition.TranslateGemma12BId) => "Alta qualidade offline — TranslateGemma 12B",
            ("pt", TranslationEngineDefinition.TranslateGemma27BId) => "Qualidade máxima offline — TranslateGemma 27B",
            ("pt", TranslationEngineDefinition.CopilotId) => "Microsoft 365 Copilot — opcional",
            _ => engine.DisplayName
        };

    public static string LanguageName(string languageCode, LanguageDefinition language) =>
        (Normalize(languageCode), language.Code) switch
        {
            ("es", "es") => "Español",
            ("es", "en") => "Inglés",
            ("es", "pt") => "Portugués (Brasil)",
            ("pt", "es") => "Espanhol",
            ("pt", "en") => "Inglês",
            ("pt", "pt") => "Português (Brasil)",
            _ => language.DisplayName
        };

    public static string EngineDescription(string languageCode, TranslationEngineDefinition engine) =>
        (Normalize(languageCode), engine.Id) switch
        {
            ("es", TranslationEngineDefinition.OfflineStandardId) => "Traducción privada y rápida para inglés, español y portugués con modelos Firefox/Bergamot.",
            ("es", TranslationEngineDefinition.TranslateGemma4BId) => "Mejor contexto y frases más naturales en portátiles empresariales modernos. Descarga aproximada: 3,3 GB.",
            ("es", TranslationEngineDefinition.TranslateGemma12BId) => "Mayor comprensión del contexto para traducciones exigentes. Descarga aproximada: 8,1 GB.",
            ("es", TranslationEngineDefinition.TranslateGemma27BId) => "La opción TranslateGemma más potente. Descarga aproximada: 17 GB; está pensada para estaciones de trabajo potentes.",
            ("es", TranslationEngineDefinition.CopilotId) => "Usa Microsoft 365 Copilot cuando la organización ya lo haya aprobado y licenciado.",
            ("pt", TranslationEngineDefinition.OfflineStandardId) => "Tradução privada e rápida para inglês, espanhol e português com modelos Firefox/Bergamot.",
            ("pt", TranslationEngineDefinition.TranslateGemma4BId) => "Melhor contexto e frases mais naturais em notebooks empresariais modernos. Download aproximado: 3,3 GB.",
            ("pt", TranslationEngineDefinition.TranslateGemma12BId) => "Maior compreensão de contexto para traduções exigentes. Download aproximado: 8,1 GB.",
            ("pt", TranslationEngineDefinition.TranslateGemma27BId) => "A opção TranslateGemma mais potente. Download aproximado: 17 GB; indicada para estações de trabalho potentes.",
            ("pt", TranslationEngineDefinition.CopilotId) => "Usa o Microsoft 365 Copilot quando a organização já o aprovou e licenciou.",
            _ => engine.ShortDescription
        };

    public static string EnginePrivacy(string languageCode, TranslationEngineDefinition engine)
    {
        var language = Normalize(languageCode);
        if (language == "en")
        {
            return engine.PrivacyDescription;
        }

        if (engine.UsesCopilot)
        {
            return language == "es"
                ? "El texto seleccionado se envía a Microsoft solamente cuando se solicita una traducción."
                : "O texto selecionado é enviado à Microsoft somente quando uma tradução é solicitada.";
        }

        return language == "es"
            ? engine.Id == TranslationEngineDefinition.OfflineStandardId
                ? "El texto permanece en este equipo. Internet se usa una sola vez para descargar el paquete de idiomas."
                : "El texto permanece en este equipo y se procesa mediante Ollama. Ollama es un componente externo y no viene incluido con Bridge."
            : engine.Id == TranslationEngineDefinition.OfflineStandardId
                ? "O texto permanece neste computador. A internet é usada uma única vez para baixar o pacote de idiomas."
                : "O texto permanece neste computador e é processado pelo Ollama. O Ollama é um componente externo e não está incluído no Bridge.";
    }

    public static string Requirements(string languageCode, TranslationEngineDefinition engine)
    {
        var language = Normalize(languageCode);
        if (language == "en")
        {
            return "Recommended hardware: " + engine.Requirements;
        }

        var gpu = (language, engine.Id) switch
        {
            ("es", TranslationEngineDefinition.TranslateGemma4BId) => "opcional; 4 GB de VRAM mejoran la velocidad",
            ("es", TranslationEngineDefinition.TranslateGemma12BId) => "10 GB de VRAM recomendados; por CPU será más lento",
            ("es", TranslationEngineDefinition.TranslateGemma27BId) => "20 GB de VRAM recomendados; por CPU será muy lento",
            ("es", _) => "no necesaria",
            ("pt", TranslationEngineDefinition.TranslateGemma4BId) => "opcional; 4 GB de VRAM melhoram a velocidade",
            ("pt", TranslationEngineDefinition.TranslateGemma12BId) => "10 GB de VRAM recomendados; por CPU será mais lento",
            ("pt", TranslationEngineDefinition.TranslateGemma27BId) => "20 GB de VRAM recomendados; por CPU será muito lento",
            _ => "não necessária"
        };

        return language == "es"
            ? $"Hardware recomendado: RAM: {engine.MinimumRamGb} GB o más · CPU: {engine.RecommendedCpuCores}+ núcleos lógicos · Disco libre: {engine.RequiredDiskGb} GB · GPU: {gpu}"
            : $"Hardware recomendado: RAM: {engine.MinimumRamGb} GB ou mais · CPU: {engine.RecommendedCpuCores}+ núcleos lógicos · Espaço livre: {engine.RequiredDiskGb} GB · GPU: {gpu}";
    }

    public static string HardwareSummary(string languageCode, HardwareProfile hardware)
    {
        return Normalize(languageCode) switch
        {
            "es" => $"Este equipo: {hardware.TotalRamGb:F1} GB de RAM · {hardware.LogicalProcessorCount} núcleos lógicos · {hardware.FreeDiskGb:F1} GB libres",
            "pt" => $"Este computador: {hardware.TotalRamGb:F1} GB de RAM · {hardware.LogicalProcessorCount} núcleos lógicos · {hardware.FreeDiskGb:F1} GB livres",
            _ => hardware.Summary
        };
    }

    public static string Compatibility(string languageCode, HardwareProfile hardware, TranslationEngineDefinition engine)
    {
        var language = Normalize(languageCode);
        if (language == "en")
        {
            return hardware.Evaluate(engine).Message;
        }

        var missing = new List<string>();
        if (hardware.TotalRamGb + 0.05 < engine.MinimumRamGb)
        {
            missing.Add($"{engine.MinimumRamGb} GB de RAM");
        }

        if (hardware.LogicalProcessorCount < engine.RecommendedCpuCores)
        {
            missing.Add(language == "es"
                ? $"{engine.RecommendedCpuCores} núcleos de CPU"
                : $"{engine.RecommendedCpuCores} núcleos de CPU");
        }

        if (hardware.FreeDiskGb + 0.05 < engine.RequiredDiskGb)
        {
            missing.Add(language == "es"
                ? $"{engine.RequiredDiskGb} GB de disco libre"
                : $"{engine.RequiredDiskGb} GB de espaço livre");
        }

        if (missing.Count == 0)
        {
            return language == "es"
                ? "Este equipo cumple la recomendación de RAM, CPU y disco."
                : "Este computador atende à recomendação de RAM, CPU e disco.";
        }

        return language == "es"
            ? "Por debajo de la recomendación: " + string.Join(", ", missing) + ". Puede intentarlo, pero podría funcionar lentamente o no cargar."
            : "Abaixo da recomendação: " + string.Join(", ", missing) + ". Você pode tentar, mas pode ficar lento ou não carregar.";
    }

    public static string Status(string languageCode, string message)
    {
        var language = Normalize(languageCode);
        if (language == "en")
        {
            return "Status: " + message;
        }

        var translated = message switch
        {
            "Offline language pack ready." => language == "es" ? "Paquete de idiomas sin conexión listo." : "Pacote de idiomas offline pronto.",
            "Download the offline language pack once before translating." => language == "es" ? "Descargue una vez el paquete de idiomas sin conexión antes de traducir." : "Baixe uma vez o pacote de idiomas offline antes de traduzir.",
            "Accept the Gemma terms before downloading this model." => language == "es" ? "Acepte los términos de Gemma antes de descargar este modelo." : "Aceite os termos do Gemma antes de baixar este modelo.",
            "Ollama is not running. Install or open Ollama, then prepare this engine." => language == "es" ? "Ollama no está en ejecución. Instálelo o ábralo y luego prepare este motor." : "O Ollama não está em execução. Instale-o ou abra-o e depois prepare este mecanismo.",
            "Copilot has not been configured by the application publisher." => language == "es" ? "El publicador de la aplicación no configuró Copilot." : "O editor do aplicativo não configurou o Copilot.",
            "Microsoft 365 Copilot is connected." => language == "es" ? "Microsoft 365 Copilot está conectado." : "O Microsoft 365 Copilot está conectado.",
            "Sign in with an approved and licensed work account." => language == "es" ? "Inicie sesión con una cuenta laboral aprobada y con licencia." : "Entre com uma conta corporativa aprovada e licenciada.",
            "Engine status could not be checked." => language == "es" ? "No se pudo comprobar el estado del motor." : "Não foi possível verificar o estado do mecanismo.",
            _ when message.EndsWith(" is installed and ready.", StringComparison.Ordinal) => language == "es" ? message.Replace(" is installed and ready.", " está instalado y listo.") : message.Replace(" is installed and ready.", " está instalado e pronto."),
            _ when message.StartsWith("Download ", StringComparison.Ordinal) => language == "es" ? message.Replace("Download ", "Descargue ").Replace(" once to use it offline.", " una vez para usarlo sin conexión.") : message.Replace("Download ", "Baixe ").Replace(" once to use it offline.", " uma vez para usá-lo offline."),
            _ => message
        };

        return (language == "es" ? "Estado: " : "Status: ") + translated;
    }

    private static string Normalize(string? languageCode) => languageCode is "es" or "pt" ? languageCode : "en";

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Strings { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>
            {
                ["SetupLanguage"] = "Setup language",
                ["WelcomeTitle"] = "Welcome",
                ["WelcomeDescription"] = "Translate messages privately without leaving the application you're working in. No account is required for offline engines.",
                ["Step1"] = "1. Select text.",
                ["Step2"] = "2. Press Ctrl + Shift + T.",
                ["Step3"] = "3. See the translation.",
                ["Step4"] = "4. Write and select your response.",
                ["Step5"] = "5. Press Ctrl + Shift + Enter.",
                ["Continue"] = "Continue",
                ["PrimaryTitle"] = "Primary language",
                ["PrimaryDescription"] = "Incoming messages will be translated into this language.",
                ["Language"] = "Language",
                ["EngineTitle"] = "Choose the translation engine",
                ["EngineIntro"] = "The standard offline engine is the easiest option. TranslateGemma improves contextual quality but needs more memory and a larger download.",
                ["Engine"] = "Translation engine",
                ["GemmaAccept"] = "I accept the Gemma Terms of Use for this local model.",
                ["GemmaRead"] = "Read the Gemma Terms of Use",
                ["Preparing"] = "Preparing engine…",
                ["InstallOllama"] = "Download Ollama (external)",
                ["SignIn"] = "Sign in with Microsoft",
                ["DownloadPack"] = "Download offline language pack",
                ["DownloadModel"] = "Download selected model",
                ["Finish"] = "Start using Bridge"
            },
            ["es"] = new Dictionary<string, string>
            {
                ["SetupLanguage"] = "Idioma de configuración",
                ["WelcomeTitle"] = "Bienvenido",
                ["WelcomeDescription"] = "Traduzca mensajes de forma privada sin salir de la aplicación en la que está trabajando. Los motores sin conexión no requieren una cuenta.",
                ["Step1"] = "1. Seleccione el texto.",
                ["Step2"] = "2. Presione Ctrl + Shift + T.",
                ["Step3"] = "3. Vea la traducción.",
                ["Step4"] = "4. Escriba y seleccione su respuesta.",
                ["Step5"] = "5. Presione Ctrl + Shift + Enter.",
                ["Continue"] = "Continuar",
                ["PrimaryTitle"] = "Idioma principal",
                ["PrimaryDescription"] = "Los mensajes recibidos se traducirán a este idioma.",
                ["Language"] = "Idioma",
                ["EngineTitle"] = "Elija el motor de traducción",
                ["EngineIntro"] = "El motor estándar sin conexión es la opción más sencilla. TranslateGemma mejora la calidad contextual, pero necesita más memoria y una descarga mayor.",
                ["Engine"] = "Motor de traducción",
                ["GemmaAccept"] = "Acepto los Términos de Uso de Gemma para este modelo local.",
                ["GemmaRead"] = "Leer los Términos de Uso de Gemma",
                ["Preparing"] = "Preparando el motor…",
                ["InstallOllama"] = "Descargar Ollama (externo)",
                ["SignIn"] = "Iniciar sesión con Microsoft",
                ["DownloadPack"] = "Descargar paquete de idiomas sin conexión",
                ["DownloadModel"] = "Descargar el modelo seleccionado",
                ["Finish"] = "Comenzar a usar Bridge"
            },
            ["pt"] = new Dictionary<string, string>
            {
                ["SetupLanguage"] = "Idioma da configuração",
                ["WelcomeTitle"] = "Bem-vindo",
                ["WelcomeDescription"] = "Traduza mensagens de forma privada sem sair do aplicativo em que está trabalhando. Os mecanismos offline não exigem uma conta.",
                ["Step1"] = "1. Selecione o texto.",
                ["Step2"] = "2. Pressione Ctrl + Shift + T.",
                ["Step3"] = "3. Veja a tradução.",
                ["Step4"] = "4. Escreva e selecione sua resposta.",
                ["Step5"] = "5. Pressione Ctrl + Shift + Enter.",
                ["Continue"] = "Continuar",
                ["PrimaryTitle"] = "Idioma principal",
                ["PrimaryDescription"] = "As mensagens recebidas serão traduzidas para este idioma.",
                ["Language"] = "Idioma",
                ["EngineTitle"] = "Escolha o mecanismo de tradução",
                ["EngineIntro"] = "O mecanismo padrão offline é a opção mais simples. O TranslateGemma melhora a qualidade contextual, mas precisa de mais memória e um download maior.",
                ["Engine"] = "Mecanismo de tradução",
                ["GemmaAccept"] = "Aceito os Termos de Uso do Gemma para este modelo local.",
                ["GemmaRead"] = "Ler os Termos de Uso do Gemma",
                ["Preparing"] = "Preparando o mecanismo…",
                ["InstallOllama"] = "Baixar Ollama (externo)",
                ["SignIn"] = "Entrar com a Microsoft",
                ["DownloadPack"] = "Baixar pacote de idiomas offline",
                ["DownloadModel"] = "Baixar o modelo selecionado",
                ["Finish"] = "Começar a usar o Bridge"
            }
        };
}
