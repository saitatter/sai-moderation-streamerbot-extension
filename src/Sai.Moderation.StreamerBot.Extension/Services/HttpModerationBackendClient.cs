using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class HttpModerationBackendClient(
    HttpClient httpClient,
    ModerationBackendClientOptions options) : IModerationBackendClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ModerationResult> ModerateAsync(
        ModerationRequest request,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(100, options.RequestTimeoutMs));

            try
            {
                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    BuildUri(options.BaseUrl, options.ModeratePath))
                {
                    Content = JsonContent.Create(request, options: JsonOptions)
                };

                using var response = await httpClient.SendAsync(httpRequest, timeoutCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var dto = await response.Content.ReadFromJsonAsync<ModerationResponseDto>(
                        JsonOptions,
                        timeoutCts.Token);

                    if (dto is null)
                    {
                        throw new InvalidOperationException("Moderation response body was empty.");
                    }

                    return MapResult(request.MessageId, dto);
                }

                var statusCode = (int)response.StatusCode;
                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                var message = $"Moderation backend returned HTTP {statusCode}: {body}";

                if (IsRetryableStatusCode(response.StatusCode) && attempt < maxAttempts)
                {
                    await DelayBeforeRetryAsync(cancellationToken);
                    continue;
                }

                throw new HttpRequestException(message, null, response.StatusCode);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException(
                    $"Moderation request timed out after {options.RequestTimeoutMs}ms.",
                    ex);

                if (attempt < maxAttempts)
                {
                    await DelayBeforeRetryAsync(cancellationToken);
                    continue;
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null)
            {
                lastException = ex;

                if (attempt < maxAttempts)
                {
                    await DelayBeforeRetryAsync(cancellationToken);
                    continue;
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Moderation backend returned invalid JSON.", ex);
            }
        }

        throw lastException ?? new InvalidOperationException("Moderation backend request failed.");
    }

    private async Task DelayBeforeRetryAsync(CancellationToken cancellationToken)
    {
        var delay = Math.Max(0, options.RetryDelayMs);
        if (delay == 0) return;
        await Task.Delay(delay, cancellationToken);
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
    }

    private static Uri BuildUri(string baseUrl, string path)
    {
        var baseUri = baseUrl.EndsWith('/')
            ? new Uri(baseUrl)
            : new Uri($"{baseUrl}/");
        var relativePath = path.TrimStart('/');
        return new Uri(baseUri, relativePath);
    }

    private static ModerationResult MapResult(string messageId, ModerationResponseDto dto)
    {
        return new ModerationResult(
            messageId,
            ParseVerdict(dto.Verdict),
            Math.Clamp(dto.Confidence, 0, 1),
            dto.Category ?? "unknown",
            dto.Reason ?? string.Empty,
            Math.Max(0, dto.LatencyMs));
    }

    private static ModerationVerdict ParseVerdict(string? verdict)
    {
        return verdict?.Trim().ToLowerInvariant() switch
        {
            "allow" => ModerationVerdict.Allow,
            "flag" => ModerationVerdict.Flag,
            "block" => ModerationVerdict.Block,
            _ => throw new InvalidOperationException($"Unknown moderation verdict: {verdict ?? "<null>"}")
        };
    }

    private sealed record ModerationResponseDto(
        string Verdict,
        double Confidence,
        string Category,
        string Reason,
        int LatencyMs);
}
