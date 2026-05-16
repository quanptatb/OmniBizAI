namespace OmniBizAI.Helpers;

/// <summary>Generates auto-increment codes for entities (EMP001, DEPT001, KPI001, etc.).</summary>
public static class CodeGeneratorHelper
{
    public static string GenerateCode(string prefix, int sequence, int padding = 3)
        => $"{prefix}{sequence.ToString().PadLeft(padding, '0')}";

    public static string GenerateEmployeeCode(int sequence) => GenerateCode("EMP", sequence);
    public static string GenerateDepartmentCode(int sequence) => GenerateCode("DEPT", sequence);
    public static string GeneratePositionCode(int sequence) => GenerateCode("POS", sequence);
    public static string GenerateKpiCode(int sequence) => GenerateCode("KPI", sequence);
    public static string GenerateOkrCode(int sequence) => GenerateCode("OKR", sequence);
}
