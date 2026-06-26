// Copyright(c) June 2026, devMobile Software
// Generated from https://docs.mistral.ai/api/endpoint/files
//
namespace devMobile.Mistral.ModelsX;

using System.Text.Json.Serialization;

// ---------------------------------------------------------------------------
// Shared schema
// ---------------------------------------------------------------------------

public sealed class FileSchema
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; } // always "file"

   [JsonPropertyName("bytes")]
   public int? Bytes { get; init; }

   [JsonPropertyName("created_at")]
   public required long CreatedAt { get; init; }

   [JsonPropertyName("expires_at")]
   public long? ExpiresAt { get; init; }

   [JsonPropertyName("filename")]
   public required string Filename { get; init; }

   [JsonPropertyName("purpose")]
   public required FilePurpose Purpose { get; init; }

   [JsonPropertyName("sample_type")]
   public required SampleType SampleType { get; init; }

   [JsonPropertyName("source")]
   public required FileSource Source { get; init; }

   [JsonPropertyName("num_lines")]
   public int? NumLines { get; init; }

   [JsonPropertyName("mimetype")]
   public string? MimeType { get; init; }

   [JsonPropertyName("signature")]
   public string? Signature { get; init; }

   [JsonPropertyName("deleted")]
   public bool? Deleted { get; init; }

   [JsonPropertyName("visibility")]
   public FileVisibility? Visibility { get; init; }
}

// ---------------------------------------------------------------------------
// List files  GET /v1/files
// ---------------------------------------------------------------------------

public sealed class FileListResponse
{
   [JsonPropertyName("object")]
   public required string Object { get; init; }

   [JsonPropertyName("data")]
   public required List<FileSchema> Data { get; init; }

   [JsonPropertyName("total")]
   public int? Total { get; init; }
}

// ---------------------------------------------------------------------------
// Upload file  POST /v1/files  (multipart/form-data)
// ---------------------------------------------------------------------------

public sealed class FileUploadRequest
{
   [JsonPropertyName("file")]
   public required UploadFileContent File { get; init; }

   [JsonPropertyName("purpose")]
   public required FilePurpose Purpose { get; init; }

   [JsonPropertyName("visibility")]
   public FileVisibility? Visibility { get; init; }

   [JsonPropertyName("expiry")]
   public int? Expiry { get; init; }
}

public sealed class UploadFileContent
{
   [JsonPropertyName("fileName")]
   public required string FileName { get; init; }

   [JsonPropertyName("content")]
   public required string ContentBase64 { get; init; }
}

// ---------------------------------------------------------------------------
// Delete file  DELETE /v1/files/{file_id}
// ---------------------------------------------------------------------------

public sealed class FileDeleteResponse
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; }

   [JsonPropertyName("deleted")]
   public required bool Deleted { get; init; }
}

// ---------------------------------------------------------------------------
// Get signed URL  GET /v1/files/{file_id}/url
// ---------------------------------------------------------------------------

public sealed class FileSignedUrlResponse
{
   [JsonPropertyName("url")]
   public required string Url { get; init; }
}

// ---------------------------------------------------------------------------
// Enums
// ---------------------------------------------------------------------------

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilePurpose
{
   [JsonStringEnumMemberName("fine-tune")]
   FineTune,

   [JsonStringEnumMemberName("batch")]
   Batch,

   [JsonStringEnumMemberName("ocr")]
   Ocr
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SampleType
{
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

   [JsonStringEnumMemberName("ocr_input")]
   OcrInput
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileSource
{
   [JsonStringEnumMemberName("upload")]
   Upload,

   [JsonStringEnumMemberName("repository")]
   Repository,

   [JsonStringEnumMemberName("mistral")]
   Mistral
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileVisibility
{
   [JsonStringEnumMemberName("workspace")]
   Workspace,

   [JsonStringEnumMemberName("user")]
   User
}
