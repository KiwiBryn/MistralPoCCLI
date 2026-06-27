// Copyright(c) June 2026, devMobile Software

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace ChatCompletionClient;

public interface IMistralChatClient
{
    ChatCompletionResponse Complete(ChatCompletionRequest request);

    Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatCompletionChunk> StreamAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class MistralChatClient : IMistralChatClient
{
    public const string HttpClientName = "MistralChat";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;

    public MistralChatClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ChatCompletionResponse Complete(ChatCompletionRequest request)
        => CompleteAsync(request).GetAwaiter().GetResult();

    public async Task<ChatCompletionResponse> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Stream = false;

        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await http.PostAsync("chat/completions", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ChatCompletionResponse>(body, _jsonOptions)!;
    }

    public async IAsyncEnumerable<ChatCompletionChunk> StreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        request.Stream = true;

        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json")
        };
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await http.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;  // end of stream
            if (!line.StartsWith("data:")) continue;

            var data = line["data:".Length..].TrimStart();
            if (data == "[DONE]") break;
            if (string.IsNullOrEmpty(data)) continue;

            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, _jsonOptions);
            if (chunk is not null)
                yield return chunk;
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new MistralApiException(response.StatusCode, body);
    }
}

public sealed class MistralApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public MistralApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Mistral API returned {(int)statusCode} {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public static class MistralChatClientExtensions
{
    /// <summary>
    /// Registers IMistralChatClient and a named HttpClient pre-configured with the Mistral base URL and bearer token.
    /// </summary>
    /// <example>
    /// builder.Services.AddMistralChatClient("https://api.mistral.ai", configuration["Mistral:ApiKey"]!);
    /// </example>
    public static IHttpClientBuilder AddMistralChatClient(
        this IServiceCollection services,
        string baseUrl,
        string apiKey)
    {
        services.AddSingleton<IMistralChatClient, MistralChatClient>();

        return services.AddHttpClient(MistralChatClient.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        });
    }
}
