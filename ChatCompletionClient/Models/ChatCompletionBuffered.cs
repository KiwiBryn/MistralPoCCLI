// Copyright(c) June 2026, devMobile Software
// Mistral AI Chat Completion API — buffered (non-streaming) response types
// Endpoint: POST /v1/chat/completions (stream: false)
// Documentation: https://docs.mistral.ai/api/endpoint/chat

using System.Text.Json.Serialization;


namespace MistralAI.Chat.Sync;

public sealed class ChatCompletionResponse
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; } // "chat.completion"

   [JsonPropertyName("created")]
   public required long Created { get; init; }

   [JsonPropertyName("model")]
   public required string Model { get; init; }

   [JsonPropertyName("choices")]
   public required List<BufferedChoice> Choices { get; init; }

   [JsonPropertyName("usage")]
   public UsageInfo? Usage { get; init; }
}

public sealed class BufferedChoice
{
   [JsonPropertyName("index")]
   public int Index { get; init; }

   [JsonPropertyName("message")]
   public required AssistantMessage Message { get; init; }

   [JsonPropertyName("finish_reason")]
   public string? FinishReason { get; init; } // "stop" | "length" | "tool_calls" | "error"
}
