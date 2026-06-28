// Copyright(c) June 2026, devMobile Software

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace ModelsClient;

public interface IMistralModelsClient
{
    Task<ModelList> ListAsync(string? provider = null, string? model = null, CancellationToken cancellationToken = default);
    Task<BaseModelCard> RetrieveAsync(string modelId, CancellationToken cancellationToken = default);
    Task<DeleteModelResponse> DeleteAsync(string modelId, CancellationToken cancellationToken = default);
    Task<FinetunedModelCard> UpdateAsync(string modelId, UpdateModelRequest request, CancellationToken cancellationToken = default);
    Task<ArchiveModelResponse> ArchiveAsync(string modelId, CancellationToken cancellationToken = default);
    Task<ArchiveModelResponse> UnarchiveAsync(string modelId, CancellationToken cancellationToken = default);
}

public sealed class MistralModelsClient : IMistralModelsClient
{
    public const string HttpClientName = "MistralModels";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;

    public MistralModelsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ModelList> ListAsync(
        string? provider = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);

        var query = new List<string>();
        if (provider is not null) query.Add($"provider={Uri.EscapeDataString(provider)}");
        if (model is not null) query.Add($"model={Uri.EscapeDataString(model)}");
        var url = query.Count > 0 ? $"models?{string.Join("&", query)}" : "models";

        using var response = await http.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<ModelList>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from list endpoint.");
    }

    public async Task<BaseModelCard> RetrieveAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.GetAsync($"models/{Uri.EscapeDataString(modelId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<BaseModelCard>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from retrieve endpoint.");
    }

    public async Task<DeleteModelResponse> DeleteAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.DeleteAsync($"models/{Uri.EscapeDataString(modelId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<DeleteModelResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from delete endpoint.");
    }

    public async Task<FinetunedModelCard> UpdateAsync(
        string modelId,
        UpdateModelRequest request,
        CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.PatchAsJsonAsync(
            $"fine_tuning/models/{Uri.EscapeDataString(modelId)}",
            request,
            _jsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<FinetunedModelCard>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from update endpoint.");
    }

    public async Task<ArchiveModelResponse> ArchiveAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.PostAsync(
            $"models/{Uri.EscapeDataString(modelId)}/archive",
            content: null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<ArchiveModelResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from archive endpoint.");
    }

    public async Task<ArchiveModelResponse> UnarchiveAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.PostAsync(
            $"models/{Uri.EscapeDataString(modelId)}/unarchive",
            content: null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<ArchiveModelResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from unarchive endpoint.");
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

public static class MistralModelsClientExtensions
{
    /// <summary>
    /// Registers IMistralModelsClient and a named HttpClient pre-configured with the Mistral base URL and bearer token.
    /// </summary>
    /// <example>
    /// builder.Services.AddMistralModelsClient("https://api.mistral.ai/v1/", configuration["Mistral:ApiKey"]!);
    /// </example>
    public static IHttpClientBuilder AddMistralModelsClient(
        this IServiceCollection services,
        string baseUrl,
        string apiKey)
    {
        services.AddSingleton<IMistralModelsClient, MistralModelsClient>();

        return services.AddHttpClient(MistralModelsClient.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        });
    }
}
