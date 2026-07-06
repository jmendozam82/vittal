namespace Vittal.DTO.Sala;

/// <summary>
/// Response DTO for the "Aplicar Plantilla a Sala" operation.
/// Reports how many items were created, reactivated, or skipped
/// in tipos_antecedente and tipos_signo_vital.
/// Historia de Usuario: HU-E02 — Plantillas de Especialidad
/// </summary>
public class AplicarPlantillaResponseDto
{
    public int AntecedentesCreados { get; set; }
    public int AntecedentesReactivados { get; set; }
    public int AntecedentesSaltados { get; set; }
    public int SignosVitalesCreados { get; set; }
    public int SignosVitalesReactivados { get; set; }
    public int SignosVitalesSaltados { get; set; }

    public int TotalProcesados =>
        AntecedentesCreados + AntecedentesReactivados + AntecedentesSaltados +
        SignosVitalesCreados + SignosVitalesReactivados + SignosVitalesSaltados;

    public string Resumen =>
        $"Antecedentes: {AntecedentesCreados} creados, {AntecedentesReactivados} reactivados, {AntecedentesSaltados} saltados. " +
        $"Signos Vitales: {SignosVitalesCreados} creados, {SignosVitalesReactivados} reactivados, {SignosVitalesSaltados} saltados.";
}
