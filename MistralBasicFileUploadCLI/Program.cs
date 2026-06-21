// Copyright(c) June 2026, devMobile Software
//
// This program demonstrates the Mistral Files API: multipart upload, list,
// and signed-URL retrieval, all using strongly typed request/response models.
//
using MistralBasicFileUploadCLI.Models;


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

Console.Write("Enter path to file to upload (Enter to exit): ");
var filePath = Console.ReadLine()?.Trim().Trim('"');


while (!string.IsNullOrWhiteSpace(filePath))
{
   if (!File.Exists(filePath))
   {
      Console.WriteLine($"File not found: {filePath}");
      Console.WriteLine();
      Console.Write("Enter path to file to upload (Enter to exit): ");
      filePath = Console.ReadLine()?.Trim().Trim('"');
      continue;
   }

   try
   {
      // ----- 1. Upload (multipart/form-data) -----
      await using var fileStream = File.OpenRead(filePath);
      var uploadRequest = new FileUploadRequest
      {
         Content  = fileStream,
         FileName = Path.GetFileName(filePath),
         Purpose  = FilePurpose.Ocr,   // change to FineTune / Batch as needed
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

      // ----- 2. List all files -----
      using var listResponse = await httpClient.GetAsync("files");
      listResponse.EnsureSuccessStatusCode();

      var list = await listResponse.Content.ReadFromJsonAsync<FileListResponse>(jsonSerializerOptions);
      if (list is not null)
      {
         Console.WriteLine();
         Console.WriteLine($"Files on account ({list.Total ?? list.Data.Count}):");
         foreach (var f in list.Data)
         {
            Console.WriteLine($"  {f.Id}  {f.SizeBytes,12:N0}  {f.Purpose,-10}  {f.Filename}");
         }
      }

      // ----- 3. Get a signed download URL for the file we just uploaded -----
      using var urlResponse = await httpClient.GetAsync($"files/{uploaded.Id}/url");
      urlResponse.EnsureSuccessStatusCode();

      var signed = await urlResponse.Content.ReadFromJsonAsync<FileSignedUrlResponse>(jsonSerializerOptions);
      if (signed is not null)
      {
         Console.WriteLine();
         Console.WriteLine($"Signed URL: {signed.Url}");
         if (signed.ExpiresAt is long exp)
         {
            Console.WriteLine($"  Expires:  {DateTimeOffset.FromUnixTimeSeconds(exp):u}");
         }
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
   Console.Write("Enter path to file to upload (Enter to exit): ");
   filePath = Console.ReadLine()?.Trim().Trim('"');
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
