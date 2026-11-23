using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace CentralPress.API.Documents;

/// <summary>
/// DOCX -> PDF converter.
/// Uses LibreOffice headless CLI when available (per PLAN.md), with fallback to Windows Word COM automation.
/// </summary>
public sealed class PdfConverter(IOptions<DocumentOptions> options)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly DocumentOptions _o = options.Value;

    public async Task<byte[]> ToPdfAsync(byte[] docx, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            if (File.Exists(_o.SofficePath))
            {
                try
                {
                    return await ConvertWithLibreOfficeAsync(docx, ct);
                }
                catch when (OperatingSystem.IsWindows())
                {
                    // Fallback to Word COM on Windows if LibreOffice process fails
                    return ConvertWithWordCom(docx);
                }
            }

            if (OperatingSystem.IsWindows())
            {
                return ConvertWithWordCom(docx);
            }

            throw new InvalidOperationException($"LibreOffice was not found at configured path '{_o.SofficePath}' and no Windows Word COM fallback is available.");
        }
        finally
        {
            Gate.Release();
        }
    }

    private string ResolveExecutablePath()
    {
        string executablePath = _o.SofficePath;
        var comCandidate = Path.ChangeExtension(_o.SofficePath, ".com");
        if (File.Exists(comCandidate))
        {
            executablePath = comCandidate;
        }
        return executablePath;
    }

    private void ApplyProfileArgument(ProcessStartInfo psi)
    {
        if (!string.IsNullOrWhiteSpace(_o.ProfileFolder))
        {
            try
            {
                if (!Directory.Exists(_o.ProfileFolder))
                    Directory.CreateDirectory(_o.ProfileFolder);
            }
            catch { }

            var profileUri = "file:///" + _o.ProfileFolder.Trim().Replace('\\', '/');
            psi.ArgumentList.Add($"-env:UserInstallation={profileUri}");
        }
    }

    /// <summary>
    /// Initializes the dedicated LibreOffice user profile once at startup if it has not been created yet,
    /// under the conversion Gate so it never spawns recurring background processes or collides with PDF exports.
    /// </summary>
    public async Task WarmUpProfileOnceAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_o.SofficePath) || string.IsNullOrWhiteSpace(_o.ProfileFolder))
        {
            return;
        }

        var profileMarker = Path.Combine(_o.ProfileFolder, "user", "registrymodifications.xcu");
        if (File.Exists(profileMarker))
        {
            return;
        }

        await Gate.WaitAsync(ct);
        try
        {
            if (File.Exists(profileMarker))
            {
                return;
            }

            var psi = new ProcessStartInfo(ResolveExecutablePath())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            ApplyProfileArgument(psi);
            foreach (var a in new[] { "--headless", "--nologo", "--nodefault", "--nofirststartwizard", "--norestore", "--version" })
            {
                psi.ArgumentList.Add(a);
            }

            using var p = Process.Start(psi);
            if (p == null) return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                await p.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(true); } catch { }
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<byte[]> ConvertWithLibreOfficeAsync(byte[] docx, CancellationToken ct)
    {
        var dir = Directory.CreateTempSubdirectory("thoth-doc-");
        try
        {
            var input = Path.Combine(dir.FullName, "in.docx");
            await File.WriteAllBytesAsync(input, docx, ct);

            // On Windows, prefer soffice.com (console launcher) if available for faster CLI synchronization
            var psi = new ProcessStartInfo(ResolveExecutablePath())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            ApplyProfileArgument(psi);

            foreach (var a in new[] { "--headless", "--nologo", "--nodefault", "--nofirststartwizard", "--norestore", "--convert-to", "pdf", "--outdir", dir.FullName, input })
            {
                psi.ArgumentList.Add(a);
            }

            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start LibreOffice process.");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_o.PdfTimeoutSeconds));
            try
            {
                await p.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { p.Kill(true); } catch { }
                throw;
            }

            var pdf = Path.Combine(dir.FullName, "in.pdf");
            if (!File.Exists(pdf))
            {
                var err = await p.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException("LibreOffice PDF conversion failed: " + err);
            }

            return await File.ReadAllBytesAsync(pdf, ct);
        }
        finally
        {
            try { dir.Delete(true); } catch { }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static byte[] ConvertWithWordCom(byte[] docx)
    {
        var dir = Directory.CreateTempSubdirectory("thoth-word-");
        try
        {
            var input = Path.Combine(dir.FullName, "in.docx");
            var output = Path.Combine(dir.FullName, "in.pdf");
            File.WriteAllBytes(input, docx);

            Type? wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null)
            {
                throw new InvalidOperationException("Microsoft Word COM is not registered on this system.");
            }

            dynamic? word = Activator.CreateInstance(wordType);
            if (word == null)
            {
                throw new InvalidOperationException("Could not instantiate Word.Application COM object.");
            }

            try
            {
                word.Visible = false;
                dynamic doc = word.Documents.Open(input, ReadOnly: true);
                try
                {
                    // 17 = wdFormatPDF
                    doc.SaveAs(output, 17);
                }
                finally
                {
                    doc.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                }
            }
            finally
            {
                word.Quit(false);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(word);
            }

            if (!File.Exists(output))
            {
                throw new InvalidOperationException("Word COM failed to create PDF file.");
            }

            return File.ReadAllBytes(output);
        }
        finally
        {
            try { dir.Delete(true); } catch { }
        }
    }
}
