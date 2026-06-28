// Copyright(c) June 2026, devMobile Software

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ChatCompletionClient;

// SSE chunk emitted when stream: true — object = "chat.completion.chunk"
public sealed class ChatCompletionChunk
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
    public required ChatCompletionChunkChoice[] Choices { get; set; }

    // Only present on the final chunk
    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }
}

public sealed class ChatCompletionChunkChoice
{
    [JsonPropertyName("index")]
    public required int Index { get; set; }

    [JsonPropertyName("delta")]
    public required MessageDelta Delta { get; set; }

    // null on all but the final chunk: "stop","length","model_length","error","tool_calls"
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("logprobs")]
    public JsonNode? Logprobs { get; set; }
}

public sealed class MessageDelta
{
    // Only in the first chunk
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    // string, ContentChunk[], or absent
    [JsonPropertyName("content")]
    public JsonNode? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public ToolCallDelta[]? ToolCalls { get; set; }
}

// Streaming tool-call fragment. Multi-chunk tool calls are reassembled by Index;
// Id/Function.Name typically arrive on the first fragment, with Function.Arguments
// streamed across subsequent fragments and concatenated by the consumer.
public sealed class ToolCallDelta
{
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public FunctionCallDelta? Function { get; set; }
}

public sealed class FunctionCallDelta
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    // Partial JSON-encoded arguments fragment; concatenate across deltas with the
    // same Index to reconstruct the full arguments string.
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}
