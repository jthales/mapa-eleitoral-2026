using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Election.Infrastructure.Tse;

public sealed class TseHttpClient(IHttpClientFactory factory, IOptions<ElectionOptions> options, ILogger<TseHttpClient> logger)
{
    public Task<string> GetAsync(Uri uri, CancellationToken cancellationToken) =>
        DownloadAsync(uri, (content, ct) => content.ReadAsStringAsync(ct), cancellationToken);

    public Task<byte[]> GetPhotoAsync(Uri uri, CancellationToken cancellationToken) =>
        DownloadAsync(uri, async (content, ct) =>
        {
            if (content.Headers.ContentType?.MediaType != "image/jpeg" || content.Headers.ContentLength > 2_000_000)
                throw new InvalidDataException("Foto não é JPEG ou excede limite.");
            var bytes = await content.ReadAsByteArrayAsync(ct);
            if (bytes.Length is < 4 or > 2_000_000 || bytes[0] != 0xff || bytes[1] != 0xd8 || bytes[2] != 0xff)
                throw new InvalidDataException("Conteúdo JPEG inválido.");
            return bytes;
        }, cancellationToken);

    private async Task<T> DownloadAsync<T>(Uri uri, Func<HttpContent, CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        using var client = factory.CreateClient("Tse");
        if (uri.AbsolutePath.EndsWith(".jpeg", StringComparison.Ordinal)) client.MaxResponseContentBufferSize = 2_000_000;
        for (var attempt = 0; ; attempt++)
        {
            await Task.Delay(options.Value.RequestSpacingMilliseconds, cancellationToken);
            try
            {
                using var response = await client.GetAsync(uri, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await read(response.Content, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < options.Value.RetryCount &&
                (ex.StatusCode is null || (int)ex.StatusCode >= 500))
            {
                logger.LogWarning("Falha transitória TSE ({Status}), tentativa {Attempt}", ex.StatusCode, attempt + 1);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < options.Value.RetryCount)
            {
                logger.LogWarning("Timeout TSE, tentativa {Attempt}", attempt + 1);
            }
            // 403/404/429 are not retried here; the worker waits its failure backoff.
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
        }
    }
}
