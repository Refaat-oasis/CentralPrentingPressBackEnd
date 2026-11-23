using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralPress.API.Documents;

/// <summary>
/// Performs a single one-shot startup initialization of the dedicated LibreOffice user profile
/// so PDF exports do not stall on first-run profile setup or suffer recurring 3-second restart loops.
/// </summary>
public sealed class LibreOfficeWarmService : BackgroundService
{
    private readonly IOptions<DocumentOptions> _options;
    private readonly PdfConverter _pdfConverter;
    private readonly ILogger<LibreOfficeWarmService> _logger;

    public LibreOfficeWarmService(
        IOptions<DocumentOptions> options,
        PdfConverter pdfConverter,
        ILogger<LibreOfficeWarmService> logger)
    {
        _options = options;
        _pdfConverter = pdfConverter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = _options.Value;
        if (!File.Exists(opt.SofficePath))
        {
            _logger.LogInformation("LibreOffice startup profile check skipped: soffice binary not found at '{Path}'.", opt.SofficePath);
            return;
        }

        try
        {
            await _pdfConverter.WarmUpProfileOnceAsync(stoppingToken);
            _logger.LogInformation("LibreOffice profile verified at '{ProfileFolder}'. No persistent background soffice process required.", opt.ProfileFolder);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Non-fatal LibreOffice startup profile initialization warning.");
        }
    }
}
