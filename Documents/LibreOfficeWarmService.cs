using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CentralPress.API.Documents;

/// <summary>
/// Keeps a warm, headless LibreOffice background worker alive so that subsequent
/// PDF conversion requests execute against an already-initialized runtime in memory.
/// </summary>
public sealed class LibreOfficeWarmService : BackgroundService
{
    private readonly IOptions<DocumentOptions> _options;
    private readonly ILogger<LibreOfficeWarmService> _logger;
    private readonly object _lock = new();
    private Process? _daemonProcess;

    public LibreOfficeWarmService(IOptions<DocumentOptions> options, ILogger<LibreOfficeWarmService> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = _options.Value;
        if (!File.Exists(opt.SofficePath))
        {
            _logger.LogInformation("LibreOffice warm daemon skipped: soffice binary not found at '{Path}'.", opt.SofficePath);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            Process? currentProc = null;
            try
            {
                currentProc = StartWarmInstance(opt);

                if (currentProc != null)
                {
                    await currentProc.WaitForExitAsync(stoppingToken);
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        int exitCode = 0;
                        try { exitCode = currentProc.ExitCode; } catch { }
                        _logger.LogWarning("LibreOffice warm daemon process exited with code {Code}. Restarting in 3s...", exitCode);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to start or maintain LibreOffice warm daemon. Retrying in 5s...");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        StopWarmInstance();
    }

    private Process? StartWarmInstance(DocumentOptions opt)
    {
        lock (_lock)
        {
            StopWarmInstance();

            var profileUri = "file:///" + opt.ProfileFolder.Trim().Replace('\\', '/');
            try
            {
                if (!Directory.Exists(opt.ProfileFolder))
                {
                    Directory.CreateDirectory(opt.ProfileFolder);
                }
            }
            catch { }

            var psi = new ProcessStartInfo(opt.SofficePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            psi.ArgumentList.Add("--headless");
            psi.ArgumentList.Add("--invisible");
            psi.ArgumentList.Add("--nologo");
            psi.ArgumentList.Add("--nodefault");
            psi.ArgumentList.Add("--nofirststartwizard");
            psi.ArgumentList.Add("--norestore");
            psi.ArgumentList.Add($"-env:UserInstallation={profileUri}");

            _daemonProcess = Process.Start(psi);
            if (_daemonProcess != null)
            {
                _logger.LogInformation("LibreOffice warm daemon started successfully (PID: {Pid}) with profile {Profile}.", _daemonProcess.Id, profileUri);
            }
            return _daemonProcess;
        }
    }

    private void StopWarmInstance()
    {
        lock (_lock)
        {
            if (_daemonProcess != null)
            {
                try
                {
                    if (!_daemonProcess.HasExited)
                    {
                        _daemonProcess.Kill(true);
                    }
                }
                catch { }
                try
                {
                    _daemonProcess.Dispose();
                }
                catch { }
                _daemonProcess = null;
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        StopWarmInstance();
        return base.StopAsync(cancellationToken);
    }
}
