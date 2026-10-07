using System.Text.RegularExpressions;

namespace AgentTut.Tools;

/// <summary>
/// Validates commands to prevent execution of dangerous operations.
/// </summary>
public static class CommandValidator
{
    // Dangerous patterns that should never be allowed
    private static readonly List<string> DangerousPatternsUnix =
    [
        // Destructive filesystem operations
        @"rm\s+-rf\s+/",
        @"rm\s+-r\s+/",
        @"mkfs",
        @"dd\s+if=",

        // Fork bomb
        @":\(\)\s*\{.*\}",

        // Writing to system devices
        @">\s*/dev/sd",
        @">\s*/dev/hd",

        // Removing boot/system files
        @"rm\s+.*(/boot/|/etc/|/sys/|/proc/)",

        // Format drives
        @"shred\s+",
        @"wipe\s+",

        // Kill all processes
        @"killall\s+-9",

        // Direct kernel operations
        @"echo\s+.*>/proc",

        // Sudo without restrictions
        @"sudo\s+.*rm\s+-rf",
        @"sudo\s+.*mkfs",
    ];

    private static readonly List<string> DangerousPatternsWindows =
    [
        // Destructive filesystem operations
        @"del\s+/s\s+/q\s*C:\\",
        @"rd\s+/s\s+/q\s*C:\\",
        @"format\s+C:",

        // Writing to raw disk
        @"dd\s+of=\\\\.\\",

        // Registry manipulation that breaks system
        @"reg\s+delete\s+HKLM",
        @"reg\s+delete\s+.*\\System32",
    ];

    private static List<string> GetDangerousPatterns()
    {
        var patterns = new List<string>(DangerousPatternsUnix);

        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            patterns.AddRange(DangerousPatternsWindows);
        }

        return patterns;
    }

    // Whitelist: commands that are known safe
    private static readonly List<string> SafeCommands =
    [
        "ls",
        "dir",
        "pwd",
        "cd",
        "cat",
        "echo",
        "grep",
        "find",
        "wc",
        "head",
        "tail",
        "file",
        "type",
        "which",
        "whereis",
        "whoami",
        "date",
        "uname",
        "df",
        "du",
        "ps",
        "top",
        "curl",
        "wget",
        "ping",
        "netstat",
    ];

    /// <summary>
    /// Validates if a command is safe to execute.
    /// </summary>
    /// <returns>A tuple of (isValid, errorMessage)</returns>
    public static (bool IsValid, string ErrorMessage) ValidateCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return (false, "Command cannot be empty");
        }

        var dangerousPatterns = GetDangerousPatterns();

        // Check for dangerous patterns
        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
            {
                return (false, $"Command blocked: matches dangerous pattern '{pattern}'");
            }
        }

        // Check for command chaining that could be dangerous
        var chainSeparators = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
            ? new[] { "&", "|", ";" }
            : new[] { "&&", "||", ";", "|" };

        foreach (var separator in chainSeparators)
        {
            if (command.Contains(separator))
            {
                var parts = Regex.Split(command, Regex.Escape(separator));
                foreach (var part in parts)
                {
                    var (isValid, error) = ValidateSingleCommand(part.Trim());
                    if (!isValid)
                    {
                        return (false, error);
                    }
                }
                break;
            }
        }

        var (isSingle, singleError) = ValidateSingleCommand(command);
        if (!isSingle)
        {
            return (false, singleError);
        }

        return (true, "");
    }

    private static (bool IsValid, string ErrorMessage) ValidateSingleCommand(string command)
    {
        var trimmedCmd = command.Trim();
        if (string.IsNullOrEmpty(trimmedCmd))
        {
            return (true, "");
        }

        // Extract the command name (first word)
        var commandName = Regex.Split(trimmedCmd, @"\s+")[0];
        commandName = commandName.TrimStart('-');

        // Optional: enforce whitelist for extra safety
        // Uncomment the line below to only allow whitelisted commands
        // if (!SafeCommands.Any(c => commandName.Equals(c, StringComparison.OrdinalIgnoreCase)))
        // {
        //     return (false, $"Command '{commandName}' is not in the whitelist of allowed commands");
        // }

        return (true, "");
    }

    /// <summary>
    /// Gets a list of allowed safe commands (for documentation).
    /// </summary>
    public static IReadOnlyList<string> GetSafeCommands() => SafeCommands.AsReadOnly();
}
