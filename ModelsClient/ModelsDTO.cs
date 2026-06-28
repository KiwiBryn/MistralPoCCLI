using System.Text.Json.Serialization;

namespace ModelsClient;

public class ModelCapabilities
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

public class FinetunedModelCard : BaseModelCard
{
    [JsonPropertyName("job")]
    public string? Job { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class ModelList
{
    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("data")]
    public required List<BaseModelCard> Data { get; set; }
}

public class DeleteModelResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("deleted")]
    public required bool Deleted { get; set; }
}

public class ArchiveModelResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("object")]
    public required string Object { get; set; }

    [JsonPropertyName("archived")]
    public required bool Archived { get; set; }
}

public class UpdateModelRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
