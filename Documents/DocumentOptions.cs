namespace CentralPress.API.Documents;

public sealed class DocumentOptions
{
    public const string Section = "Documents";

    /// <summary>Full path of soffice(.exe).</summary>
    public string SofficePath { get; set; } = @"C:\Program Files\LibreOffice\program\soffice.exe";

    /// <summary>Folder (relative to ContentRoot) holding Moqaysa.docx, Tasweya_Single.docx, Tasweya_Multi.docx.</summary>
    public string TemplatesFolder { get; set; } = "TasweyaAndMoqaysaShape";

    /// <summary>Dedicated LibreOffice user profile folder for IIS AppPool permissions isolation.</summary>
    public string ProfileFolder { get; set; } = @"C:\ProgramData\Thoth\LibreOfficeProfile";

    public int PdfTimeoutSeconds { get; set; } = 90;
}
