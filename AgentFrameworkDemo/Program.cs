using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using StackExchange.Redis;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();


var openAiClient = new OpenAIClient(new ApiKeyCredential(config["LLM:ApiKey"] ?? ""), new OpenAIClientOptions
{
    Endpoint = new Uri(config["LLM:Api"] ?? "")
});

//AIAgent joker = openAiClient
//    .GetChatClient(modelName)
//    .AsAIAgent(
//        instructions: "You are very good at telling jokes",
//        name: "JokerAgent"
//    );

var redisOptions = ConfigurationOptions.Parse("localhost:6379");
//redisOptions.Password = "fyV9f2G5naz$G@slHdwNIMJ@";  // Replace with your Redis password
var redis = ConnectionMultiplexer.Connect(redisOptions);

const string messagesKey = "joker:messages";
const string sessionStateKey = "joker:session";

AIAgent joker = openAiClient
    .GetChatClient(config["LLM:Model"])
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions { Instructions = "You are very good at telling jokes" },
        Name = "JokerAgent",
        ChatHistoryProvider = new RedisChatHistoryProvider(redis, messagesKey)
    });

var db = redis.GetDatabase();

AgentSession session;

var existingState = await db.StringGetAsync(sessionStateKey);
if (existingState.HasValue)
{
    session = await joker.DeserializeSessionAsync(JsonElement.Parse(existingState.ToString()));

    Console.WriteLine("Session restored from Redis");
}
else
{
    session = await joker.CreateSessionAsync();
}

//if (File.Exists("./session.json"))
//{
//    string sessionData = await File.ReadAllTextAsync("./session.json");
//    if (!string.IsNullOrWhiteSpace(sessionData))
//    {
//        session = await joker.DeserializeSessionAsync(JsonElement.Parse(sessionData));
//    }
//    else
//    {
//        session = await joker.CreateSessionAsync();
//    }
//}
//else
//{
//    session = await joker.CreateSessionAsync();
//}

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    var response = await joker.RunAsync(input, session);

    Console.WriteLine("Joker: " + response);
    Console.WriteLine("Input Tokens: {0} / Output Tokens: {1}", response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount);

    //var provider = joker.GetService<InMemoryChatHistoryProvider>();
    //var messages = provider.GetMessages(session);

    //Console.WriteLine();
    //Console.WriteLine("HISTORY:");
    //Console.WriteLine("----------------------------");
    //Console.WriteLine();

    //foreach (var m in messages)
    //{
    //    Console.WriteLine($"[{m.Role}] {m.Text}");
    //}

    //Console.WriteLine();
    //Console.WriteLine("----------------------------");
    //Console.WriteLine();

    var sessionBackup = await joker.SerializeSessionAsync(session);

    await db.StringSetAsync(sessionStateKey, sessionBackup.ToString());
    Console.WriteLine("Session saved to Redis");
    //await File.WriteAllTextAsync("./session.json", sessionBackup.ToString());
}

internal class RedisChatHistoryState
{
    [JsonPropertyName("RedisKey")]
    public string? RedisKey { get; set; }
}

public class RedisChatHistoryProvider : ChatHistoryProvider
{
    private readonly IDatabase _redis;
    private readonly ProviderSessionState<RedisChatHistoryState> _sessionState;

    public RedisChatHistoryProvider(IConnectionMultiplexer connectionMultiplexer, string messageKey)
    {
        _redis = connectionMultiplexer.GetDatabase();
        _sessionState = new ProviderSessionState<RedisChatHistoryState>(stateInitializer: _ => new RedisChatHistoryState
        {
            RedisKey = messageKey
        },
            stateKey: GetType().Name);
    }

    public override IReadOnlyList<string> StateKeys => [_sessionState.StateKey];

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);

        var entries = await _redis.ListRangeAsync(state.RedisKey);

        return entries
            .Select(entry => JsonSerializer.Deserialize<ChatMessage>(entry.ToString()))
            .ToList();
    }

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);

        var msg = context.RequestMessages.Concat(context.ResponseMessages);

        foreach (var m in msg)
        {
            var serialized = JsonSerializer.Serialize(m);
            await _redis.ListRightPushAsync(state.RedisKey, serialized);
        }

        _sessionState.SaveState(context.Session, state);
    }
}