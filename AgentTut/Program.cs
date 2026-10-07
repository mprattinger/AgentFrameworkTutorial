using AgentTut;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

AnsiConsole.MarkupLine("[bold]AI Assistant[/] - type [dim]exit[/] or [dim]quit[/] to stop\n");

var agent = new Agent(config);

AnsiConsole.MarkupLine("");

while (true)
{
    var userInput = AnsiConsole.Ask<string>("[bold cyan]You:[/]").Trim();
    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) || userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    await agent.Run(userInput);
}
AnsiConsole.MarkupLine("[dim]Goodbye![/]");