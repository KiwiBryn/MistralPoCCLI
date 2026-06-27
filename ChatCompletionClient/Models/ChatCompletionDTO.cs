// Copyright(c) June 2026, devMobile Software

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ChatCompletionClient;

// Content chunks — polymorphic on "type"
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextChunk), "text")]
[JsonDerivedType(typeof(ImageUrlChunk), "image_url")]
[JsonDerivedType(typeof(DocumentUrlChunk), "document_url")]
[JsonDerivedType(typeof(ReferenceChunk), "reference")]
public abstract class ContentChunk { }

public sealed class TextChunk : ContentChunk
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}

public sealed class ImageUrlChunk : ContentChunk
{
    [JsonPropertyName("image_url")]
    public required ImageUrl ImageUrl { get; set; }
}

public sealed class ImageUrl
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }  // "auto", "low", "high"
}

public sealed class DocumentUrlChunk : ContentChunk
{
    [JsonPropertyName("document_url")]
    public required string DocumentUrl { get; set; }
}

public sealed class ReferenceChunk : ContentChunk
{
    [JsonPropertyName("reference_ids")]
    public required int[] ReferenceIds { get; set; }
}

// Chat messages — polymorphic on "role"
[JsonPolymorphic(TypeDiscriminatorPropertyName = "role")]
[JsonDerivedType(typeof(SystemMessage), "system")]
[JsonDerivedType(typeof(UserMessage), "user")]
[JsonDerivedType(typeof(AssistantMessage), "assistant")]
[JsonDerivedType(typeof(ToolMessage), "tool")]
public abstract class ChatMessage { }

public sealed class SystemMessage : ChatMessage
{
    // string or ContentChunk[]
    [JsonPropertyName("content")]
    public required JsonNode Content { get; set; }
}

public sealed class UserMessage : ChatMessage
{
    // string or ContentChunk[]
    [JsonPropertyName("content")]
    public required JsonNode Content { get; set; }
}

public sealed class AssistantMessage : ChatMessage
{
    // string, ContentChunk[], or null
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCall[]? ToolCalls { get; set; }

    [JsonPropertyName("prefix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Prefix { get; set; }
}

public sealed class ToolMessage : ChatMessage
{
    // string or ContentChunk[]
    [JsonPropertyName("content")]
    public required JsonNode Content { get; set; }

    [JsonPropertyName("tool_call_id")]
    public required string ToolCallId { get; set; }
}

// Tool definitions — polymorphic on "type"
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FunctionTool), "function")]
[JsonDerivedType(typeof(WebSearchTool), "web_search")]
[JsonDerivedType(typeof(CodeInterpreterTool), "code_interpreter")]
public abstract class Tool { }

public sealed class FunctionTool : Tool
{
    [JsonPropertyName("function")]
    public required FunctionDefinition Function { get; set; }
}

public sealed class FunctionDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    // JSON Schema object
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Parameters { get; set; }

    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; set; }
}

public sealed class WebSearchTool : Tool { }

public sealed class CodeInterpreterTool : Tool { }

// Tool calls returned in assistant messages
public sealed class ToolCall
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type => "function";

    [JsonPropertyName("function")]
    public required FunctionCall Function { get; set; }
}

public sealed class FunctionCall
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    // Arguments are a JSON-encoded string
    [JsonPropertyName("arguments")]
    public required string Arguments { get; set; }
}

// Specific-function tool choice object
public sealed class ToolChoiceFunction
{
    [JsonPropertyName("type")]
    public string Type => "function";

    [JsonPropertyName("function")]
    public required ToolChoiceFunctionSpec Function { get; set; }
}

public sealed class ToolChoiceFunctionSpec
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

// Response format
public sealed class ResponseFormat
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }  // "text", "json_object", "json_schema"

    [JsonPropertyName("json_schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchemaFormat? JsonSchema { get; set; }
}

public sealed class JsonSchemaFormat
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Schema { get; set; }

    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; set; }
}

// Guardrail config
public sealed class GuardrailConfig
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("keywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Keywords { get; set; }
}

// Predicted completion optimisation
public sealed class Prediction
{
    [JsonPropertyName("type")]
    public string Type => "content";

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

// ── Request ──────────────────────────────────────────────────────────────────

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("messages")]
    public required ChatMessage[] Messages { get; set; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stream { get; set; }

    // string or string[] — use JsonValue.Create("stop") or new JsonArray(...)
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Stop { get; set; }

    [JsonPropertyName("random_seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RandomSeed { get; set; }

    [JsonPropertyName("safe_prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SafePrompt { get; set; }

    // "auto", "none", "any", "required" or a ToolChoiceFunction object
    // Use JsonValue.Create("auto") or JsonSerializer.SerializeToNode(new ToolChoiceFunction {...})
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? ToolChoice { get; set; }

    [JsonPropertyName("parallel_tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ParallelToolCalls { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Tool[]? Tools { get; set; }

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }

    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? N { get; set; }

    [JsonPropertyName("prompt_cache_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromptCacheKey { get; set; }

    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }  // "none","minimal","low","medium","high","xhigh"

    [JsonPropertyName("prompt_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromptMode { get; set; }  // "reasoning"

    [JsonPropertyName("guardrails")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GuardrailConfig[]? Guardrails { get; set; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("prediction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Prediction? Prediction { get; set; }
}

// ── Response ─────────────────────────────────────────────────────────────────

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("created")]
    public required long Created { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("choices")]
    public required ChatCompletionChoice[] Choices { get; set; }

    [JsonPropertyName("usage")]
    public required UsageInfo Usage { get; set; }
}

public sealed class ChatCompletionChoice
{
    [JsonPropertyName("index")]
    public required int Index { get; set; }

    [JsonPropertyName("message")]
    public required AssistantMessage Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public required string FinishReason { get; set; }  // "stop","length","model_length","error","tool_calls"

    [JsonPropertyName("logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Logprobs { get; set; }
}

public sealed class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public required int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public required int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public required int TotalTokens { get; set; }
}
