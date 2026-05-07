using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class HttpModerationBackendClient(
    HttpClient httpClient,
    HttpModerationBackendClientOptions options,
    ILogger<HttpModerationBackendClient> logger) : IModerationBackendClient
{
    public async Task<ModerationResult> ModerateAsync(
        ModerationRequest request,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildUri(options.BaseUrl, options.ModeratePath);
        var maxAttempts = Math.Max(1, options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCts.CancelAfter(Math.Max(100, options.TimeoutMs));

                using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(request)
                };
                if (!string.IsNullOrWhiteSpace(options.ApiToken))
                {
                    message.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", options.ApiToken);
                }

                using var response = await httpClient.SendAsync(message, linkedCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode >= 500 && attempt < maxAttempts)
                    {
                        await DelayBeforeRetryAsync(cancellationToken);
                        continue;
                    }

                    throw new HttpRequestException(
                        $"Moderation backend responded with {(int)response.StatusCode}",
                        null,
                        response.StatusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<ModerationResult>(
                    cancellationToken: cancellationToken);
                if (result is null)
                {
                    throw new InvalidOperationException("Moderation backend returned an empty response.");
                }

                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null && attempt < maxAttempts)
            {
                lastException = ex;
                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                lastException = ex;
                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to moderate message {MessageId}.", request.MessageId);
                throw;
            }
        }

        throw lastException ?? new InvalidOperationException("Moderation request failed.");
    }

    private static Uri BuildUri(string baseUrl, string path)
    {
        var baseUri = baseUrl.EndsWith('/') ? new Uri(baseUrl) : new Uri(baseUrl + "/");
        var relativePath = path.TrimStart('/');
        return new Uri(baseUri, relativePath);
    }

    private async Task DelayBeforeRetryAsync(CancellationToken cancellationToken)
    {
        var retryDelayMs = Math.Max(0, options.RetryDelayMs);
        if (retryDelayMs > 0)
        {
            await Task.Delay(retryDelayMs, cancellationToken);
        }
    }
}
