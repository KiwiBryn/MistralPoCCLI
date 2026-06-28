// Copyright(c) June 2026, devMobile Software
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

public interface IMistralFilesClient
{
   Task<FileObject> UploadAsync(string filePath, FilePurpose purpose, CancellationToken cancellationToken = default);
   Task<FileListResponse> ListAsync(FilePurpose? purpose = null, FileSource? source = null, FileSampleType? sampleType = null, int? page = null, int? pageSize = null, string? search = null, CancellationToken cancellationToken = default);
   Task<FileObject> RetrieveAsync(string fileId, CancellationToken cancellationToken = default);
   Task<FileDeleteResponse> DeleteAsync(string fileId, CancellationToken cancellationToken = default);
   Task<byte[]> DownloadContentAsync(string fileId, CancellationToken cancellationToken = default);
   Task<FileSignedUrlResponse> GetSignedUrlAsync(string fileId, int? expiry = null, CancellationToken cancellationToken = default);
}

public class MistralFilesClient(IHttpClientFactory httpClientFactory) : IMistralFilesClient
{
   private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

   private HttpClient CreateClient() => httpClientFactory.CreateClient("MistralFiles");

   // Serializes an enum to its JSON string value (e.g. FilePurpose.FineTune -> "fine-tune")
   private static string EnumToString<T>(T value) where T : struct, Enum
      => JsonSerializer.Serialize(value, _jsonOptions).Trim('"');

   public async Task<FileObject> UploadAsync(string filePath, FilePurpose purpose, CancellationToken cancellationToken = default)
   {
      using var client = CreateClient();
      await using var fileStream = File.OpenRead(filePath);

      using var form = new MultipartFormDataContent();
      using var streamContent = new StreamContent(fileStream);
      form.Add(streamContent, "file", Path.GetFileName(filePath));
      form.Add(new StringContent(EnumToString(purpose)), "purpose");

      var response = await client.PostAsync("files", form, cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<FileObject>(_jsonOptions, cancellationToken)
         ?? throw new InvalidOperationException("Empty response from upload endpoint.");
   }

   public async Task<FileListResponse> ListAsync(FilePurpose? purpose = null, FileSource? source = null, FileSampleType? sampleType = null, int? page = null, int? pageSize = null, string? search = null, CancellationToken cancellationToken = default)
   {
      using var client = CreateClient();

      var query = new List<string>();
      if (purpose.HasValue) query.Add($"purpose={Uri.EscapeDataString(EnumToString(purpose.Value))}");
      if (source.HasValue) query.Add($"source={Uri.EscapeDataString(EnumToString(source.Value))}");
      if (sampleType.HasValue) query.Add($"sample_type={Uri.EscapeDataString(EnumToString(sampleType.Value))}");
      if (page.HasValue) query.Add($"page={page.Value}");
      if (pageSize.HasValue) query.Add($"page_size={pageSize.Value}");
      if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");

      var url = query.Count > 0 ? $"files?{string.Join("&", query)}" : "files";

      var response = await client.GetAsync(url, cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<FileListResponse>(_jsonOptions, cancellationToken)
         ?? throw new InvalidOperationException("Empty response from list endpoint.");
   }

   public async Task<FileObject> RetrieveAsync(string fileId, CancellationToken cancellationToken = default)
   {
      using var client = CreateClient();
      var response = await client.GetAsync($"files/{fileId}", cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<FileObject>(_jsonOptions, cancellationToken)
         ?? throw new InvalidOperationException("Empty response from retrieve endpoint.");
   }

   public async Task<FileDeleteResponse> DeleteAsync(string fileId, CancellationToken cancellationToken = default)
   {
      using var client = CreateClient();
      var response = await client.DeleteAsync($"files/{fileId}", cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<FileDeleteResponse>(_jsonOptions, cancellationToken)
         ?? throw new InvalidOperationException("Empty response from delete endpoint.");
   }

   public async Task<byte[]> DownloadContentAsync(string fileId, CancellationToken cancellationToken = default)
   {
      using var client = CreateClient();
      var response = await client.GetAsync($"files/{fileId}/content", cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsByteArrayAsync(cancellationToken);
   }

   public async Task<FileSignedUrlResponse> GetSignedUrlAsync(string fileId, int? expiry = null, CancellationToken cancellationToken = default)
   {
      using var client = CreateClient();
      var url = expiry.HasValue ? $"files/{fileId}/url?expiry={expiry.Value}" : $"files/{fileId}/url";
      var response = await client.GetAsync(url, cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadFromJsonAsync<FileSignedUrlResponse>(_jsonOptions, cancellationToken)
         ?? throw new InvalidOperationException("Empty response from signed URL endpoint.");
   }
}
