using System.Text.Json;

namespace System.Net.Http.Json;

/// <summary>
/// Extension methods for <see cref="HttpResponseMessage"/> to detect, read, and throw on
/// <c>application/problem+json</c> responses, as defined by
/// <see href="https://www.rfc-editor.org/rfc/rfc9457">RFC 9457</see>.
/// </summary>
/// <example>
/// The following example sends a request and converts a problem details response into an exception:
/// <code>
/// using var client = new HttpClient();
/// using var response = await client.GetAsync("https://api.example.com/orders/42");
///
/// // Throws a ProblemDetailsException if the body is application/problem+json.
/// await response.ThrowIfProblemJsonAsync();
/// response.EnsureSuccessStatusCode();
/// </code>
/// </example>
public static class ProblemJsonExtensions
{
#if NET8_0_OR_GREATER
    private const string ProblemJsonMediaType = System.Net.Mime.MediaTypeNames.Application.ProblemJson;
#else
    private const string ProblemJsonMediaType = "application/problem+json";
#endif

    /// <summary>
    /// Determines whether the response body is an <c>application/problem+json</c> payload
    /// by inspecting the <c>Content-Type</c> header.
    /// </summary>
    /// <param name="response">The HTTP response message to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when the content type is <c>application/problem+json</c>; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// if (response.IsProblemJson())
    /// {
    ///     var problem = await response.ReadProblemJsonAsync();
    /// }
    /// </code>
    /// </example>
    public static bool IsProblemJson(this HttpResponseMessage response)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        var mediaType = response.Content?.Headers.ContentType?.MediaType;

        return string.Equals(mediaType, ProblemJsonMediaType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads and deserializes the response body into a <see cref="ProblemDetails"/> instance
    /// using the source-generated serializer context.
    /// </summary>
    /// <param name="response">The HTTP response message to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The deserialized <see cref="ProblemDetails"/>, or <see langword="null"/> when the response is not
    /// an <c>application/problem+json</c> payload.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var problem = await response.ReadProblemJsonAsync();
    /// if (problem is not null)
    ///     Console.WriteLine($"{problem.Title}: {problem.Detail}");
    /// </code>
    /// </example>
    public static async Task<ProblemDetails?> ReadProblemJsonAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        if (!response.IsProblemJson())
            return null;

        return await response.Content
            .ReadFromJsonAsync(ProblemDetailsSerializerContext.Default.ProblemDetails, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Reads and deserializes the response body into a <see cref="ProblemDetails"/> instance
    /// using the specified serializer options.
    /// </summary>
    /// <param name="response">The HTTP response message to read.</param>
    /// <param name="options">The serializer options to use, or <see langword="null"/> to use the default options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The deserialized <see cref="ProblemDetails"/>, or <see langword="null"/> when the response is not
    /// an <c>application/problem+json</c> payload.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    /// var problem = await response.ReadProblemJsonAsync(options);
    /// </code>
    /// </example>
    public static async Task<ProblemDetails?> ReadProblemJsonAsync(
        this HttpResponseMessage response,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        if (!response.IsProblemJson())
            return null;

        return await response.Content
            .ReadFromJsonAsync<ProblemDetails>(options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Throws a <see cref="ProblemDetailsException"/> when the response body is an
    /// <c>application/problem+json</c> payload. Does not otherwise validate the status code;
    /// callers should still call <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> as needed.
    /// </summary>
    /// <param name="response">The HTTP response message to inspect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The same <paramref name="response"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <exception cref="ProblemDetailsException">Thrown when the response is a problem details payload.</exception>
    /// <example>
    /// <code>
    /// using var response = await client.GetAsync("https://api.example.com/orders/42");
    /// await response.ThrowIfProblemJsonAsync();
    /// response.EnsureSuccessStatusCode();
    /// </code>
    /// </example>
    public static async Task<HttpResponseMessage> ThrowIfProblemJsonAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        if (!response.IsProblemJson())
            return response;

        var problemDetails = await response
                .ReadProblemJsonAsync(cancellationToken)
                .ConfigureAwait(false);

#if NET5_0_OR_GREATER
        throw new ProblemDetailsException(
            message: null,
            inner: null,
            statusCode: response.StatusCode,
            problemDetails: problemDetails);
#else
        throw new ProblemDetailsException(
            message: null,
            inner: null,
            problemDetails: problemDetails);
#endif
    }

    /// <summary>
    /// Throws a <see cref="ProblemDetailsException"/> when the response body is an
    /// <c>application/problem+json</c> payload. Does not otherwise validate the status code;
    /// callers should still call <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> as needed.
    /// </summary>
    /// <param name="response">The HTTP response message to inspect.</param>
    /// <param name="options">The serializer options to use, or <see langword="null"/> to use the default options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The same <paramref name="response"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <exception cref="ProblemDetailsException">Thrown when the response is a problem details payload.</exception>
    /// <example>
    /// <code>
    /// var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    /// using var response = await client.GetAsync("https://api.example.com/orders/42");
    /// await response.ThrowIfProblemJsonAsync(options);
    /// </code>
    /// </example>
    public static async Task<HttpResponseMessage> ThrowIfProblemJsonAsync(
        this HttpResponseMessage response,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        if (!response.IsProblemJson())
            return response;

        var problemDetails = await response
                .ReadProblemJsonAsync(options, cancellationToken)
                .ConfigureAwait(false);

#if NET5_0_OR_GREATER
        throw new ProblemDetailsException(
            message: null,
            inner: null,
            statusCode: response.StatusCode,
            problemDetails: problemDetails);
#else
        throw new ProblemDetailsException(
            message: null,
            inner: null,
            problemDetails: problemDetails);
#endif
    }
}
