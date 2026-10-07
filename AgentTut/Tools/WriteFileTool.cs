using Spectre.Console;
using System.Text.Json.Nodes;

namespace AgentTut.Tools;

public class WriteFileTool : ToolBase
{
    public override string Name => "write_file";
    public override string Description => "Write content to a file at the given path, creating directories as needed. Use this tool when you need to save data to a file.";

    public override JsonObject Parameters => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Absolute or relative path to the file to write."
            },
            ["content"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The content to write to the file."
            }
        },
        ["required"] = new JsonArray { "path", "content" }
    };

    public override async Task<string> ExecuteAsync(Dictionary<string, object> args)
    {
        try
        {
            AnsiConsole.MarkupLine($"[bold magenta]⚡Agent wants to write a file[/]");

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                args["path"]?.ToString() ?? ""
            );
            var content = args["content"]?.ToString() ?? "";

            // Handle escape sequences like \n and \t
            content = System.Text.RegularExpressions.Regex.Unescape(content);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, content);

            AnsiConsole.MarkupLine($"[bold green]⚡Agent successfully wrote a file: {path}[/]");

            return $"Wrote {content.Length} bytes to {path}";
        }
        catch (Exception e)
        {
            return $"Error writing file: {e.Message}";
        }
    }
}