// Copyright(c) June 2026, devMobile Software
//
// This program demonstrates the Mistral Files API: multipart upload, list,
// signed-URL retrieval, and delete, all using strongly typed request/response models.
//
using MistralBasicFieldExtractionCLI.Models;

Console.WriteLine("################");
Console.WriteLine("   ##      ##   ");
Console.WriteLine("   ##      ##   ");
Console.WriteLine("     ##  ##     ");
Console.WriteLine("   ##########   ");
Console.WriteLine("   ##  ##  ##   ");
Console.WriteLine(" ####  ##  #### ");
Console.WriteLine("################");
Console.WriteLine();

IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new InvalidOperationException("Failed to load application settings");

// Create HttpClient with required headers. Timeout bumped from the 100s default - large file uploads can exceed that.
using HttpClient httpClient = new()
{
   DefaultRequestHeaders =
   {
      Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
      Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey)
   },
   BaseAddress = new Uri(settings.BaseUrl),
   Timeout = TimeSpan.FromMinutes(5),
};

var jsonSerializerOptions = new JsonSerializerOptions()
{
   // PropertyNamingPolicy removed - [JsonPropertyName] attributes on model handle wire names
   WriteIndented = false,
   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
   AllowTrailingCommas = false,
   ReadCommentHandling = JsonCommentHandling.Disallow,
   UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
};

Console.Write("Enter command (upload, list, extract) (Enter to exit): ");
var operation = Console.ReadLine()?.Trim().Trim('"');

while (!string.IsNullOrWhiteSpace(operation))
{
   try
   {
      switch (operation?.ToLower())
      {
         case "upload":
            { 
            Console.Write("Enter path to file to upload (Enter to exit): ");
            var filePath = Console.ReadLine()?.Trim().Trim('"');
            if (!File.Exists(filePath))
            {
               Console.WriteLine($"File not found: {filePath}");
               Console.WriteLine();
               Console.Write("Enter path to file to upload (Enter to exit): ");
               filePath = Console.ReadLine()?.Trim().Trim('"');
               continue;
            }

            await using var fileStream = File.OpenRead(filePath);
            var uploadRequest = new FileUploadRequest
            {
               Content = fileStream,
               FileName = Path.GetFileName(filePath),
               Purpose = FilePurpose.Ocr,   // change to FineTune / Batch as needed
            };

            var uploaded = await UploadFileAsync(httpClient, uploadRequest, jsonSerializerOptions);
            if (uploaded is null)
            {
               Console.WriteLine("Upload returned no payload.");
               continue;
            }

            Console.WriteLine();
            Console.WriteLine($"Uploaded:  {uploaded.Id}");
            Console.WriteLine($"  Name:    {uploaded.Filename}");
            Console.WriteLine($"  Size:    {uploaded.SizeBytes:N0} bytes");
            Console.WriteLine($"  Purpose: {uploaded.Purpose}");
            Console.WriteLine($"  Created: {DateTimeOffset.FromUnixTimeSeconds(uploaded.CreatedAt):u}");
         }
         break;
         case "list":
            //{
               Console.WriteLine("Listing files...");
               var list = await ListFilesAsync(httpClient, jsonSerializerOptions);
               if (list is not null)
               {
                  Console.WriteLine();
                  Console.WriteLine($"Files on account ({list.Total ?? list.Data.Count}):");
                  foreach (var f in list.Data)
                  {
                     Console.WriteLine($"  {f.Id}  {f.SizeBytes,12:N0}  {f.Purpose,-10}  {f.Filename}");
                  }
               }
            //}
            break;
         case "extract":
            {
               Console.WriteLine();
               Console.Write("Enter file ID to proces (Enter to cancel): ");
               var fileId = Console.ReadLine()?.Trim().Trim('"');
               if (string.IsNullOrWhiteSpace(fileId))
               {
                  break;
               }

               Console.Write("Enter prompt file name(Enter to exit): ");
               var promptFileName = Console.ReadLine()?.Trim().Trim('"');
               if (string.IsNullOrWhiteSpace(promptFileName ))
               {
                  break;
               }
               Console.WriteLine();

               var extract = await GetExtractedFieldsAsync(httpClient, settings.ModelName, fileId, promptFileName, jsonSerializerOptions);

               if (extract is null)
               {
                  Console.WriteLine("Extraction returned no payload.");
                  break;
               }
               Console.WriteLine(extract.Fields);

               Console.WriteLine();
               if (extract.Usage != null)
               {
                  Console.WriteLine($"Prompt tokens: {extract.Usage.PromptTokens}");
                  Console.WriteLine($"Completion tokens: {extract.Usage.CompletionTokens}");
                  Console.WriteLine($"Total tokens: {extract.Usage.TotalTokens}");
               }
               Console.WriteLine();
            }
            break;
         default:
            Console.WriteLine("Unknown operation. Defaulting to full workflow (upload, list, extract, ocr).");
            break;
      }
   }
   catch (HttpRequestException ex)
   {
      Console.WriteLine($"Request failed: {(int?)ex.StatusCode} {ex.Message}");
   }
   catch (TaskCanceledException)
   {
      Console.WriteLine("Request timed out.");
   }
   catch (JsonException ex)
   {
      Console.WriteLine($"Failed to parse response: {ex.Message}");
   }
   catch (IOException ex)
   {
      Console.WriteLine($"Could not read file: {ex.Message}");
   }

   Console.WriteLine();
   Console.Write("Enter command (upload, list, extract ) (Enter to exit): ");
   operation = Console.ReadLine()?.Trim().Trim('"');
}


// ---- Local helper: POST /v1/files using multipart/form-data ----
static async Task<FileObject?> UploadFileAsync(
   HttpClient http,
   FileUploadRequest req,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var multipart = new MultipartFormDataContent();

   // The 'file' part - stream the file content, octet-stream is the safe default.
   var fileContent = new StreamContent(req.Content);
   fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
   multipart.Add(fileContent, name: "file", fileName: req.FileName);

   // The 'purpose' form field.
   multipart.Add(new StringContent(req.Purpose), name: "purpose");

   using var response = await http.PostAsync("files", multipart, ct);
   response.EnsureSuccessStatusCode();

   return await response.Content.ReadFromJsonAsync<FileObject>(options, ct);
}


static async Task<FileListResponse?> ListFilesAsync(
   HttpClient http,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.GetAsync("files", ct);
   response.EnsureSuccessStatusCode();

   return await response.Content.ReadFromJsonAsync<FileListResponse>(options, ct);
}

static async Task<FileSignedUrlResponse?> GetSignedUrlAsync(
   HttpClient http,
   string fileId,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.GetAsync($"files/{fileId}/url", ct);
   response.EnsureSuccessStatusCode();

   return await response.Content.ReadFromJsonAsync<FileSignedUrlResponse>(options, ct);
}


static async Task<FieldExtractionResponse?> GetExtractedFieldsAsync(
   HttpClient httpClient,
   string modelName,
   string fileId,
   string promptFile,
   JsonSerializerOptions jsonSerializerOptions,
   CancellationToken ct = default)
{
   if (!File.Exists(promptFile))
   {
      Console.WriteLine($"Prompt file not found: {promptFile}");
      return null;
   }
   string promptText = await File.ReadAllTextAsync(promptFile, ct);

   FileSignedUrlResponse? signed = await GetSignedUrlAsync(httpClient, fileId, jsonSerializerOptions, ct);
   if (signed is null)
   {
      Console.WriteLine($"Could not resolve signed URL for file {fileId}");
      return null;
   }


   var request = new MultiModalChatCompletionRequest
   {
      Model = modelName,
      Temperature = 0,
      Messages =
      [
         new MultiModalChatMessage
         {
            Role = "user",
            Content =
            [
               ChatContentPart.DocumentFromUrl(signed.Url),
               ChatContentPart.FromText(promptText),
            ],
         },
      ],
      ResponseFormat = new ResponseFormat { Type = "json_object" },
   };

   using var httpResponse = await httpClient.PostAsJsonAsync("chat/completions", request, jsonSerializerOptions, ct);
   httpResponse.EnsureSuccessStatusCode();

   ChatCompletionResponse? completion = await httpResponse.Content.ReadFromJsonAsync<ChatCompletionResponse>(jsonSerializerOptions, ct);
   if (completion is null || completion.Choices.Count == 0)
   {
      return null;
   }


   return new FieldExtractionResponse
   {
      Fields = completion.Choices[0].Message.Content,
      Usage = completion.Usage is null
         ? null
         : new FieldExtractionUsage
         {
            PromptTokens = completion.Usage.PromptTokens,
            CompletionTokens = completion.Usage.CompletionTokens,
            TotalTokens = completion.Usage.TotalTokens,
         },
   };
}

public sealed class FieldExtractionResponse
{
   [JsonPropertyName("fields")] public required string Fields { get; init; }
   [JsonPropertyName("usage")]  public FieldExtractionUsage? Usage { get; init; }
}

/// <summary>Token accounting for the field-extraction call.</summary>
public sealed class FieldExtractionUsage
{
   [JsonPropertyName("prompt_tokens")]     public int? PromptTokens { get; init; }
   [JsonPropertyName("completion_tokens")] public int? CompletionTokens { get; init; }
   [JsonPropertyName("total_tokens")]      public int? TotalTokens { get; init; }
}
