using System.Diagnostics;

namespace MyWorkbench.DevLauncher;

internal static class Program
{
    private static readonly CancellationTokenSource ShutdownCts = new();

    public static int Main(string[] args)
    {
        Console.WriteLine("Starting MyWorkbench development stack...");

        var repoRoot = FindRepoRoot();
        var backendProject = Path.Combine(repoRoot, "src", "MyWorkbench.Api", "MyWorkbench.Api.csproj");
        var frontendDirectory = Path.Combine(repoRoot, "frontend", "my-workbench-web");

        if (!File.Exists(backendProject))
        {
            Console.Error.WriteLine($"Backend project not found: {backendProject}");
            return 1;
        }

        if (!Directory.Exists(frontendDirectory))
        {
            Console.Error.WriteLine($"Frontend project not found: {frontendDirectory}");
            return 1;
        }

        var backendProcess = StartProcess("dotnet", $"run --project \"{backendProject}\" --launch-profile http", repoRoot, "MyWorkbench API");
        var frontendProcess = StartProcess("npm", "install", frontendDirectory, "npm install");

        if (!WaitForProcessToExit(frontendProcess, "npm install"))
        {
            StopProcess(frontendProcess, "npm install");
            StopProcess(backendProcess, "MyWorkbench API");
            Console.Error.WriteLine("Frontend dependency installation failed.");
            return 1;
        }

        StopProcess(frontendProcess, "npm install");

        frontendProcess = StartProcess("npm", "start -- --host 0.0.0.0", frontendDirectory, "Angular frontend");

        Console.WriteLine();
        Console.WriteLine("Backend: http://localhost:5109");
        Console.WriteLine("Frontend: http://localhost:4200");
        Console.WriteLine();
        Console.WriteLine("Opening the browser...");
        TryOpenBrowser("http://localhost:4200");
        Console.WriteLine("Press Ctrl+C to stop both processes.");

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            StopProcess(backendProcess, "MyWorkbench API");
            StopProcess(frontendProcess, "Angular frontend");
            ShutdownCts.Cancel();
        };

        try
        {
            Task.WaitAny(
                backendProcess.WaitForExitAsync(ShutdownCts.Token),
                frontendProcess.WaitForExitAsync(ShutdownCts.Token));
        }
        catch (OperationCanceledException)
        {
            // Expected when the user presses Ctrl+C.
        }

        StopProcess(backendProcess, "MyWorkbench API");
        StopProcess(frontendProcess, "Angular frontend");
        return 0;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MyWorkbench.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static Process StartProcess(string fileName, string arguments, string workingDirectory, string label)
    {
        var resolvedFileName = ResolveExecutable(fileName);

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            CreateNoWindow = false,
            UseShellExecute = false
        };

        if (Path.GetExtension(resolvedFileName).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(resolvedFileName).Equals(".bat", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c \"\"{resolvedFileName}\" {arguments}\"";
        }
        else
        {
            startInfo.FileName = resolvedFileName;
            startInfo.Arguments = arguments;
        }

        Console.WriteLine($"Starting {label} using {resolvedFileName}...");
        var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new InvalidOperationException($"Unable to start {label}.");
        }

        return process;
    }

    private static string ResolveExecutable(string command)
    {
        if (File.Exists(command))
        {
            return command;
        }

        var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var pathEntry in existingPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var cmdCandidate = Path.Combine(pathEntry, command + ".cmd");
            if (File.Exists(cmdCandidate))
            {
                return cmdCandidate;
            }

            var exeCandidate = Path.Combine(pathEntry, command + ".exe");
            if (File.Exists(exeCandidate))
            {
                return exeCandidate;
            }

            var ps1Candidate = Path.Combine(pathEntry, command + ".ps1");
            if (File.Exists(ps1Candidate))
            {
                return ps1Candidate;
            }

            var candidate = Path.Combine(pathEntry, command);
            if (File.Exists(candidate) &&
                (Path.GetExtension(candidate).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetExtension(candidate).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetExtension(candidate).Equals(".bat", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetExtension(candidate).Equals(".ps1", StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        var knownNodeDirs = new[]
        {
            "C:\\Program Files\\nodejs",
            "C:\\Program Files (x86)\\nodejs",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "nodejs")
        };

        foreach (var nodeDir in knownNodeDirs)
        {
            if (!Directory.Exists(nodeDir))
            {
                continue;
            }

            var candidates = new[]
            {
                Path.Combine(nodeDir, command + ".cmd"),
                Path.Combine(nodeDir, command + ".exe"),
                Path.Combine(nodeDir, command + ".ps1"),
                Path.Combine(nodeDir, command)
            };

            var found = candidates
                .Where(File.Exists)
                .FirstOrDefault(candidate =>
                    Path.GetExtension(candidate).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(candidate).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(candidate).Equals(".ps1", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(Path.GetExtension(candidate)) == false);

            if (found is not null)
            {
                return found;
            }
        }

        throw new FileNotFoundException($"Could not find '{command}' on PATH or in common Node.js install locations. Install Node.js and ensure 'npm' is available.", command);
    }

    private static bool WaitForProcessToExit(Process process, string label)
    {
        try
        {
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{label} did not complete successfully: {ex.Message}");
            return false;
        }
    }

    private static void StopProcess(Process process, string label)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            Console.WriteLine($"Stopped {label}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not stop {label}: {ex.Message}");
        }
    }

    private static void TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open the browser automatically: {ex.Message}");
        }
    }
}
