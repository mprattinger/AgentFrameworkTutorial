
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ComponentModel;


var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();


var openAiClient = new OpenAIClient(new ApiKeyCredential(config["LLM:ApiKey"] ?? ""), new OpenAIClientOptions
{
    Endpoint = new Uri(config["LLM:Api"] ?? "")
});

var chatClient = openAiClient
    .GetChatClient(config["LLM:Model"] ?? "")
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

AIAgent weatherAgent = chatClient
    .AsAIAgent(
        instructions: "You are a helpful weather assistant. When asked about the weather, use the available tools to get current correct weather information",
        name: "WeatherBot",
        tools:
        [
            AIFunctionFactory.Create(GetCurrentWeather),
            AIFunctionFactory.Create(ConvertToFahrenheit)
        ]
    );

Console.WriteLine(await weatherAgent.RunAsync("What is the weather in Vienna? provide the temperature in Fahrenheit and Celsius."));

[Description("Get the current weather for a specified city.")]
static string GetCurrentWeather([Description("The name of the city to get the weather for.")] string city)
{
    var temp = Random.Shared.Next(-10, 35);
    var conditions = new[] { "sunny", "cloudy", "rainy", "snowy" };
    var currentCondition = conditions[Random.Shared.Next(conditions.Length)];

    // Simulate getting the current weather for the specified city
    return $"The current weather in {city} is {currentCondition} with a temperature of {temp}°C.";
}

[Description("Convert a temperature from Celsius to Fahrenheit.")]
static string ConvertToFahrenheit([Description("The temperature in Celsius to convert.")] double celsius)
{
    var fahrenheit = (celsius * 9 / 5) + 32;
    return fahrenheit.ToString();
}