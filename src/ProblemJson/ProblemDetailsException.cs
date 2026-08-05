using System.Text;

namespace System.Net.Http.Json;

/// <summary>
/// Represents an error that occurs when an HTTP response contains an
/// <c>application/problem+json</c> body, as defined by
/// <see href="https://www.rfc-editor.org/rfc/rfc9457">RFC 9457</see>.
/// </summary>
/// <remarks>
/// The exception message is composed from the supplied <see cref="ProblemDetails"/> when available,
/// combining the HTTP status code with the problem's <see cref="System.Net.Http.Json.ProblemDetails.Title"/>,
/// <see cref="System.Net.Http.Json.ProblemDetails.Detail"/>, <see cref="System.Net.Http.Json.ProblemDetails.Instance"/>,
/// and <see cref="System.Net.Http.Json.ProblemDetails.Type"/>. The explicitly provided message is used when the
/// problem has neither a title nor a detail, for example: <c>404 NotFound: Order not found. No order with id 42. (Instance: /orders/42)</c>.
/// </remarks>
[Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "Constructors are intentionally limited for this exception.")]
public class ProblemDetailsException : HttpRequestException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsException"/> class
    /// using the specified problem details.
    /// </summary>
    /// <param name="problemDetails">The parsed problem details describing the error, or <see langword="null"/> if none are available.</param>
    public ProblemDetailsException(
        ProblemDetails? problemDetails = null)
        : this(message: null, inner: null, problemDetails)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error, or <see langword="null"/> to derive the message from <paramref name="problemDetails"/>.</param>
    /// <param name="problemDetails">The parsed problem details describing the error, or <see langword="null"/> if none are available.</param>
    public ProblemDetailsException(
        string? message,
        ProblemDetails? problemDetails = null)
        : this(message, inner: null, problemDetails)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error, or <see langword="null"/> to derive the message from <paramref name="problemDetails"/>.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is specified.</param>
    /// <param name="problemDetails">The parsed problem details describing the error, or <see langword="null"/> if none are available.</param>
    public ProblemDetailsException(
        string? message,
        Exception? inner,
        ProblemDetails? problemDetails = null)
        : base(GetMessage(problemDetails, message), inner)
    {
        ProblemDetails = problemDetails;
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsException"/> class
    /// with a specified error message, inner exception, and HTTP status code.
    /// </summary>
    /// <param name="message">The message that describes the error, or <see langword="null"/> to derive the message from <paramref name="problemDetails"/> or <paramref name="statusCode"/>.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is specified.</param>
    /// <param name="statusCode">The HTTP status code associated with the response. Ignored when <paramref name="problemDetails"/> specifies a status.</param>
    /// <param name="problemDetails">The parsed problem details describing the error, or <see langword="null"/> if none are available.</param>
    /// <remarks>This constructor is only available on target frameworks that support setting <see cref="HttpRequestException.StatusCode"/>.</remarks>
    public ProblemDetailsException(
        string? message,
        Exception? inner,
        HttpStatusCode? statusCode,
        ProblemDetails? problemDetails = null)
        : base(
            message: GetMessage(problemDetails, message, statusCode),
            inner: inner,
            statusCode: GetStatusCode(problemDetails, statusCode))
    {
        ProblemDetails = problemDetails;
    }
#endif

    /// <summary>
    /// Gets the parsed problem details from the response body associated with this exception.
    /// </summary>
    /// <value>The <see cref="System.Net.Http.Json.ProblemDetails"/> parsed from the response body, or <see langword="null"/> if none were available.</value>
    public ProblemDetails? ProblemDetails { get; }

    private static string? GetMessage(
        ProblemDetails? problemDetails,
        string? message = null,
        HttpStatusCode? statusCode = null)
    {
        var statusMessage = GetStatusMessage(problemDetails, statusCode);
        if (problemDetails is null && string.IsNullOrEmpty(message))
            return statusMessage;

        var builder = new StringBuilder(128);

        AppendPart(builder, problemDetails?.Title);
        AppendPart(builder, problemDetails?.Detail);

        if (builder.Length == 0)
            AppendPart(builder, message);

        AppendPart(builder, problemDetails?.Instance, "Instance");
        AppendPart(builder, problemDetails?.Type, "Type");

        if (builder.Length == 0)
            return statusMessage;

        if (statusMessage is null)
            return builder.ToString();

        return $"{statusMessage}: {builder}";
    }

    private static void AppendPart(
        StringBuilder builder,
        string? value,
        string? named = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (builder.Length > 0)
            builder.Append(' ');

        if (string.IsNullOrWhiteSpace(named))
        {
            builder.Append(value);
            return;
        }

        builder
            .Append('(')
            .Append(named)
            .Append(": ")
            .Append(value)
            .Append(')');
    }

    private static string? GetStatusMessage(
        ProblemDetails? problemDetails,
        HttpStatusCode? statusCode)
    {
        var effectiveStatusCode = GetStatusCode(problemDetails, statusCode);

        return effectiveStatusCode is not null
            ? $"{(int)effectiveStatusCode.Value} {effectiveStatusCode.Value}"
            : null;
    }

    private static HttpStatusCode? GetStatusCode(
        ProblemDetails? problemDetails,
        HttpStatusCode? statusCode)
    {
        return (HttpStatusCode?)problemDetails?.Status ?? statusCode;
    }
}
