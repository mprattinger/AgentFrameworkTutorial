using System.Text.Json.Nodes;

namespace AgentTut.Tools;

public abstract class ToolBase
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    /// <summary>
    /// JSON Schema for the tool parameters.
    /// Example:
    /// {
    ///   "type": "object",
    ///   "properties": {
    ///     "path": { "type": "string", "description": "File path" }
    ///   },
    ///   "required": ["path"]
    /// }
    /// </summary>
    public abstract JsonObject Parameters { get; }

    public abstract Task<string> ExecuteAsync(Dictionary<string, object> args);

    public JsonObject Schema()
    {
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = Parameters
            }
        };
    }
}
