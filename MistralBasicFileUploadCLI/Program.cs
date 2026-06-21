// Copyright(c) June 2026, devMobile Software
//
// This program demonstrates the Mistral Files API: multipart upload, list,
// signed-URL retrieval, and delete, all using strongly typed request/response models.
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

Console.Write("Enter command (upload, list, url, delete, ocr) or path to file to upload (Enter to exit): ");
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
            {
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
            }
            break;
         case "url":
            {
               Console.WriteLine("Enter file ID to retrieve signed URL (Enter to exit): ");
               var fileId = Console.ReadLine()?.Trim().Trim('"');
               if (string.IsNullOrWhiteSpace(fileId))
               {
                  break;
               }

               var signed = await GetSignedUrlAsync(httpClient, fileId, jsonSerializerOptions);
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
            break;
         case "delete":
            {
               Console.WriteLine("Enter file ID to delete (Enter to exit): ");
               var fileId = Console.ReadLine()?.Trim().Trim('"');
               if (string.IsNullOrWhiteSpace(fileId))
               {
                  break;
               }

               var deleted = await DeleteFileAsync(httpClient, fileId, jsonSerializerOptions);
               if (deleted is not null)
               {
                  Console.WriteLine();
                  Console.WriteLine($"File {deleted.Id} deleted: {deleted.Deleted}");
               }
            }
            break;
         case "ocr":
            {
               Console.Write("Enter file ID to OCR (Enter to cancel): ");
               var fileId = Console.ReadLine()?.Trim().Trim('"');
               if (string.IsNullOrWhiteSpace(fileId))
               {
                  break;
               }

               Console.WriteLine($"Running OCR on {fileId} with model '{settings.OcrModelName}'...");
               var ocr = await OcrFileAsync(httpClient, fileId, settings.OcrModelName, jsonSerializerOptions);
               if (ocr is not null)
               {
                  Console.WriteLine();
                  Console.WriteLine($"OCR complete - model: {ocr.Model}, pages: {ocr.Pages.Count}");
                  if (ocr.UsageInfo is not null)
                  {
                     Console.WriteLine($"  Doc size: {ocr.UsageInfo.DocSizeBytes:N0} bytes, processed: {ocr.UsageInfo.PagesProcessed} page(s)");
                  }

                  foreach (var page in ocr.Pages)
                  {
                     Console.WriteLine();
                     Console.WriteLine($"---- Page {page.Index} ----");
                     Console.WriteLine(page.Markdown);
                  }
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

// ---- Local helper: GET /v1/files ----
static async Task<FileListResponse?> ListFilesAsync(
   HttpClient http,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.GetAsync("files", ct);
   response.EnsureSuccessStatusCode();

   return await response.Content.ReadFromJsonAsync<FileListResponse>(options, ct);
}

// ---- Local helper: GET /v1/files/{file_id}/url ----
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

// ---- Local helper: DELETE /v1/files/{file_id} ----
static async Task<FileDeleteResponse?> DeleteFileAsync(
   HttpClient http,
   string fileId,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   using var response = await http.DeleteAsync($"files/{fileId}", ct);
   response.EnsureSuccessStatusCode();

   return await response.Content.ReadFromJsonAsync<FileDeleteResponse>(options, ct);
}

// ---- Local helper: POST /v1/ocr ----
static async Task<OcrResponse?> OcrFileAsync(
   HttpClient http,
   string fileId,
   string ocrModel,
   JsonSerializerOptions options,
   CancellationToken ct = default)
{
   var signed = await GetSignedUrlAsync(http, fileId, options, ct);
   if (signed is null)
   {
      return null;
   }

   var request = new OcrRequest
   {
      Model = ocrModel,
      Document = OcrDocument.FromUrl(signed.Url),
      // IncludeImageBase64 = true,   // uncomment if you want embedded image bytes back
   };

   using var response = await http.PostAsJsonAsync("ocr", request, options, ct);
   response.EnsureSuccessStatusCode();

   return await response.Content.ReadFromJsonAsync<OcrResponse>(options, ct);
}
