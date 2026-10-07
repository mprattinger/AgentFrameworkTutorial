using Spectre.Console;
using System.Text.Json.Nodes;

namespace AgentTut.Tools;

public class ExecTool : ToolBase
{
    public override string Name => "exec";
    public override string Description => "Run a shell command and return the output. Use for listing files, checking system state, running scripts, etc.";

    public override JsonObject Parameters => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["command"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The shell command to execute."
            }
        },
        ["required"] = new JsonArray { "command" }
    };

    public override async Task<string> ExecuteAsync(Dictionary<string, object> args)
    {
        try
        {
            var cmd = args["command"]?.ToString() ?? "";

            // Validate command before execution
            var (isValid, errorMessage) = CommandValidator.ValidateCommand(cmd);
            if (!isValid)
            {
                AnsiConsole.MarkupLine($"[bold red]✗ Command blocked: {errorMessage}[/]");
                return $"Command rejected for security reasons: {errorMessage}";
            }

            AnsiConsole.MarkupLine($"[bold magenta]  Agent wants to execute:[/] {cmd}");

            // Execute the command
            var (output, exitCode) = await RunCommandAsync(cmd);

            if (exitCode != 0)
            {
                return $"Command failed with exit code {exitCode}:\n{output}";
            }

            return output;
        }
        catch (Exception e)
        {
            return $"Error executing command: {e.Message}";
        }
    }

    private async Task<(string Output, int ExitCode)> RunCommandAsync(string command)
    {
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);

        System.Diagnostics.ProcessStartInfo processInfo;

        if (isWindows)
        {
            // Windows: use PowerShell Core (pwsh) if available, otherwise cmd.exe
            var shell = "cmd.exe";
            var args = $"/c {command}";

            // Try to use PowerShell if available (more modern)
            try
            {
                if (System.IO.File.Exists("C:\\Program Files\\PowerShell\\7\\pwsh.exe") ||
                    ShellExists("pwsh"))
                {
                    shell = "pwsh";
                    args = $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"";
                }
            }
            catch
            {
                // Fall back to cmd.exe
            }

            processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shell,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        else
        {
            // Linux/macOS: use bash or sh
            var shell = System.IO.File.Exists("/bin/bash") ? "/bin/bash" : "/bin/sh";

            processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shell,
                Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        using var process = System.Diagnostics.Process.Start(processInfo)
            ?? throw new InvalidOperationException("Failed to start process");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = string.IsNullOrEmpty(error) ? output : $"{output}\nSTDERR: {error}";
        return (result, process.ExitCode);
    }

    private static bool ShellExists(string shellName)
    {
        try
        {
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
            var findCommand = isWindows ? "where" : "which";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = findCommand,
                Arguments = shellName,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
