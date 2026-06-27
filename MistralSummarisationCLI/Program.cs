// Copyright(c) June 2026, devMobile Software
//
// This program is nasty, it demonstrates the Mistral Files API: multipart upload, list,
// signed-URL retrieval, and delete, all using strongly typed request/response models.
//
using devMobile.Mistral.Models;
using System.Text;


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

Console.Write("Enter command (upload, list, url, delete, summarise) or path to file to upload (Enter to exit): ");
var operation = Console.ReadLine()?.Trim().Trim('"');

while (!string.IsNullOrWhiteSpace(operation))
{
   try
   {
      switch (operation?.ToLower())
      {
         case "exit":
            return;
         //case "upload":
         //    break;
         case "list":
            {
               using var response = await httpClient.GetAsync("files");
               response.EnsureSuccessStatusCode();

               var responseContent = await response.Content.ReadAsStringAsync();

               try
               {
                  FileListResponse listResponse = JsonSerializer.Deserialize<FileListResponse>(responseContent, jsonSerializerOptions);

                  foreach (var file in listResponse.Data)
                  {
                     Console.WriteLine($"ID: {file.Id} File: {file.Filename}, Size: {file.Bytes}, Created: {file.CreatedAt}");
                  }
               }
               catch (JsonException ex)
               {
                  Console.WriteLine($"Failed to parse response: {ex.Message}");
               }
            }
            break;
         //case "url":
         //   break;
         //case "delete":
         //    break;
         case "summarise":
         case "summarize":
            {
               Console.Write("Enter file ID to Summariser (Enter to cancel): ");
               var fileId = Console.ReadLine()?.Trim().Trim('"');
               if (string.IsNullOrWhiteSpace(fileId))
               {
                  break;
               }

               // 1. Resolve a short-lived signed URL for the uploaded file.
               FileSignedUrlResponse? signed;
               using (var urlResponse = await httpClient.GetAsync($"files/{fileId}/url"))
               {
                  urlResponse.EnsureSuccessStatusCode();
                  signed = await urlResponse.Content.ReadFromJsonAsync<FileSignedUrlResponse>(jsonSerializerOptions);
               }
               if (signed is null)
               {
                  Console.WriteLine($"Failed to get signed URL for {fileId}");
                  break;
               }

               // 2. Multi-modal user message: document URL + text instruction.
               var chatCompletionRequest = new ChatCompletionRequest
               {
                  Model = settings.ModelName,
                  Messages =
                  [
                     //UserMessage.FromParts(new DocumentUrlPart { DocumentName = "Program.cs", DocumentUrl = signed.Url },new TextPart{ Text = "Summarise this document in three concise bullet points." })
                     UserMessage.FromText($"Summarise the document at {signed.Url} in three concise bullet points.")
                  ],
               };

               var jsonContent = new StringContent(
                  JsonSerializer.Serialize(chatCompletionRequest, jsonSerializerOptions),
                  Encoding.UTF8,
                  "application/json");

               using var response = await httpClient.PostAsync("chat/completions", jsonContent);
               response.EnsureSuccessStatusCode();
               var responseContent = await response.Content.ReadAsStringAsync();

               try
               {
                  var summariseResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, jsonSerializerOptions);
                  foreach (var choice in summariseResponse!.Choices)
                  {
                     switch (choice.Message)
                     {
                        case AssistantMessage { Content: { } text }:
                           Console.WriteLine(text);
                           break;
                        default:
                           Console.WriteLine($"(finish_reason={choice.FinishReason})");
                           break;
                     }
                  }

                  if (summariseResponse.Usage is not null)
                  {
                     Console.WriteLine();
                     Console.WriteLine($"Tokens - prompt: {summariseResponse.Usage.PromptTokens}, " +
                                       $"completion: {summariseResponse.Usage.CompletionTokens}, " +
                                       $"total: {summariseResponse.Usage.TotalTokens}");
                  }
               }
               catch (JsonException ex)
               {
                  Console.WriteLine($"Failed to parse response: {ex.Message}");
               }
            }
            break;
         default:
            Console.WriteLine("Unknown operation. Defaulting to full workflow (upload, list, url).");
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
   Console.Write("Enter command (upload, list, url, delete, ocr) or path to file to upload (Enter to exit): ");
   operation = Console.ReadLine()?.Trim().Trim('"');
}
