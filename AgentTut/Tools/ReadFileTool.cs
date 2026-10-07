using Spectre.Console;
using System.Text.Json.Nodes;

namespace AgentTut.Tools;

public class ReadFileTool : ToolBase
{
    public override string Name => "read_file";
    public override string Description => "Read the contents of a file at the given path.";

    public override JsonObject Parameters => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Absolute or relative path to the file to read."
            }
        },
        ["required"] = new JsonArray { "path" }
    };

    public override async Task<string> ExecuteAsync(Dictionary<string, object> args)
    {
        try
        {
            AnsiConsole.MarkupLine($"[bold magenta]⚡Agent wants to read a file[/]");

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                args["path"]?.ToString() ?? ""
            );
            var content = await File.ReadAllTextAsync(path);

            AnsiConsole.MarkupLine($"[bold green]⚡Agent successfully read a file: {path}[/]");

            return content;
        }
        catch (Exception e)
        {
            return $"Error reading file: {e.Message}";
        }
    }
}
