
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

AIAgent agent = chatClient
    .AsAIAgent(
        instructions: "You are a helpful assistant that can book meeting rooms",
        name: "Booking agent",
        tools:
        [
            AIFunctionFactory.Create(BookMeetingRoom)
        ]
    );

//var resp = await agent.RunAsync("Book a meeting room for 5 people tomorrow at lunch");
//Console.WriteLine(resp);

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    var response = await agent.RunAsync(input);

    Console.WriteLine("Booking agent: " + response);
}


[Description("Book a meeting room with the specified details")]
static string BookMeetingRoom(BookingRequest request)
{
    // Simulate booking a meeting room based on the request
    return $"Meeting room '{request.RoomName}' booked on {request.Date} from {request.StartTime} to {request.EndTime} for {request.Attendees} attendees with required equipment: {string.Join(", ", request.RequiredEquipment)}.";
}


public record BookingRequest(
string RoomName,
DateOnly Date,
TimeOnly StartTime,
TimeOnly EndTime,
int Attendees,
string[] RequiredEquipment
);