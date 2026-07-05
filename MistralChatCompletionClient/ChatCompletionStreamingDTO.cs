// Copyright(c) June 2026, devMobile Software
// Mistral AI Chat Completion API — streaming response types (SSE / text/event-stream)
// Endpoint: POST /v1/chat/completions (stream: true)
// Documentation: https://docs.mistral.ai/api/endpoint/chat
namespace devMobile.MistralAI;

// Each SSE data: line deserialises to one of these.
// The final chunk (before data: [DONE]) carries a non-null Usage.
public sealed class ChatCompletionChunkEvent
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public string? Object { get; init; } // "chat.completion.chunk"

   [JsonPropertyName("created")]
   public long Created { get; init; }

   [JsonPropertyName("model")]
   public string? Model { get; init; }

   [JsonPropertyName("choices")]
   public required List<ChunkChoice> Choices { get; init; }

   [JsonPropertyName("usage")]
   public UsageInfo? Usage { get; init; }
}

public sealed class ChunkChoice
{
   [JsonPropertyName("index")]
   public int Index { get; init; }

   [JsonPropertyName("delta")]
   public required ChunkDelta Delta { get; init; }

   [JsonPropertyName("finish_reason")]
   public string? FinishReason { get; init; } // "stop" | "length" | "tool_calls" | "error" | null
}

public sealed class ChunkDelta
{
   [JsonPropertyName("role")]
   public string? Role { get; init; }

   // string for text; array of content parts for multi-modal; null on tool-call-only turns
   [JsonPropertyName("content")]
   public JsonNode? Content { get; init; }

   [JsonPropertyName("tool_calls")]
   public List<ChunkToolCall>? ToolCalls { get; init; }
}

public sealed class ChunkToolCall
{
   [JsonPropertyName("id")]
   public string? Id { get; init; }

   [JsonPropertyName("type")]
   public string? Type { get; init; }

   [JsonPropertyName("function")]
   public ChunkToolCallFunction? Function { get; init; }
}

public sealed class ChunkToolCallFunction
{
   [JsonPropertyName("name")]
   public string? Name { get; init; }

   // Streamed in fragments — accumulate across chunks before parsing
   [JsonPropertyName("arguments")]
   public string? Arguments { get; init; }
}
