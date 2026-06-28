// Copyright(c) June 2026, devMobile Software
// Mistral AI API DTOs — https://docs.mistral.ai/api/

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MistralClient;

// ============================================================================
// ENUMERATIONS
// ============================================================================

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilePurpose
{
    [JsonStringEnumMemberName("fine-tune")]
    FineTune,
    [JsonStringEnumMemberName("batch")]
    Batch,
    [JsonStringEnumMemberName("ocr")]
    Ocr,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileVisibility
{
    [JsonStringEnumMemberName("workspace")]
    Workspace,
    [JsonStringEnumMemberName("user")]
    User,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileSource
{
    [JsonStringEnumMemberName("upload")]
    Upload,
    [JsonStringEnumMemberName("repository")]
    Repository,
    [JsonStringEnumMemberName("mistral")]
    Mistral,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileSampleType
{
    [JsonStringEnumMemberName("ocr_input")]
    OcrInput,
    [JsonStringEnumMemberName("pretrain")]
    Pretrain,
    [JsonStringEnumMemberName("instruct")]
    Instruct,
    [JsonStringEnumMemberName("batch_request")]
    BatchRequest,
    [JsonStringEnumMemberName("batch_result")]
    BatchResult,
    [JsonStringEnumMemberName("batch_error")]
    BatchError,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BatchJobStatus
{
    [JsonStringEnumMemberName("QUEUED")]
    Queued,
    [JsonStringEnumMemberName("RUNNING")]
    Running,
    [JsonStringEnumMemberName("SUCCESS")]
    Success,
    [JsonStringEnumMemberName("FAILED")]
    Failed,
    [JsonStringEnumMemberName("TIMEOUT_EXCEEDED")]
    TimeoutExceeded,
    [JsonStringEnumMemberName("CANCELLATION_REQUESTED")]
    CancellationRequested,
    [JsonStringEnumMemberName("CANCELLED")]
    Cancelled,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmbeddingEncodingFormat
{
    [JsonStringEnumMemberName("float")]
    Float,
    [JsonStringEnumMemberName("base64")]
    Base64,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmbeddingOutputDtype
{
    [JsonStringEnumMemberName("float")]
    Float,
    [JsonStringEnumMemberName("int8")]
    Int8,
    [JsonStringEnumMemberName("uint8")]
    UInt8,
    [JsonStringEnumMemberName("binary")]
    Binary,
    [JsonStringEnumMemberName("ubinary")]
    UBinary,
}

// ============================================================================
// SHARED TYPES
// ============================================================================

public sealed class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public required int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public required int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public required int TotalTokens { get; set; }
}

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
    public required ImageUrlDetail ImageUrl { get; set; }
}

public sealed class ImageUrlDetail
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

// ============================================================================
// CHAT COMPLETIONS — POST /v1/chat/completions
// ============================================================================

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

// tool_choice: "auto", "none", "any", "required", or a specific function object
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

public sealed class GuardrailConfig
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("keywords")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Keywords { get; set; }
}

public sealed class Prediction
{
    [JsonPropertyName("type")]
    public string Type => "content";

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}

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

    // string or string[]
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Stop { get; set; }

    [JsonPropertyName("random_seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RandomSeed { get; set; }

    [JsonPropertyName("safe_prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SafePrompt { get; set; }

    // "auto", "none", "any", "required", or ToolChoiceFunction object
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

    // "none","minimal","low","medium","high","xhigh"
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }

    // "reasoning"
    [JsonPropertyName("prompt_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromptMode { get; set; }

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

    // "stop","length","model_length","error","tool_calls"
    [JsonPropertyName("finish_reason")]
    public required string FinishReason { get; set; }

    [JsonPropertyName("logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Logprobs { get; set; }
}

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

    // null on all but the final chunk
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
    public ToolCall[]? ToolCalls { get; set; }
}

// ============================================================================
// FIM COMPLETIONS — POST /v1/fim/completions
// ============================================================================

public sealed class FimCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("suffix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Suffix { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("min_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinTokens { get; set; }

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    // string or string[]
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Stop { get; set; }

    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Stream { get; set; }

    [JsonPropertyName("random_seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RandomSeed { get; set; }

    [JsonPropertyName("prompt_cache_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromptCacheKey { get; set; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class FimCompletionResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("created")]
    public required long Created { get; set; }

    [JsonPropertyName("choices")]
    public required ChatCompletionChoice[] Choices { get; set; }

    [JsonPropertyName("usage")]
    public required UsageInfo Usage { get; set; }
}

// ============================================================================
// EMBEDDINGS — POST /v1/embeddings
// ============================================================================

public sealed class EmbeddingRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    // string or string[]
    [JsonPropertyName("input")]
    public required JsonNode Input { get; set; }

    [JsonPropertyName("encoding_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmbeddingEncodingFormat? EncodingFormat { get; set; }

    [JsonPropertyName("output_dimension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OutputDimension { get; set; }

    [JsonPropertyName("output_dtype")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmbeddingOutputDtype? OutputDtype { get; set; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class EmbeddingResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("data")]
    public required EmbeddingData[] Data { get; set; }

    [JsonPropertyName("usage")]
    public required UsageInfo Usage { get; set; }
}

public sealed class EmbeddingData
{
    [JsonPropertyName("index")]
    public required int Index { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("embedding")]
    public required float[] Embedding { get; set; }
}

// ============================================================================
// FILES API — /v1/files
// ============================================================================

public sealed class FileObject
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("filename")]
    public required string Filename { get; set; }

    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("purpose")]
    public FilePurpose Purpose { get; set; }

    [JsonPropertyName("source")]
    public FileSource Source { get; set; }

    [JsonPropertyName("sample_type")]
    public FileSampleType SampleType { get; set; }

    [JsonPropertyName("num_lines")]
    public int? NumLines { get; set; }

    [JsonPropertyName("mimetype")]
    public string? MimeType { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("visibility")]
    public FileVisibility? Visibility { get; set; }

    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; set; }

    [JsonPropertyName("deleted")]
    public bool? Deleted { get; set; }
}

public sealed class FileListResponse
{
    [JsonPropertyName("data")]
    public required List<FileObject> Data { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }
}

public sealed class FileDeleteResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("deleted")]
    public required bool Deleted { get; set; }
}

public sealed class FileSignedUrlResponse
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

// ============================================================================
// MODELS API — /v1/models
// ============================================================================

public sealed class ModelCapabilities
{
    [JsonPropertyName("audio")]
    public bool Audio { get; set; }

    [JsonPropertyName("audio_speech")]
    public bool AudioSpeech { get; set; }

    [JsonPropertyName("audio_transcription")]
    public bool AudioTranscription { get; set; }

    [JsonPropertyName("audio_transcription_realtime")]
    public bool AudioTranscriptionRealtime { get; set; }

    [JsonPropertyName("classification")]
    public bool Classification { get; set; }

    [JsonPropertyName("completion_chat")]
    public bool CompletionChat { get; set; }

    [JsonPropertyName("completion_fim")]
    public bool CompletionFim { get; set; }

    [JsonPropertyName("fine_tuning")]
    public bool FineTuning { get; set; }

    [JsonPropertyName("function_calling")]
    public bool FunctionCalling { get; set; }

    [JsonPropertyName("moderation")]
    public bool Moderation { get; set; }

    [JsonPropertyName("ocr")]
    public bool Ocr { get; set; }

    [JsonPropertyName("reasoning")]
    public bool Reasoning { get; set; }

    [JsonPropertyName("vision")]
    public bool Vision { get; set; }
}

public class BaseModelCard
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("owned_by")]
    public string? OwnedBy { get; set; }

    [JsonPropertyName("root")]
    public string? Root { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("max_context_length")]
    public int? MaxContextLength { get; set; }

    [JsonPropertyName("capabilities")]
    public ModelCapabilities? Capabilities { get; set; }

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }
}

public sealed class FinetunedModelCard : BaseModelCard
{
    [JsonPropertyName("job")]
    public string? Job { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class ModelList
{
    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("data")]
    public required List<BaseModelCard> Data { get; set; }
}

public sealed class DeleteModelResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("deleted")]
    public required bool Deleted { get; set; }
}

public sealed class ArchiveModelResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("archived")]
    public required bool Archived { get; set; }
}

public sealed class UpdateModelRequest
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}

// ============================================================================
// BATCH API — /v1/batch/jobs
// ============================================================================

public sealed class BatchJobRequest
{
    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; set; }

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("input_files")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? InputFiles { get; set; }

    // Inline request objects (alternative to input_files)
    [JsonPropertyName("requests")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode[]? Requests { get; set; }

    [JsonPropertyName("timeout_hours")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TimeoutHours { get; set; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class BatchJob
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("status")]
    public required BatchJobStatus Status { get; set; }

    [JsonPropertyName("created_at")]
    public required long CreatedAt { get; set; }

    [JsonPropertyName("started_at")]
    public long? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public long? CompletedAt { get; set; }

    [JsonPropertyName("total_requests")]
    public int TotalRequests { get; set; }

    [JsonPropertyName("succeeded_requests")]
    public int SucceededRequests { get; set; }

    [JsonPropertyName("failed_requests")]
    public int FailedRequests { get; set; }

    [JsonPropertyName("completed_requests")]
    public int CompletedRequests { get; set; }

    [JsonPropertyName("input_files")]
    public List<string>? InputFiles { get; set; }

    [JsonPropertyName("output_file")]
    public string? OutputFile { get; set; }

    [JsonPropertyName("error_file")]
    public string? ErrorFile { get; set; }

    [JsonPropertyName("errors")]
    public List<BatchError>? Errors { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class BatchError
{
    [JsonPropertyName("message")]
    public required string Message { get; set; }
}

public sealed class BatchJobList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public required List<BatchJob> Data { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }
}

public sealed class BatchJobDeleteResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}

// ============================================================================
// OCR — POST /v1/ocr
// ============================================================================

// OCR document input — polymorphic on "type"
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OcrFileChunk), "file")]
[JsonDerivedType(typeof(OcrDocumentUrlChunk), "document_url")]
[JsonDerivedType(typeof(OcrImageUrlChunk), "image_url")]
public abstract class OcrDocument { }

public sealed class OcrFileChunk : OcrDocument
{
    [JsonPropertyName("file_id")]
    public required string FileId { get; set; }
}

public sealed class OcrDocumentUrlChunk : OcrDocument
{
    [JsonPropertyName("document_url")]
    public required string DocumentUrl { get; set; }
}

public sealed class OcrImageUrlChunk : OcrDocument
{
    [JsonPropertyName("image_url")]
    public required string ImageUrl { get; set; }
}

public sealed class OcrRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("document")]
    public required OcrDocument Document { get; set; }

    // string, int[], or null
    [JsonPropertyName("pages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Pages { get; set; }

    [JsonPropertyName("include_image_base64")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeImageBase64 { get; set; }

    [JsonPropertyName("image_limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ImageLimit { get; set; }

    [JsonPropertyName("image_min_size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ImageMinSize { get; set; }

    [JsonPropertyName("extract_header")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ExtractHeader { get; set; }

    [JsonPropertyName("extract_footer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ExtractFooter { get; set; }

    [JsonPropertyName("include_blocks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IncludeBlocks { get; set; }

    [JsonPropertyName("document_annotation_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseFormat? DocumentAnnotationFormat { get; set; }

    [JsonPropertyName("document_annotation_prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentAnnotationPrompt { get; set; }

    [JsonPropertyName("bbox_annotation_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseFormat? BboxAnnotationFormat { get; set; }

    // "word" or "page"
    [JsonPropertyName("confidence_scores_granularity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConfidenceScoresGranularity { get; set; }

    // "markdown" or "html"
    [JsonPropertyName("table_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TableFormat { get; set; }
}

public sealed class OcrResponse
{
    [JsonPropertyName("pages")]
    public required OcrPageObject[] Pages { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("usage_info")]
    public required OcrUsageInfo UsageInfo { get; set; }
}

public sealed class OcrPageObject
{
    [JsonPropertyName("index")]
    public required int Index { get; set; }

    [JsonPropertyName("markdown")]
    public required string Markdown { get; set; }

    [JsonPropertyName("images")]
    public OcrImageObject[]? Images { get; set; }

    [JsonPropertyName("dimensions")]
    public OcrPageDimensions? Dimensions { get; set; }
}

public sealed class OcrImageObject
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("top_left_x")]
    public int TopLeftX { get; set; }

    [JsonPropertyName("top_left_y")]
    public int TopLeftY { get; set; }

    [JsonPropertyName("bottom_right_x")]
    public int BottomRightX { get; set; }

    [JsonPropertyName("bottom_right_y")]
    public int BottomRightY { get; set; }

    [JsonPropertyName("image_base64")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageBase64 { get; set; }
}

public sealed class OcrPageDimensions
{
    [JsonPropertyName("dpi")]
    public int Dpi { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }
}

public sealed class OcrUsageInfo
{
    [JsonPropertyName("pages_processed")]
    public int PagesProcessed { get; set; }

    [JsonPropertyName("doc_size_bytes")]
    public long DocSizeBytes { get; set; }
}
