// Copyright(c) June 2026, devMobile Software
//
// This program is horrible, it demonstrates how to call the Mistral API using HttpClient and strongly typed request & response messages
// 
using MistralBasicTypedCLI.Models;


IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new InvalidOperationException("Failed to load application settings");

// Create HttpClient with required headers.
using HttpClient httpClient = new()
{
   DefaultRequestHeaders =
   {
      Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
      Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey)
   },
   BaseAddress = new Uri(settings.BaseUrl)
};

var jsonSerializerOptions = new JsonSerializerOptions()
{
   // PropertyNamingPolicy removed - [JsonPropertyName] attributes on model handle wire names
   WriteIndented           = false,
   DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull,
   AllowTrailingCommas     = false,
   ReadCommentHandling     = JsonCommentHandling.Disallow,
   UnmappedMemberHandling  = JsonUnmappedMemberHandling.Skip,
};

Console.Write("Enter chat message: ");
var content = Console.ReadLine();

while (!string.IsNullOrWhiteSpace(content))
{
   var request = new ChatCompletionRequest
   {
      Model = settings.ModelName,
      Messages =
      [
         new ChatMessage { Role = "user", Content = content }
      ],
   };

   try
   {
      string Text = JsonSerializer.Serialize(request, jsonSerializerOptions);

      using var httpResponse = await httpClient.PostAsJsonAsync("chat/completions", request, jsonSerializerOptions);
      httpResponse.EnsureSuccessStatusCode();

      ChatCompletionResponse? chatCompletionResponse = await httpResponse.Content.ReadFromJsonAsync<ChatCompletionResponse>(jsonSerializerOptions);

      if (chatCompletionResponse != null)
      {
         foreach (var choice in chatCompletionResponse.Choices)
         {
            Console.WriteLine(choice.Message.Content);
         }

         Console.WriteLine();
         if (chatCompletionResponse.Usage != null)
         {
            Console.WriteLine($"Prompt tokens: {chatCompletionResponse.Usage.PromptTokens}");
            Console.WriteLine($"Completion tokens: {chatCompletionResponse.Usage.CompletionTokens}");
            Console.WriteLine($"Total tokens: {chatCompletionResponse.Usage.TotalTokens}");
         }
         Console.WriteLine();
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

   Console.Write("Enter chat message: ");
   content = Console.ReadLine();
}
