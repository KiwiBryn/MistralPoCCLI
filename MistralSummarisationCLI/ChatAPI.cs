namespace devMobile.Mistral.Models;

using devMobile.Mistral.Models1;
using System.Text.Json.Serialization;

// ---------------------------------------------------------------------------
// Request
// ---------------------------------------------------------------------------

public sealed class ChatCompletionRequest
{
   [JsonPropertyName("model")]
   public required string Model { get; init; }

   [JsonPropertyName("messages")]
   public required List<ChatMessage> Messages { get; init; }

   [JsonPropertyName("frequency_penalty")]
   public double? FrequencyPenalty { get; init; }

   [JsonPropertyName("guardrails")]
   public List<GuardrailConfig>? Guardrails { get; init; }

   [JsonPropertyName("max_tokens")]
   public int? MaxTokens { get; init; }

   [JsonPropertyName("temperature")]
   public double? Temperature { get; init; }

   [JsonPropertyName("top_p")]
   public double? TopP { get; init; }

   [JsonPropertyName("n")]
   public int? N { get; init; }

   [JsonPropertyName("stream")]
   public bool? Stream { get; init; }

   [JsonPropertyName("stop")]
   public string[]? Stop { get; init; }

   [JsonPropertyName("presence_penalty")]
   public double? PresencePenalty { get; init; }

   [JsonPropertyName("random_seed")]
   public int? RandomSeed { get; init; }

   [JsonPropertyName("prompt_cache_key")]
   public string? PromptCacheKey { get; init; }

   [JsonPropertyName("prompt_mode")]
   public PromptMode? PromptMode { get; init; }

   [JsonPropertyName("reasoning_effort")]
   public ReasoningEffort? ReasoningEffort { get; init; }

   [JsonPropertyName("response_format")]
   public ResponseFormat? ResponseFormat { get; init; }

   [JsonPropertyName("safe_prompt")]
   public bool? SafePrompt { get; init; }

   [JsonPropertyName("parallel_tool_calls")]
   public bool? ParallelToolCalls { get; init; }

   [JsonPropertyName("tool_choice")]
   public ToolChoiceUnion? ToolChoice { get; init; }

   [JsonPropertyName("tools")]
   public List<ToolDefinition>? Tools { get; init; }

   [JsonPropertyName("prediction")]
   public Prediction? Prediction { get; init; }

   [JsonPropertyName("metadata")]
   public Dictionary<string, object>? Metadata { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "role")]
[JsonDerivedType(typeof(SystemMessage), typeDiscriminator: "system")]
[JsonDerivedType(typeof(UserMessage), typeDiscriminator: "user")]
[JsonDerivedType(typeof(AssistantMessage), typeDiscriminator: "assistant")]
[JsonDerivedType(typeof(ToolMessage), typeDiscriminator: "tool")]
public abstract class ChatMessage
{
}

public sealed class SystemMessage : ChatMessage
{
   [JsonPropertyName("content")]
   public required string Content { get; init; }
}

public sealed class UserMessage : ChatMessage
{
   // string for text-only messages, or IReadOnlyList<UserContentPart> for multi-modal.
   [JsonPropertyName("content")]
   public required object Content { get; init; }

   public static UserMessage FromText(string text) =>
      new() { Content = text };

   public static UserMessage FromParts(params UserContentPart[] parts) =>
      new() { Content = parts };
}

public sealed class AssistantMessage : ChatMessage
{
   [JsonPropertyName("content")]
   public string? Content { get; init; }

   [JsonPropertyName("tool_calls")]
   public List<ToolCall>? ToolCalls { get; init; }
}

public sealed class ToolMessage : ChatMessage
{
   [JsonPropertyName("content")]
   public required string Content { get; init; }

   [JsonPropertyName("tool_call_id")]
   public required string ToolCallId { get; init; }
}

public sealed class ToolCall
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("type")]
   public required string Type { get; init; } // always "function"

   [JsonPropertyName("function")]
   public required ToolCallFunction Function { get; init; }
}

public sealed class ToolCallFunction
{
   [JsonPropertyName("name")]
   public required string Name { get; init; }

   [JsonPropertyName("arguments")]
   public required string ArgumentsJson { get; init; }
}


public sealed class ToolChoiceUnion
{
   [JsonPropertyName("type")]
   public string? Type { get; init; } // "auto" | "none" | "any" | "required" | "function"

   [JsonPropertyName("function")]
   public ToolChoiceFunction? Function { get; init; }
}

public sealed class ToolChoiceFunction
{
   [JsonPropertyName("name")]
   public required string Name { get; init; }
}


public sealed class ToolDefinition
{
   [JsonPropertyName("type")]
   public required string Type { get; init; } // "function"

   [JsonPropertyName("function")]
   public required ToolFunctionDefinition Function { get; init; }
}

public sealed class ToolFunctionDefinition
{
   [JsonPropertyName("name")]
   public required string Name { get; init; }

   [JsonPropertyName("description")]
   public string? Description { get; init; }

   [JsonPropertyName("parameters")]
   public object? ParametersSchema { get; init; }
}

public sealed class Prediction
{
   [JsonPropertyName("type")]
   public string? Type { get; init; }

   [JsonPropertyName("content")]
   public string? Content { get; init; }
}

public sealed class GuardrailConfig
{
   [JsonPropertyName("type")]
   public string? Type { get; init; }

   [JsonPropertyName("params")]
   public Dictionary<string, object>? Params { get; init; }
}

public sealed class ResponseFormat
{
   [JsonPropertyName("type")]
   public required ResponseFormatType Type { get; init; }

   [JsonPropertyName("schema")]
   public object? Schema { get; init; }
}

// ---------------------------------------------------------------------------
// Enums
// ---------------------------------------------------------------------------

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResponseFormatType
{
   [JsonStringEnumMemberName("text")]
   Text,

   [JsonStringEnumMemberName("json_object")]
   JsonObject,

   [JsonStringEnumMemberName("json_schema")]
   JsonSchema
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PromptMode
{
   [JsonStringEnumMemberName("reasoning")]
   Reasoning
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReasoningEffort
{
   [JsonStringEnumMemberName("none")]
   None,

   [JsonStringEnumMemberName("minimal")]
   Minimal,

   [JsonStringEnumMemberName("low")]
   Low,

   [JsonStringEnumMemberName("medium")]
   Medium,

   [JsonStringEnumMemberName("high")]
   High,

   [JsonStringEnumMemberName("xhigh")]
   XHigh
}

// ---------------------------------------------------------------------------
// Response (non-streaming)
// ---------------------------------------------------------------------------

public sealed class ChatCompletionResponse
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; }

   [JsonPropertyName("created")]
   public required long Created { get; init; }

   [JsonPropertyName("model")]
   public required string Model { get; init; }

   [JsonPropertyName("choices")]
   public required List<ChatCompletionChoice> Choices { get; init; }

   [JsonPropertyName("usage")]
   public required UsageInfo Usage { get; init; }
}

public sealed class ChatCompletionChoice
{
   [JsonPropertyName("index")]
   public int Index { get; init; }

   [JsonPropertyName("message")]
   public ChatMessage? Message { get; init; }

   [JsonPropertyName("finish_reason")]
   public string? FinishReason { get; init; }
}

public sealed class UsageInfo
{
   [JsonPropertyName("prompt_tokens")]
   public int? PromptTokens { get; init; }

   [JsonPropertyName("completion_tokens")]
   public int? CompletionTokens { get; init; }

   [JsonPropertyName("total_tokens")]
   public int? TotalTokens { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextPart),        typeDiscriminator: "text")]
[JsonDerivedType(typeof(DocumentUrlPart), typeDiscriminator: "document_url")]
[JsonDerivedType(typeof(ImageUrlPart),    typeDiscriminator: "image_url")]
[JsonDerivedType(typeof(ReferencePart),   typeDiscriminator: "reference")]
public abstract class UserContentPart
{
}

public sealed class TextPart : UserContentPart
{
   [JsonPropertyName("text")]
   public required string Text { get; init; }
}

public sealed class DocumentUrlPart : UserContentPart
{
   [JsonPropertyName("document_url")]
   public required string DocumentUrl { get; init; }

   [JsonPropertyName("document_name")]
   public string? DocumentName { get; init; }
}

public sealed class ImageUrlPart : UserContentPart
{
   [JsonPropertyName("image_url")]
   public required string ImageUrl { get; init; }
}

public sealed class ReferencePart : UserContentPart
{
   [JsonPropertyName("reference_ids")]
   public required List<int> ReferenceIds { get; init; }
}


/*
[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatCompletionResponse))]
[JsonSerializable(typeof(ChatCompletionChoice))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(SystemMessage))]
[JsonSerializable(typeof(UserMessage))]
[JsonSerializable(typeof(AssistantMessage))]
[JsonSerializable(typeof(ToolMessage))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(ToolCallFunction))]
[JsonSerializable(typeof(ToolDefinition))]
[JsonSerializable(typeof(ToolFunctionDefinition))]
[JsonSerializable(typeof(ResponseFormat))]
[JsonSerializable(typeof(ResponseFormatType))]
[JsonSerializable(typeof(PromptMode))]
[JsonSerializable(typeof(ReasoningEffort))]
[JsonSerializable(typeof(UsageInfo))]
public partial class MistralChatJsonContext : JsonSerializerContext
{
}
*/
