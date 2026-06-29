// Copyright(c) June 2026, devMobile Software

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using MistralAI.Chat;
using MistralAI.Chat.Streaming;
using MistralAI.Chat.Sync;

namespace ChatCompletionClient;

public interface IMistralChatClient
{
   /// <summary>
   /// Synchronous wrapper around <see cref="CompleteAsync"/>.
   /// Safe to use on console / generic host apps with no synchronization context.
   /// Avoid calling from UI (WinForms/WPF) or ASP.NET (classic) contexts: the
   /// underlying <c>GetAwaiter().GetResult()</c> can deadlock there.
   /// </summary>
   ChatCompletionResponse Complete(ChatCompletionRequest request);

   Task<ChatCompletionResponse> CompleteAsync(
       ChatCompletionRequest request,
       CancellationToken cancellationToken = default);

   IAsyncEnumerable<ChatCompletionChunkEvent> StreamAsync(
       ChatCompletionRequest request,
       CancellationToken cancellationToken = default);
}

public sealed class MistralChatClient : IMistralChatClient
{
   public const string HttpClientName = "MistralChat";

   private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   private readonly IHttpClientFactory _httpClientFactory;

   public MistralChatClient(IHttpClientFactory httpClientFactory)
   {
      _httpClientFactory = httpClientFactory;
   }

   public ChatCompletionResponse Complete(ChatCompletionRequest request)
       => Task.Run(() => CompleteAsync(request)).GetAwaiter().GetResult();

   public async Task<ChatCompletionResponse> CompleteAsync(
       ChatCompletionRequest request,
       CancellationToken cancellationToken = default)
   {
      // HttpClient from IHttpClientFactory is pooled — do NOT dispose it.
      var http = _httpClientFactory.CreateClient(HttpClientName);

      using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
      {
         Content = BuildJsonContent(request, stream: false)
      };
      requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

      using var response = await http.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
      await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

      var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      return JsonSerializer.Deserialize<ChatCompletionResponse>(body, _jsonOptions)
         ?? throw new MistralApiException(response.StatusCode, body);
   }

   public async IAsyncEnumerable<ChatCompletionChunkEvent> StreamAsync(
       ChatCompletionRequest request,
       [EnumeratorCancellation] CancellationToken cancellationToken = default)
   {
      // HttpClient from IHttpClientFactory is pooled — do NOT dispose it.
      var http = _httpClientFactory.CreateClient(HttpClientName);

      using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
      {
         Content = BuildJsonContent(request, stream: true)
      };
      requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

      using var response = await http.SendAsync(
          requestMessage,
          HttpCompletionOption.ResponseHeadersRead,
          cancellationToken).ConfigureAwait(false);

      await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

      using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);

      // SSE inactivity guard — abort the read if Mistral stops sending lines.
      // CancelAfter on a CTS resets the timer on each call (as long as the CTS
      // hasn't already cancelled), so we reuse one CTS for the whole stream.
      var inactivity = TimeSpan.FromMinutes(2);
      using var inactivityCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

      var dataBuilder = new StringBuilder();

      while (true)
      {
         inactivityCts.CancelAfter(inactivity);

         string? line;
         try { line = await reader.ReadLineAsync(inactivityCts.Token).ConfigureAwait(false); }
         catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
         { throw new TimeoutException(
            $"SSE stream inactive for {inactivity.TotalSeconds:0} s — aborting."); }
         inactivityCts.CancelAfter(Timeout.InfiniteTimeSpan);   // pause until next read

         if (line is null) break; // end of stream

         if (line.Length == 0)
         {
            if (dataBuilder.Length == 0) continue;

            var data = dataBuilder.ToString();
            dataBuilder.Clear();

            if (data == "[DONE]") yield break;

            ChatCompletionChunkEvent? chunk;
            try
            {
               chunk = JsonSerializer.Deserialize<ChatCompletionChunkEvent>(data, _jsonOptions);
            }
            catch (JsonException)
            {
               // TODO: log; skip the bad chunk rather than abort the whole stream.
               continue;
            }
            if (chunk is not null) yield return chunk;
            continue;
         }

         if (line[0] == ':') continue;                                  // SSE comment / heartbeat
         if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

         var value = line.AsSpan("data:".Length);
         if (value.Length > 0 && value[0] == ' ') value = value[1..];

         if (dataBuilder.Length > 0) dataBuilder.Append('\n');
         dataBuilder.Append(value);
      }

      // Flush any trailing buffered event not terminated by a blank line.
      if (dataBuilder.Length > 0)
      {
         var data = dataBuilder.ToString();
         if (data != "[DONE]")
         {
            var chunk = JsonSerializer.Deserialize<ChatCompletionChunkEvent>(data, _jsonOptions);
            if (chunk is not null) yield return chunk;
         }
      }
   }

   // Serialise the request to a JsonObject so we can force `stream` without
   // mutating the caller's instance (avoids subtle reuse / concurrency bugs).
   private static JsonContent BuildJsonContent(ChatCompletionRequest request, bool stream)
   {
      var effective = new ChatCompletionRequest
      {
         Model = request.Model,
         Messages = request.Messages,
         // ... copy other fields ...
         Stream = stream,
      };
      return JsonContent.Create(effective, options: _jsonOptions);
   }

   private static async Task EnsureSuccessAsync(
       HttpResponseMessage response,
       CancellationToken cancellationToken)
   {
      if (response.IsSuccessStatusCode) return;

      var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      throw new MistralApiException(response.StatusCode, body);
   }
}

public sealed class MistralApiException : Exception
{
   public HttpStatusCode StatusCode { get; }
   public string ResponseBody { get; }

   /// <summary>The "message" field from the Mistral error JSON, if it could be parsed.</summary>
   public string? ErrorMessage { get; }

   /// <summary>The "type" field from the Mistral error JSON, if it could be parsed.</summary>
   public string? ErrorType { get; }

   /// <summary>The "code" field from the Mistral error JSON, if it could be parsed.</summary>
   public string? ErrorCode { get; }

   /// <summary>The "param" field from the Mistral error JSON, if it could be parsed.</summary>
   public string? ErrorParam { get; }

   public MistralApiException(HttpStatusCode statusCode, string responseBody)
       : base($"Mistral API returned {(int)statusCode} {statusCode}: {responseBody}")
   {
      StatusCode = statusCode;
      ResponseBody = responseBody;

      TryParseErrorBody(responseBody, out var message, out var type, out var code, out var param);
      ErrorMessage = message;
      ErrorType = type;
      ErrorCode = code;
      ErrorParam = param;
   }

   private static void TryParseErrorBody(
       string body,
       out string? message,
       out string? type,
       out string? code,
       out string? param)
   {
      message = type = code = param = null;
      if (string.IsNullOrWhiteSpace(body)) return;

      try
      {
         using var doc = JsonDocument.Parse(body);
         var root = doc.RootElement;
         if (root.ValueKind != JsonValueKind.Object) return;

         if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) message = m.GetString();
         if (root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String) type = t.GetString();
         if (root.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String) code = c.GetString();
         if (root.TryGetProperty("param", out var p) && p.ValueKind == JsonValueKind.String) param = p.GetString();
      }
      catch (JsonException)
      {
         // Non-JSON error body — already captured raw in ResponseBody.
      }
   }
}

public sealed class MistralChatClientOptions
{
   public required string BaseUrl { get; init; }
   public required string ApiKey { get; init; }
   public TimeSpan? RequestTimeout { get; init; }
   public string? UserAgent { get; init; }
}

public static class MistralChatClientExtensions
{
   /// <summary>
   /// Registers IMistralChatClient and a named HttpClient pre-configured with the Mistral base URL and bearer token.
   /// </summary>
   /// <example>
   /// builder.Services.AddMistralChatClient("https://api.mistral.ai/v1/", configuration["Mistral:ApiKey"]!);
   /// </example>
   public static IHttpClientBuilder AddMistralChatClient(
       this IServiceCollection services,
       Action<MistralChatClientOptions> configure)
   {
      services.AddSingleton<IMistralChatClient, MistralChatClient>();

      return (IHttpClientBuilder)services.AddHttpClient(MistralChatClient.HttpClientName, client =>
      {
         var options = new MistralChatClientOptions();
         configure(options);

         client.BaseAddress = new Uri(options.BaseUrl);
         client.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", options.ApiKey);

         if (options.RequestTimeout is { } timeout)
            client.Timeout = timeout;

         // TODO: set User-Agent, other headers ...
      })
      .AddStandardResilienceHandler(opts =>
      {
         // Honour Retry-After on 429 + retry transient 5xx
         opts.Retry.ShouldHandle = static args =>
            ValueTask.FromResult(
               args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests }
               || (args.Outcome.Result is { } r && (int)r.StatusCode >= 500));
      });
   }
}
