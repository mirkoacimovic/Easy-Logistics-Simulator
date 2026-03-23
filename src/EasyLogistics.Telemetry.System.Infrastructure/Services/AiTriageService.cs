using System.Diagnostics;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Telemetry.System.Infrastructure.Services;

public sealed class AiTriageService : ITriageService
{
    private readonly ILogger<AiTriageService> _logger;
    private readonly string _pythonPath;
    private readonly string _scriptPath;

    public AiTriageService(ILogger<AiTriageService> logger)
    {
        _logger = logger;
        // Paths defined in our Hybrid Dockerfile
        _pythonPath = Environment.GetEnvironmentVariable("PYTHON_EXECUTABLE") ?? "python3";
        _scriptPath = Environment.GetEnvironmentVariable("PYTHON_SCRIPT_PATH") ?? "/app/Engine/triage_logic.py";
    }

    public async Task<string> AnalyzeTelemetryAsync(string truckId, double speed, double fuelLevel)
    {
        // We pass the data as command line arguments to the Python Engine
        // Naming Convention: python triage_logic.py <id> <speed> <fuel>
        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = $"{_scriptPath} {truckId} {speed} {fuelLevel}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return "CRITICAL: Engine Offline";

            // StandardOutput will be the "Triage Level" returned by Python
            string result = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Python Engine Output: {Error}", error.Trim());
            }

            return string.IsNullOrWhiteSpace(result) ? "NORMAL" : result.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bridge Failure: Could not reach Python Engine at {Path}", _pythonPath);
            return "UNKNOWN";
        }
    }
}