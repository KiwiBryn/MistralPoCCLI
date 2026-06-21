// Copyright(c) June 2026, devMobile Software
//
namespace MistralBasicStreamingCLI.Models;


public sealed class ChatCompletionRequest
{
   [JsonPropertyName("model")] public required string Model { get; init; }
   [JsonPropertyName("messages")] public required List<ChatMessage> Messages { get; init; }
   [JsonPropertyName("temperature")] public double? Temperature { get; init; }
   [JsonPropertyName("max_tokens")] public int? MaxTokens { get; init; }
   [JsonPropertyName("stream")] public bool? Stream { get; init; }
   [JsonPropertyName("response_format")] public ResponseFormat? ResponseFormat { get; init; }
}

public sealed class ChatStreamingCompletionRequest
{
   [JsonPropertyName("model")] public required string Model { get; init; }
   [JsonPropertyName("messages")] public required List<ChatMessage> Messages { get; init; }
   [JsonPropertyName("temperature")] public double? Temperature { get; init; }
   [JsonPropertyName("max_tokens")] public int? MaxTokens { get; init; }
   [JsonPropertyName("stream")] public bool? Stream { get; init; }
   [JsonPropertyName("response_format")] public ResponseFormat? ResponseFormat { get; init; }
}

public sealed class ChatMessage
{
   [JsonPropertyName("role")] public required string Role { get; init; }
   [JsonPropertyName("content")] public required string Content { get; init; }
}

public sealed class ResponseFormat
{
   [JsonPropertyName("type")] public required string Type { get; init; }
}

public sealed class ChatCompletionResponse
{
   [JsonPropertyName("id")] public required string Id { get; init; }
   [JsonPropertyName("choices")] public required List<ChatCompletionChoice> Choices { get; init; }
   [JsonPropertyName("usage")] public TokenUsage? Usage { get; init; }
}

public sealed class TokenUsage
{
   [JsonPropertyName("prompt_tokens")] public int? PromptTokens { get; init; }
   [JsonPropertyName("completion_tokens")] public int? CompletionTokens { get; init; }
   [JsonPropertyName("total_tokens")] public int? TotalTokens { get; init; }
}

public sealed class ChatCompletionChoice
{
   [JsonPropertyName("index")] public int Index { get; init; }
   [JsonPropertyName("message")] public required ChatMessage Message { get; init; }
   [JsonPropertyName("finish_reason")] public string? FinishReason { get; init; }
}

public sealed class ChatCompletionChunk
{
   [JsonPropertyName("id")] public required string Id { get; init; }
   [JsonPropertyName("choices")] public required List<ChatCompletionDelta> Choices { get; init; }
   [JsonPropertyName("usage")] public TokenUsage? Usage { get; init; }
}

public sealed class ChatCompletionDelta
{
   [JsonPropertyName("index")] public int Index { get; init; }
   [JsonPropertyName("delta")] public required ChatMessageDelta Delta { get; init; }
   [JsonPropertyName("finish_reason")] public string? FinishReason { get; init; }
}

public sealed class ChatMessageDelta
{
   [JsonPropertyName("role")] public string? Role { get; init; }
   [JsonPropertyName("content")] public string? Content { get; init; }
}


