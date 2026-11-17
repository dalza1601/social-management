namespace SocialManagement.Beneficiarios.Application.Features.DesactivarBeneficiario;

/// <summary>Datos de entrada para desactivar un beneficiario (baja lógica: el registro no se elimina).</summary>
public sealed record DesactivarBeneficiarioCommand(int Id);
