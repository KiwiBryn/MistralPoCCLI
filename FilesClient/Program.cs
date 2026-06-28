// Copyright(c) June 2026, devMobile Software
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(AppContext.BaseDirectory)
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
   .AddUserSecrets<Program>()
   .Build();

builder.Services.Configure<ApplicationSettings>(configuration.GetSection(nameof(ApplicationSettings)));

builder.Services.AddHttpClient("MistralFiles", (sp, client) =>
{
   var settings = sp.GetRequiredService<IOptions<ApplicationSettings>>().Value;
   client.BaseAddress = new Uri(settings.BaseUrl);
   client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
   client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<IMistralFilesClient, MistralFilesClient>();

var host = builder.Build();
var filesClient = host.Services.GetRequiredService<IMistralFilesClient>();

Console.WriteLine("Mistral Files — commands: list | upload | retrieve | delete | download | url");
Console.WriteLine("Press Enter with no command to exit.");

while (true)
{
   Console.WriteLine();
   Console.Write("Command: ");
   var command = Console.ReadLine()?.Trim().ToLowerInvariant();

   if (string.IsNullOrEmpty(command))
      break;

   try
   {
      switch (command)
      {
         case "list":     await RunListAsync(filesClient);     break;
         case "upload":   await RunUploadAsync(filesClient);   break;
         case "retrieve": await RunRetrieveAsync(filesClient); break;
         case "delete":   await RunDeleteAsync(filesClient);   break;
         case "download": await RunDownloadAsync(filesClient); break;
         case "url":      await RunUrlAsync(filesClient);      break;
         default:
            Console.WriteLine("Unknown command. Try: list | upload | retrieve | delete | download | url");
            break;
      }
   }
   catch (HttpRequestException ex)
   {
      Console.WriteLine($"API error {(int?)ex.StatusCode}: {ex.Message}");
   }
   catch (Exception ex)
   {
      Console.WriteLine($"Error: {ex.Message}");
   }
}

// ── helpers ──────────────────────────────────────────────────────────────────

static string? Prompt(string label, string? defaultValue = null)
{
   Console.Write(defaultValue is null ? $"  {label}: " : $"  {label} [{defaultValue}]: ");
   var value = Console.ReadLine()?.Trim();
   if (string.IsNullOrEmpty(value)) return defaultValue;
   return value;
}

static string Require(string label)
{
   while (true)
   {
      Console.Write($"  {label}: ");
      var value = Console.ReadLine()?.Trim();
      if (!string.IsNullOrEmpty(value)) return value;
      Console.WriteLine($"  {label} is required.");
   }
}

// ── commands ──────────────────────────────────────────────────────────────────

static async Task RunListAsync(IMistralFilesClient client)
{
   var result = await client.ListAsync();

   if (result.Data.Count == 0)
   {
      Console.WriteLine("No files found.");
      return;
   }

   Console.WriteLine($"\n  {"ID",-36}  {"Filename",-28}  {"Bytes",10}  {"Purpose",-8}  {"Source",-10}  Created");
   Console.WriteLine($"  {new string('-', 105)}");

   foreach (var f in result.Data)
   {
      var created = DateTimeOffset.FromUnixTimeSeconds(f.CreatedAt).ToString("yyyy-MM-dd");
      Console.WriteLine($"  {f.Id,-36}  {f.Filename,-28}  {f.Bytes,10:N0}  {f.Purpose,-8}  {f.Source,-10}  {created}");
   }

   Console.WriteLine($"\n  Total: {result.Total ?? result.Data.Count}");
}

static async Task RunUploadAsync(IMistralFilesClient client)
{
   var path = Require("File path");
   if (!File.Exists(path))
   {
      Console.WriteLine("  File not found.");
      return;
   }

   Console.WriteLine($"  Purpose options: {string.Join(" | ", Enum.GetNames<FilePurpose>())}");
   FilePurpose purpose;
   while (true)
   {
      var input = Require("Purpose");
      if (Enum.TryParse(input, ignoreCase: true, out purpose)) break;
      Console.WriteLine("  Invalid purpose.");
   }

   var result = await client.UploadAsync(path, purpose);
   Console.WriteLine($"  Uploaded — ID: {result.Id}  Filename: {result.Filename}  Size: {result.Bytes:N0} bytes");
}

static async Task RunRetrieveAsync(IMistralFilesClient client)
{
   var id = Require("File ID");
   var f = await client.RetrieveAsync(id);

   Console.WriteLine($"  ID:        {f.Id}");
   Console.WriteLine($"  Filename:  {f.Filename}");
   Console.WriteLine($"  Bytes:     {f.Bytes:N0}");
   Console.WriteLine($"  Purpose:   {f.Purpose}");
   Console.WriteLine($"  Source:    {f.Source}");
   Console.WriteLine($"  MimeType:  {f.MimeType ?? "—"}");
   Console.WriteLine($"  Created:   {DateTimeOffset.FromUnixTimeSeconds(f.CreatedAt):yyyy-MM-dd HH:mm:ss zzz}");
   if (f.ExpiresAt.HasValue)
      Console.WriteLine($"  Expires:   {DateTimeOffset.FromUnixTimeSeconds(f.ExpiresAt.Value):yyyy-MM-dd HH:mm:ss zzz}");
}

static async Task RunDeleteAsync(IMistralFilesClient client)
{
   var id = Require("File ID");
   var result = await client.DeleteAsync(id);
   Console.WriteLine(result.Deleted ? $"  Deleted: {result.Id}" : $"  Not deleted: {result.Id}");
}

static async Task RunDownloadAsync(IMistralFilesClient client)
{
   var id = Require("File ID");
   var savePath = Prompt("Save path", $"{id}.bin")!;

   var bytes = await client.DownloadContentAsync(id);
   await File.WriteAllBytesAsync(savePath, bytes);
   Console.WriteLine($"  Saved {bytes.Length:N0} bytes → {Path.GetFullPath(savePath)}");
}

static async Task RunUrlAsync(IMistralFilesClient client)
{
   var id = Require("File ID");
   int? expiry = null;
   var expiryInput = Prompt("Expiry seconds (blank for API default)");
   if (expiryInput is not null && int.TryParse(expiryInput, out var parsed))
      expiry = parsed;

   var result = await client.GetSignedUrlAsync(id, expiry);
   Console.WriteLine($"  URL: {result.Url}");
}
