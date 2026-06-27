// Copyright(c) June 2026, devMobile Software
//
// This program is horrible, it demonstrates how to call the Mistral API using HttpClient and System.Text.Json
// 
IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

var settings = configuration.GetSection(nameof(ApplicationSettings)).Get<ApplicationSettings>() ?? throw new Exception("Failed to load application settings");

Console.Write("Enter chat message: ");
var prompt = Console.ReadLine();

// Anonymous type for the request body which feels bit "hinky" but, it works and is concise. Alternatively, could define a class for request body for better type safety and maintainability.
var requestObject = new
{
   model = settings.ModelName,
   messages = new[]
   {
      new { role = "user", content = prompt }
   }
};

// Alternatively, you can use JsonObject and JsonArray for more control over the JSON structure
var requestJson = new JsonObject()
{
   ["model"] = settings.ModelName,
   ["messages"] = new JsonArray
   {
      new JsonObject
      {
         ["role"] = "user",
         ["content"] = prompt
      }
   }
};

// Create HttpClient with required headers. Note that HttpClient should ideally be reused, but for simplicity we're creating a new instance here.
HttpClient httpClient = new()
{
   DefaultRequestHeaders =
   {
      Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
      Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey)
   },
   BaseAddress = new Uri(settings.BaseUrl)
};

//using var httpResponse1 = await httpClient.PostAsJsonAsync("chat/completions", requestObject);
using var httpResponse = await httpClient.PostAsync("chat/completions", new StringContent(JsonSerializer.Serialize(requestObject), Encoding.UTF8, "application/json"));

httpResponse.EnsureSuccessStatusCode();

// Read the response as a string and parse it into a JsonDocument. This is straightforward but can be inefficient for large responses since it loads the entire response into memory as a string first.
//string responseJson = await httpResponse.Content.ReadAsStringAsync();
//using var responseDocument = JsonDocument.Parse(responseJson);

// Streaming the response directly into JsonDocument to avoid loading the entire response into memory as a string first. This is more efficient for large responses.
using var stream = await httpResponse.Content.ReadAsStreamAsync();
using var responseDocument = await JsonDocument.ParseAsync(stream);

var content = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

Console.WriteLine(content);
Console.WriteLine();

var usage = responseDocument.RootElement.GetProperty("usage");
Console.WriteLine($"Prompt tokens: {usage.GetProperty("prompt_tokens").GetInt32()}");
Console.WriteLine($"Completion tokens: {usage.GetProperty("completion_tokens").GetInt32()}");
Console.WriteLine($"Total tokens: {usage.GetProperty("total_tokens").GetInt32()}");
Console.WriteLine();

Console.WriteLine("Press <Enter> to exit...");
Console.ReadLine();
