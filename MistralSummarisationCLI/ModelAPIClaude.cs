// Copyright(c) June 2026, devMobile Software
// Generated from https://docs.mistral.ai/api/endpoint/models
//
namespace devMobile.Mistral.Models;

using System.Text.Json.Serialization;

// ---------------------------------------------------------------------------
// Shared sub-objects
// ---------------------------------------------------------------------------

public sealed class ModelCapabilities
{
   [JsonPropertyName("completion_chat")]
   public bool CompletionChat { get; init; }

   [JsonPropertyName("completion_fim")]
   public bool CompletionFim { get; init; }

   [JsonPropertyName("function_calling")]
   public bool FunctionCalling { get; init; }

   [JsonPropertyName("fine_tuning")]
   public bool FineTuning { get; init; }

   [JsonPropertyName("vision")]
   public bool Vision { get; init; }

   [JsonPropertyName("classification")]
   public bool Classification { get; init; }
}

// ---------------------------------------------------------------------------
// Model card  (BaseModelCard + FTModelCard combined)
// ---------------------------------------------------------------------------

public sealed class ModelCard
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; } // always "model"

   [JsonPropertyName("created")]
   public required long Created { get; init; }

   [JsonPropertyName("owned_by")]
   public required string OwnedBy { get; init; }

   [JsonPropertyName("max_context_length")]
   public required int MaxContextLength { get; init; }

   [JsonPropertyName("capabilities")]
   public required ModelCapabilities Capabilities { get; init; }

   [JsonPropertyName("aliases")]
   public List<string>? Aliases { get; init; }

   [JsonPropertyName("archived")]
   public bool? Archived { get; init; }

   // FTModelCard-only fields
   [JsonPropertyName("job")]
   public string? Job { get; init; }

   [JsonPropertyName("root")]
   public string? Root { get; init; }

   [JsonPropertyName("TYPE")]
   public string? Type { get; init; } // "fine-tuned" when present
}

// ---------------------------------------------------------------------------
// List models  GET /v1/models
// ---------------------------------------------------------------------------

public sealed class ModelListResponse
{
   [JsonPropertyName("object")]
   public required string Object { get; init; } // always "list"

   [JsonPropertyName("data")]
   public required List<ModelCard> Data { get; init; }
}

// ---------------------------------------------------------------------------
// Delete model  DELETE /v1/models/{model_id}
// ---------------------------------------------------------------------------

public sealed class ModelDeleteResponse
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; } // always "model"

   [JsonPropertyName("deleted")]
   public required bool Deleted { get; init; }
}

// ---------------------------------------------------------------------------
// Archive / Unarchive  POST|DELETE /v1/fine_tuning/models/{model_id}/archive
// ---------------------------------------------------------------------------

public sealed class ModelArchiveResponse
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public required string Object { get; init; } // always "model"

   [JsonPropertyName("archived")]
   public required bool Archived { get; init; }
}

// ---------------------------------------------------------------------------
// Update fine-tuned model  PATCH /v1/fine_tuning/models/{model_id}
// ---------------------------------------------------------------------------

public sealed class UpdateFineTunedModelRequest
{
   [JsonPropertyName("name")]
   public string? Name { get; init; }

   [JsonPropertyName("description")]
   public string? Description { get; init; }
}

// Response from PATCH and archive/unarchive endpoints
public sealed class FineTunedModel
{
   [JsonPropertyName("id")]
   public required string Id { get; init; }

   [JsonPropertyName("object")]
   public string? Object { get; init; }

   [JsonPropertyName("created")]
   public long? Created { get; init; }

   [JsonPropertyName("owned_by")]
   public string? OwnedBy { get; init; }

   [JsonPropertyName("root")]
   public string? Root { get; init; }

   [JsonPropertyName("root_version")]
   public string? RootVersion { get; init; }

   [JsonPropertyName("workspace_id")]
   public string? WorkspaceId { get; init; }

   [JsonPropertyName("archived")]
   public bool? Archived { get; init; }

   [JsonPropertyName("capabilities")]
   public ModelCapabilities? Capabilities { get; init; }
}

// ---------------------------------------------------------------------------
// Source-generated serialisation context
// ---------------------------------------------------------------------------

[JsonSerializable(typeof(ModelListResponse))]
[JsonSerializable(typeof(ModelCard))]
[JsonSerializable(typeof(ModelCapabilities))]
[JsonSerializable(typeof(ModelDeleteResponse))]
[JsonSerializable(typeof(ModelArchiveResponse))]
[JsonSerializable(typeof(UpdateFineTunedModelRequest))]
[JsonSerializable(typeof(FineTunedModel))]
public partial class MistralModelsJsonContext : JsonSerializerContext
{
}
