namespace SocialManagement.Beneficiarios.Infrastructure.Configurations;

/// <summary>
/// Configuración del servidor local de IA Ollama (sección <c>Ollama</c>).
/// Se usará en un módulo posterior a través de Semantic Kernel.
/// </summary>
/// <remarks>Fase 0: solo configuración. No existe ninguna integración funcional de IA.</remarks>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public bool Enabled { get; set; }

    /// <summary>Endpoint HTTP de Ollama, p. ej. http://localhost:11434 o http://ollama:11434.</summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>Modelo descargado manualmente (p. ej. "llama3.2:1b"). Obligatorio si Enabled = true.</summary>
    public string? ModelId { get; set; }
}
