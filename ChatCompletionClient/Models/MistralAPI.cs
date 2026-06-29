// Copyright(c) June 2026, devMobile Software
// Generated from https://docs.mistral.ai/api/endpoint/chat

namespace devMobile.Mistral.ModelsV1;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

// =============================================================================
// CUSTOM CONVERTERS
// =============================================================================

public sealed class ToolChoiceConverter : JsonConverter<ToolChoiceUnion>
{
   public override ToolChoiceUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (reader.TokenType == JsonTokenType.String)
         return new ToolChoiceUnion { StringValue = reader.GetString() };

      if (reader.TokenType == JsonTokenType.StartObject)
      {
         using var doc = JsonDocument.ParseValue(ref reader);
         if (doc.RootElement.TryGetProperty("type", out var typeProp) &&
             typeProp.GetString() == "function" &&
             doc.RootElement.TryGetProperty("function", out var funcProp))
         {
            return new ToolChoiceUnion
            {
               Type = "function",
               Function = funcProp.Deserialize<ToolChoiceFunction>(options)
            };
         }
         throw new JsonException(
            $"Unexpected JSON for {nameof(ToolChoiceUnion)}: object missing 'type'='function' and/or 'function' member.");
      }

      throw new JsonException(
         $"Unexpected JSON token for {nameof(ToolChoiceUnion)}: {reader.TokenType}.");
   }

   public override void Write(Utf8JsonWriter writer, ToolChoiceUnion value, JsonSerializerOptions options)
   {
      if (value.StringValue != null)
      {
         writer.WriteStringValue(value.StringValue);
         return;
      }

      writer.WriteStartObject();
      writer.WriteString("type", value.Type ?? "function");
      if (value.Function != null)
      {
         writer.WritePropertyName("function");
         JsonSerializer.Serialize(writer, value.Function, options);
      }
      writer.WriteEndObject();
   }
}

public sealed class StopConverter : JsonConverter<StopUnion>
{
   public override StopUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
      if (reader.TokenType == JsonTokenType.String)
         return new StopUnion { StringValue = reader.GetString() };

      if (reader.TokenType == JsonTokenType.StartArray)
      {
         var list = JsonSerializer.Deserialize<List<string>>(ref reader, options);
         return new StopUnion { StringListValue = list };
      }

      throw new JsonException(
         $"Unexpected JSON token for {nameof(StopUnion)}: {reader.TokenType}.");
   }

   public override void Write(Utf8JsonWriter writer, StopUnion value, JsonSerializerOptions options)
   {
      if (value.StringValue is not null)
         writer.WriteStringValue(value.StringValue);
      else if (value.StringListValue is not null)
         JsonSerializer.Serialize(writer, value.StringListValue, options);
      else
         throw new JsonException($"{nameof(StopUnion)} has neither {nameof(StopUnion.StringValue)} nor {nameof(StopUnion.StringListValue)} set.");
   }
}

// =============================================================================
// UNION TYPES
// =============================================================================

[JsonConverter(typeof(ToolChoiceConverter))]
public sealed class ToolChoiceUnion
{
   [JsonIgnore] public string? StringValue { get; init; }
   [JsonIgnore] public string? Type { get; init; }
   [JsonIgnore] public ToolChoiceFunction? Function { get; init; }

   public static implicit operator ToolChoiceUnion(string value) => new() { StringValue = value };
}

[JsonConverter(typeof(StopConverter))]
public sealed class StopUnion
{
   [JsonIgnore] public string? StringValue { get; init; }
   [JsonIgnore] public List<string>? StringListValue { get; init; }

   public static implicit operator StopUnion(string value) => new() { StringValue = value };
   public static implicit operator StopUnion(string[] values) => new() { StringListValue = values.ToList() };
}

// =============================================================================
// CONSTANTS (forward-compatible string fields)
// =============================================================================

/// <summary>
/// Known values for <see cref="ChatCompletionChoice.FinishReason"/> /
/// <see cref="ChatCompletionDelta.FinishReason"/>. Kept as constants rather than
/// an enum so that unrecognized server-side additions (e.g. "content_filter")
/// don't break deserialization mid-stream.
/// </summary>
public static class FinishReasons
{
   public const string Stop      = "stop";
   public const string Length    = "length";
   public const string ToolCalls = "tool_calls";
   public const string Error     = "error";
}

// =============================================================================
// ENUMS (request-side only — values are produced by this client, not the server,
// so a closed enum is safe.)
// =============================================================================

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PredictionType
{
   [JsonStringEnumMemberName("content")]
   Content
}

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

// =============================================================================
// REQUEST
// =============================================================================

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

   [JsonPropertyName("metadata")]
   public Dictionary<string, JsonElement>? Metadata { get; init; }

   [JsonPropertyName("n")]
   public int? N { get; init; }

   [JsonPropertyName("parallel_tool_calls")]
   public bool? ParallelToolCalls { get; init; }

   [JsonPropertyName("prediction")]
   public Prediction? Prediction { get; init; }

   [JsonPropertyName("presence_penalty")]
   public double? PresencePenalty { get; init; }

   [JsonPropertyName("prompt_cache_key")]
   public string? PromptCacheKey { get; init; }

   [JsonPropertyName("prompt_mode")]
   public PromptMode? PromptMode { get; init; }

   [JsonPropertyName("random_seed")]
   public int? RandomSeed { get; init; }

   [JsonPropertyName("reasoning_effort")]
   public ReasoningEffort? ReasoningEffort { get; init; }

   [JsonPropertyName("response_format")]
   public ResponseFormat? ResponseFormat { get; init; }

   [JsonPropertyName("safe_prompt")]
   public bool? SafePrompt { get; init; }

   [JsonPropertyName("stop")]
   public StopUnion? Stop { get; init; }

   [JsonPropertyName("stream")]
   public bool? Stream { get; init; }

   [JsonPropertyName("temperature")]
   public double? Temperature { get; init; }

   [JsonPropertyName("top_p")]
   public double? TopP { get; init; }

   [JsonPropertyName("tool_choice")]
   public ToolChoiceUnion? ToolChoice { get; init; }

   [JsonPropertyName("tools")]
   public List<ToolDefinition>? Tools { get; init; }
}

// =============================================================================
// MESSAGES
// =============================================================================

[JsonPolymorphic(TypeDiscriminatorPropertyName = "role")]
[JsonDerivedType(typeof(SystemMessage), typeDiscriminator: "system")]
[JsonDerivedType(typeof(UserMessage), typeDiscriminator: "user")]
[JsonDerivedType(typeof(AssistantMessage), typeDiscriminator: "assistant")]
[JsonDerivedType(typeof(ToolMessage), typeDiscriminator: "tool")]
public abstract class ChatMessage
{
   public static SystemMessage System(string content) => new(content);
   public static UserMessage User(string text) => UserMessage.FromText(text);
   public static UserMessage User(params UserContentPart[] parts) => UserMessage.FromParts(parts);
   public static ToolMessage Tool(string content, string toolCallId) => new(content, toolCallId);
}

public sealed class SystemMessage : ChatMessage
{
   [JsonPropertyName("content")]
   public required string Content { get; init; }

   [SetsRequiredMembers]
   internal SystemMessage(string content) => Content = content;
}

public sealed class UserMessage : ChatMessage
{
   /// <summary>
   /// JSON value: either a <see cref="string"/> (text-only message) or a
   /// <see cref="UserContentPart"/>[] (multi-modal: text + document_url / image_url / reference).
   /// Construct only via <see cref="FromText(string)"/> or <see cref="FromParts(UserContentPart[])"/>
   /// so System.Text.Json emits the polymorphic "type" discriminator on each part.
   /// Assigning a single <see cref="UserContentPart"/> (without wrapping it in an array)
   /// will produce malformed JSON because the static element type is lost.
   /// </summary>
   [JsonPropertyName("content")]
   public object Content { get; private init; } = null!;

   public static UserMessage FromText(string text) => new() { Content = text };
   public static UserMessage FromParts(params UserContentPart[] parts) => new() { Content = parts };
}

public sealed class AssistantMessage : ChatMessage
{
   [JsonPropertyName("content")]
   public string? Content { get; init; }

   [JsonPropertyName("tool_calls")]
   public List<ToolCall>? ToolCalls { get; init; }

   internal AssistantMessage() { }
}

public sealed class ToolMessage : ChatMessage
{
   [JsonPropertyName("content")]
   public required string Content { get; init; }

   [JsonPropertyName("tool_call_id")]
   public required string ToolCallId { get; init; }

   [SetsRequiredMembers]
   internal ToolMessage(string content, string toolCallId) =>
       (Content, ToolCallId) = (content, toolCallId);
}

// =============================================================================
// USER CONTENT PARTS (multi-modal)
// =============================================================================

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextPart), typeDiscriminator: "text")]
[JsonDerivedType(typeof(DocumentUrlPart), typeDiscriminator: "document_url")]
[JsonDerivedType(typeof(ImageUrlPart), typeDiscriminator: "image_url")]
[JsonDerivedType(typeof(ReferencePart), typeDiscriminator: "reference")]
public abstract class UserContentPart { }

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
   public required ImageUrlObject ImageUrl { get; init; }
}

public sealed class ImageUrlObject
{
   [JsonPropertyName("url")]
   public required string Url { get; init; }

   [JsonPropertyName("detail")]
   public string? Detail { get; init; }
}

public sealed class ReferencePart : UserContentPart
{
   [JsonPropertyName("reference_ids")]
   public required List<int> ReferenceIds { get; init; }
}

// =============================================================================
// TOOL CALLING
// =============================================================================

public sealed class ToolCall
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("type")]
   public required string Type { get; init; }

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

public sealed class ToolDefinition
{
   // "function" is currently the only Mistral-accepted value; default removes ceremony.
   [JsonPropertyName("type")]
   public string Type { get; init; } = "function";

   [JsonPropertyName("function")]
   public required ToolFunctionDefinition Function { get; init; }
}

public sealed class ToolFunctionDefinition
{
   [JsonPropertyName("name")]
   public required string Name { get; init; }

   [JsonPropertyName("description")]
   public string? Description { get; init; }

   // JSON Schema describing the function's parameters.
   [JsonPropertyName("parameters")]
   public JsonNode? ParametersSchema { get; init; }
}

public sealed class ToolChoiceFunction
{
   [JsonPropertyName("name")]
   public required string Name { get; init; }
}

// =============================================================================
// OTHER REQUEST SUB-OBJECTS
// =============================================================================

public sealed class Prediction
{
   [JsonPropertyName("type")]
   public PredictionType Type { get; init; } = PredictionType.Content;

   [JsonPropertyName("content")]
   public string? Content { get; init; }
}

public sealed class GuardrailConfig
{
   [JsonPropertyName("type")]
   public string? Type { get; init; }

   [JsonPropertyName("params")]
   public Dictionary<string, JsonElement>? Params { get; init; }
}

public sealed class ResponseFormat
{
   [JsonPropertyName("type")]
   public required ResponseFormatType Type { get; init; }

   [JsonPropertyName("json_schema")]
   public JsonNode? JsonSchema { get; init; }
}

// =============================================================================
// RESPONSE (non-streaming)
// =============================================================================

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
   public AssistantMessage? Message { get; init; }

   // string for forward-compat with new server-side reasons. See FinishReasons.
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

// =============================================================================
// RESPONSE (streaming - text/event-stream)
// =============================================================================

public sealed class ChatCompletionChunk
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public string? Object { get; init; }

   [JsonPropertyName("created")]
   public long Created { get; init; }

   [JsonPropertyName("model")]
   public string? Model { get; init; }

   [JsonPropertyName("choices")]
   public required List<ChatCompletionDelta> Choices { get; init; }

   [JsonPropertyName("usage")]
   public UsageInfo? Usage { get; init; }
}

public sealed class ChatCompletionDelta
{
   [JsonPropertyName("index")]
   public int Index { get; init; }

   [JsonPropertyName("delta")]
   public required ChatMessageDelta Delta { get; init; }

   // string for forward-compat with new server-side reasons. See FinishReasons.
   [JsonPropertyName("finish_reason")]
   public string? FinishReason { get; init; }
}

public sealed class ChatMessageDelta
{
   [JsonPropertyName("role")]
   public string? Role { get; init; }

   [JsonPropertyName("content")]
   public string? Content { get; init; }

   [JsonPropertyName("tool_calls")]
   public List<ToolCallDelta>? ToolCalls { get; init; }
}

// =============================================================================
// STREAMING TOOL CALL DTOs
// =============================================================================

public sealed class ToolCallDelta
{
   [JsonPropertyName("index")]
   public int? Index { get; init; }

   [JsonPropertyName("id")]
   public string? Id { get; init; }

   [JsonPropertyName("type")]
   public string? Type { get; init; }

   [JsonPropertyName("function")]
   public ToolCallFunctionDelta? Function { get; init; }
}

public sealed class ToolCallFunctionDelta
{
   [JsonPropertyName("name")]
   public string? Name { get; init; }

   [JsonPropertyName("arguments")]
   public string? ArgumentsJson { get; init; }
}

// =============================================================================
// ERROR RESPONSE
// =============================================================================

/// <summary>
/// Body shape returned by Mistral on 4xx/5xx (and as the inner payload of some
/// error chunks during streaming). Deserialize this whenever an HTTP call comes
/// back !IsSuccessStatusCode, then surface <see cref="Message"/> to the user.
/// </summary>
public sealed class MistralErrorResponse
{
   [JsonPropertyName("object")]  public string? Object  { get; init; }   // always "error"
   [JsonPropertyName("message")] public string? Message { get; init; }
   [JsonPropertyName("type")]    public string? Type    { get; init; }
   [JsonPropertyName("code")]    public string? Code    { get; init; }
   [JsonPropertyName("param")]   public string? Param   { get; init; }
}

// =============================================================================
// SOURCE-GENERATED JSON CONTEXT (.NET 10 trim/AOT-friendly)
//
// Opt-in: callers using reflection-based JsonSerializerOptions are unaffected.
// To use, pass MistralJsonContext.Default.ChatCompletionRequest (etc.) to
// JsonSerializer / JsonContent.Create.
//
// Caveats:
//   * UserMessage.Content is typed as `object`; STJ falls back to reflection
//     for that one property, so full AOT cleanliness needs that to be tightened
//     to a concrete (or polymorphic) type first.
//   * ToolChoiceConverter / StopConverter use JsonDocument.ParseValue and
//     JsonSerializer.Deserialize<List<string>> internally — both work at
//     runtime but emit IL2026/IL3050 warnings under PublishAot.
// =============================================================================

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatCompletionResponse))]
[JsonSerializable(typeof(ChatCompletionChunk))]
[JsonSerializable(typeof(MistralErrorResponse))]
[JsonSerializable(typeof(UserContentPart))]
[JsonSerializable(typeof(UserContentPart[]))]
public sealed partial class MistralJsonContext : JsonSerializerContext { }