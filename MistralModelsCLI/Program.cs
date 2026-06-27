// Copyright(c) June 2026, devMobile Software
//
// This program is nasty, it demonstrates the Mistral Models API: list all models, retrieve
// a specific model, delete a fine-tuned model, archive/unarchive a fine-tuned
// model, and update a fine-tuned model's name or description.
//
using MistralModelsCLI;


IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new InvalidOperationException("Failed to load application settings");

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
   WriteIndented = false,
   DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
   AllowTrailingCommas = false,
   ReadCommentHandling = JsonCommentHandling.Disallow,
   UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
};

Console.Write("Enter command (list, retrieve, delete, archive, unarchive, update) (Enter to exit): ");
var operation = Console.ReadLine()?.Trim().Trim('"');

while (!string.IsNullOrWhiteSpace(operation))
{
   try
   {
      switch (operation?.ToLower())
      {
         case "list":
         {
            Console.WriteLine("Listing models...");
            var list = await ListModelsAsync(httpClient, jsonSerializerOptions);
            if (list is not null)
            {
               Console.WriteLine();
               Console.WriteLine($"Models ({list.Data.Count}):");
               foreach (var m in list.Data)
               {
                  var caps = m.Capabilities;
                  Console.WriteLine($"  {m.Id,-45}  ctx:{m.MaxContextLength,8:N0}  " +
                     $"chat:{(caps.CompletionChat ? "Y" : "N")}  " +
                     $"ft:{(caps.FineTuning ? "Y" : "N")}  " +
                     $"vision:{(caps.Vision ? "Y" : "N")}  " +
                     $"owned:{m.OwnedBy}");
               }
            }
            break;
         }
         case "retrieve":
         {
            Console.Write("Enter model ID (Enter to cancel): ");
            var modelId = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(modelId))
               break;

            var model = await RetrieveModelAsync(httpClient, modelId, jsonSerializerOptions);
            if (model is null)
            {
               Console.WriteLine("No model returned.");
               break;
            }

            Console.WriteLine();
            Console.WriteLine($"Id:               {model.Id}");
            Console.WriteLine($"Object:           {model.Object}");
            Console.WriteLine($"Created:          {DateTimeOffset.FromUnixTimeSeconds(model.Created):u}");
            Console.WriteLine($"Owned by:         {model.OwnedBy}");
            Console.WriteLine($"Max context:      {model.MaxContextLength:N0}");
            Console.WriteLine($"Archived:         {model.Archived?.ToString() ?? "N/A"}");
            Console.WriteLine($"Type:             {model.Type ?? "base"}");
            Console.WriteLine($"Root:             {model.Root ?? "N/A"}");
            if (model.Aliases is { Count: > 0 })
               Console.WriteLine($"Aliases:          {string.Join(", ", model.Aliases)}");
            var c = model.Capabilities;
            Console.WriteLine($"Capabilities:     chat:{(c.CompletionChat ? "Y" : "N")}  fim:{(c.CompletionFim ? "Y" : "N")}  " +
               $"tools:{(c.FunctionCalling ? "Y" : "N")}  ft:{(c.FineTuning ? "Y" : "N")}  " +
               $"vision:{(c.Vision ? "Y" : "N")}  classify:{(c.Classification ? "Y" : "N")}");
            break;
         }
         case "delete":
         {
            Console.Write("Enter fine-tuned model ID to delete (Enter to cancel): ");
            var modelId = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(modelId))
               break;

            var result = await DeleteModelAsync(httpClient, modelId, jsonSerializerOptions);
            if (result is null)
            {
               Console.WriteLine("No response returned.");
               break;
            }

            Console.WriteLine();
            Console.WriteLine($"Id:      {result.Id}");
            Console.WriteLine($"Deleted: {result.Deleted}");
            break;
         }
         case "archive":
         {
            Console.Write("Enter fine-tuned model ID to archive (Enter to cancel): ");
            var modelId = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(modelId))
               break;

            var result = await ArchiveModelAsync(httpClient, modelId, jsonSerializerOptions);
            if (result is null)
            {
               Console.WriteLine("No response returned.");
               break;
            }

            Console.WriteLine();
            Console.WriteLine($"Id:       {result.Id}");
            Console.WriteLine($"Archived: {result.Archived}");
            break;
         }
         case "unarchive":
         {
            Console.Write("Enter fine-tuned model ID to unarchive (Enter to cancel): ");
            var modelId = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(modelId))
               break;

            var result = await UnarchiveModelAsync(httpClient, modelId, jsonSerializerOptions);
            if (result is null)
            {
               Console.WriteLine("No response returned.");
               break;
            }

            Console.WriteLine();
            Console.WriteLine($"Id:       {result.Id}");
            Console.WriteLine($"Archived: {result.Archived}");
            break;
         }
         case "update":
         {
            Console.Write("Enter fine-tuned model ID to update (Enter to cancel): ");
            var modelId = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(modelId))
               break;

            Console.Write("New name (Enter to leave unchanged): ");
            var name = Console.ReadLine()?.Trim().Trim('"');

            Console.Write("New description (Enter to leave unchanged): ");
            var description = Console.ReadLine()?.Trim().Trim('"');

            var request = new UpdateFineTunedModelRequest
            {
               Name = string.IsNullOrWhiteSpace(name) ? null : name,
               Description = string.IsNullOrWhiteSpace(description) ? null : description,
            };

            var result = await UpdateModelAsync(httpClient, modelId, request, jsonSerializerOptions);
            if (result is null)
            {
               Console.WriteLine("No response returned.");
               break;
            }

            Console.WriteLine();
            Console.WriteLine($"Id:          {result.Id}");
            Console.WriteLine($"Owned by:    {result.OwnedBy ?? "N/A"}");
            Console.WriteLine($"Root:        {result.Root ?? "N/A"}");
            Console.WriteLine($"Archived:    {result.Archived?.ToString() ?? "N/A"}");
            Console.WriteLine($"WorkspaceId: {result.WorkspaceId ?? "N/A"}");
            break;
         }
         default:
            Console.WriteLine($"Unknown command '{operation}'. Valid commands: list, retrieve, delete, archive, unarchive, update.");
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

   Console.WriteLine();
   Console.Write("Enter command (list, retrieve, delete, archive, unarchive, update) (Enter to exit): ");
   operation = Console.ReadLine()?.Trim().Trim('"');
}


// ---- Local helpers ----

static async Task<ModelListResponse?> ListModelsAsync(
   HttpClient http,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.GetAsync("models", ct);
   response.EnsureSuccessStatusCode();
   return await response.Content.ReadFromJsonAsync<ModelListResponse>(options, ct);
}

static async Task<ModelCard?> RetrieveModelAsync(
   HttpClient http,
   string modelId,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.GetAsync($"models/{modelId}", ct);
   response.EnsureSuccessStatusCode();
   return await response.Content.ReadFromJsonAsync<ModelCard>(options, ct);
}

static async Task<ModelDeleteResponse?> DeleteModelAsync(
   HttpClient http,
   string modelId,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.DeleteAsync($"models/{modelId}", ct);
   response.EnsureSuccessStatusCode();
   return await response.Content.ReadFromJsonAsync<ModelDeleteResponse>(options, ct);
}

static async Task<ModelArchiveResponse?> ArchiveModelAsync(
   HttpClient http,
   string modelId,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.PostAsync($"fine_tuning/models/{modelId}/archive", null, ct);
   response.EnsureSuccessStatusCode();
   return await response.Content.ReadFromJsonAsync<ModelArchiveResponse>(options, ct);
}

static async Task<ModelArchiveResponse?> UnarchiveModelAsync(
   HttpClient http,
   string modelId,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.DeleteAsync($"fine_tuning/models/{modelId}/archive", ct);
   response.EnsureSuccessStatusCode();
   return await response.Content.ReadFromJsonAsync<ModelArchiveResponse>(options, ct);
}

static async Task<FineTunedModel?> UpdateModelAsync(
   HttpClient http,
   string modelId,
   UpdateFineTunedModelRequest request,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"fine_tuning/models/{modelId}")
   {
      Content = JsonContent.Create(request, options: options),
   };
   using var response = await http.SendAsync(httpRequest, ct);
   response.EnsureSuccessStatusCode();
   return await response.Content.ReadFromJsonAsync<FineTunedModel>(options, ct);
}
