using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace CentralPress.API.Documents;

/// <summary>
/// Losslessly merges multiple PDF documents into a single multi-page PDF while preserving
/// each source page's exact dimensions (e.g., A4 Tasweya + B4 Moqaysa), fonts, and RTL Arabic glyphs.
/// </summary>
public static class PdfMerger
{
    public static byte[] Merge(IReadOnlyList<byte[]> pdfBuffers)
    {
        if (pdfBuffers == null || pdfBuffers.Count == 0)
            throw new ArgumentException("At least one PDF document is required for merging.", nameof(pdfBuffers));

        if (pdfBuffers.Count == 1)
            return pdfBuffers[0];

        using var output = new PdfDocument();
        foreach (var bytes in pdfBuffers)
        {
            if (bytes == null || bytes.Length == 0) continue;

            using var inputStream = new MemoryStream(bytes, writable: false);
            using var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
            for (int i = 0; i < inputDoc.PageCount; i++)
            {
                output.AddPage(inputDoc.Pages[i]);
            }
        }

        if (output.PageCount == 0)
            throw new InvalidOperationException("No pages were found in the supplied PDF documents.");

        using var outputStream = new MemoryStream();
        output.Save(outputStream, closeStream: false);
        return outputStream.ToArray();
    }
}
