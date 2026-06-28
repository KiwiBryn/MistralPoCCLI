// Copyright(c) June 2026, devMobile Software
using System.Text.Json.Serialization;

// https://docs.mistral.ai/api/endpoint/files

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

public class FileObject
{
   [JsonPropertyName("id")]
   public string Id { get; set; } = string.Empty;

   [JsonPropertyName("object")]
   public string Object { get; set; } = string.Empty;

   [JsonPropertyName("filename")]
   public string Filename { get; set; } = string.Empty;

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

public class FileListResponse
{
   [JsonPropertyName("data")]
   public List<FileObject> Data { get; set; } = [];

   [JsonPropertyName("object")]
   public string Object { get; set; } = string.Empty;

   [JsonPropertyName("total")]
   public int? Total { get; set; }
}

public class FileDeleteResponse
{
   [JsonPropertyName("id")]
   public string Id { get; set; } = string.Empty;

   [JsonPropertyName("object")]
   public string Object { get; set; } = string.Empty;

   [JsonPropertyName("deleted")]
   public bool Deleted { get; set; }
}

public class FileSignedUrlResponse
{
   [JsonPropertyName("url")]
   public string Url { get; set; } = string.Empty;
}
