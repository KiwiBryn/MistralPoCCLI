// Copyright(c) June 2026, devMobile Software
//
namespace MistralBasicFileUploadCLI;

/// <summary>
/// File metadata. Returned from POST /v1/files (upload), GET /v1/files/{id}
/// (retrieve), and as each element of GET /v1/files (list).
/// </summary>
public sealed class FileObject
{
   [JsonPropertyName("id")]          public required string Id { get; init; }
   [JsonPropertyName("object")]      public string? Object { get; init; }            // always "file"
   [JsonPropertyName("size_bytes")]  public long? SizeBytes { get; init; }
   [JsonPropertyName("created_at")]  public required long CreatedAt { get; init; }   // Unix seconds
   [JsonPropertyName("filename")]    public required string Filename { get; init; }
   [JsonPropertyName("purpose")]     public required string Purpose { get; init; }
   [JsonPropertyName("sample_type")] public string? SampleType { get; init; }        // e.g. "pretrain", "instruct"
   [JsonPropertyName("source")]      public string? Source { get; init; }            // "upload" | "repository" | "mistral"
   [JsonPropertyName("num_lines")]   public long? NumLines { get; init; }            // JSONL row count, when known
   [JsonPropertyName("deleted")]     public bool? Deleted { get; init; }             // present on some endpoints
}

/// <summary>
/// Response from GET /v1/files - a page of file metadata.
/// </summary>
public sealed class FileListResponse
{
   [JsonPropertyName("data")]   public required List<FileObject> Data { get; init; }
   [JsonPropertyName("object")] public string? Object { get; init; }                // always "list"
   [JsonPropertyName("total")]  public int? Total { get; init; }
}

/// <summary>
/// Response from DELETE /v1/files/{file_id}.
/// </summary>
public sealed class FileDeleteResponse
{
   [JsonPropertyName("id")]      public required string Id { get; init; }
   [JsonPropertyName("object")]  public string? Object { get; init; }               // always "file"
   [JsonPropertyName("deleted")] public required bool Deleted { get; init; }
}

/// <summary>
/// Response from GET /v1/files/{file_id}/url - a short-lived signed URL
/// for direct download of the file content.
/// </summary>
public sealed class FileSignedUrlResponse
{
   [JsonPropertyName("url")]        public required string Url { get; init; }
   [JsonPropertyName("expires_at")] public long? ExpiresAt { get; init; }           // Unix seconds
}

/// <summary>
/// Known values for the file 'purpose' field. The Mistral API treats this as
/// an open enum (new values may appear), so we model it as const strings
/// rather than a closed C# enum.
/// </summary>
public static class FilePurpose
{
   public const string FineTune = "fine-tune";
   public const string Batch    = "batch";
   public const string Ocr      = "ocr";
}

