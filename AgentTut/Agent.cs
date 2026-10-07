using AgentTut.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Spectre.Console;
using System.ClientModel;

namespace AgentTut;

public class Agent
{
    AIAgent? _agent;

    public Agent(IConfiguration config)
    {
        var llmConfig = config.GetSection("LLM").Get<LLMConfig>();

        var openAiClient = new OpenAIClient(new ApiKeyCredential(llmConfig.ApiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(llmConfig.Api)
        });

        var chatClient = openAiClient
            .GetChatClient(llmConfig.Model)
            .AsIChatClient()
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                options.Reasoning = new ReasoningOptions
                {
                    Effort = ReasoningEffort.None
                };
            })
            .Build();

        var sprompt = buildSystemPrompt();
        var tools = detectTools();

        _agent = chatClient
            .AsAIAgent(
            instructions: sprompt,
            name: "PersonalAssistant",
            tools: tools
            );
    }

    public async Task Run(string userMessage)
    {
        if (_agent is null)
        {
            throw new InvalidOperationException("Agent is not initialized.");
        }

        var response = await _agent.RunAsync(userMessage);

        var panel = new Panel(new Markup(response.ToString()))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader("[bold green]Assistant[/]", Justify.Left),
            Padding = new Padding(1, 1, 1, 1),
        };
        AnsiConsole.Write(panel);
    }

    string buildSystemPrompt()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var cwd = Directory.GetCurrentDirectory();

        var workspace = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ai-assistant", "workspace");

        return $"""
            You are a personal AI assistant.
            Current date/time: {now}
            Current working directory: {cwd}
            Workspace: {workspace}
            When writing files, always use absolute paths unless the user explicitly specifies otherwise.
            Store any files you create in the workspace ({workspace}) unless the user specifies a different location.
            first try to read files from the workspace before accessing other locations.
            """;
    }

    List<AITool> detectTools()
    {
        var tools = new List<AITool>();

        var toolTypes = typeof(Agent).Assembly.GetTypes()
            .Where(t => typeof(ToolBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var toolType in toolTypes)
        {
            try
            {
                var toolInstance = (ToolBase)Activator.CreateInstance(toolType)!;

                // Convert tool to AITool using a static wrapper
                string toolName = toolInstance.Name;
                string toolDescription = toolInstance.Description;
                var toolParams = toolInstance.Parameters;

                var aiTool = AIFunctionFactory.Create(
                    (Dictionary<string, object> args) => toolInstance.ExecuteAsync(args),
                    toolName,
                    toolDescription
                );

                tools.Add(aiTool);
                AnsiConsole.MarkupLine($"[green]✓[/] Registered tool: [bold]{toolName}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to load {toolType.Name}: {ex.Message}");
            }
        }

        return tools;
    }
}

public record LLMConfig(string Api, string Model, string ApiKey);