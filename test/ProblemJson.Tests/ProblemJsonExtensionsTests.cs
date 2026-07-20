using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ProblemJson.Tests;

public class ProblemJsonExtensionsTests
{
    private const string ProblemJson =
        "{\"type\":\"https://example.com/probs/out-of-credit\","
        + "\"title\":\"You do not have enough credit.\","
        + "\"status\":403,"
        + "\"detail\":\"Your current balance is 30.\"}";

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string content,
        string mediaType)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType),
        };
    }

    [Fact]
    public void IsProblemJsonThrowsWhenResponseIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((HttpResponseMessage)null!).IsProblemJson());
    }

    [Fact]
    public void IsProblemJsonReturnsTrueForProblemMediaType()
    {
        using var response = CreateResponse(HttpStatusCode.Forbidden, ProblemJson, "application/problem+json");

        Assert.True(response.IsProblemJson());
    }

    [Fact]
    public void IsProblemJsonIgnoresCase()
    {
        using var response = CreateResponse(HttpStatusCode.Forbidden, ProblemJson, "APPLICATION/PROBLEM+JSON");

        Assert.True(response.IsProblemJson());
    }

    [Fact]
    public void IsProblemJsonReturnsFalseForPlainJson()
    {
        using var response = CreateResponse(HttpStatusCode.OK, "{}", "application/json");

        Assert.False(response.IsProblemJson());
    }

    [Fact]
    public void IsProblemJsonReturnsFalseWhenNoContent()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        Assert.False(response.IsProblemJson());
    }

    [Fact]
    public async Task ReadProblemJsonAsyncThrowsWhenResponseIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ((HttpResponseMessage)null!).ReadProblemJsonAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadProblemJsonAsyncReturnsNullForNonProblemResponse()
    {
        using var response = CreateResponse(HttpStatusCode.OK, "{}", "application/json");

        var problem = await response.ReadProblemJsonAsync(CancellationToken.None);

        Assert.Null(problem);
    }

    [Fact]
    public async Task ReadProblemJsonAsyncDeserializesProblemDetails()
    {
        using var response = CreateResponse(HttpStatusCode.Forbidden, ProblemJson, "application/problem+json");

        var problem = await response.ReadProblemJsonAsync(CancellationToken.None);

        Assert.NotNull(problem);
        Assert.Equal("You do not have enough credit.", problem.Title);
        Assert.Equal(403, problem.Status);
        Assert.Equal("Your current balance is 30.", problem.Detail);
    }

    [Fact]
    public async Task ReadProblemJsonAsyncWithOptionsDeserializesProblemDetails()
    {
        using var response = CreateResponse(HttpStatusCode.Forbidden, ProblemJson, "application/problem+json");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var problem = await response.ReadProblemJsonAsync(options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
    }

    [Fact]
    public async Task ThrowIfProblemJsonAsyncReturnsSameResponseForNonProblem()
    {
        using var response = CreateResponse(HttpStatusCode.OK, "{}", "application/json");

        var result = await response.ThrowIfProblemJsonAsync(CancellationToken.None);

        Assert.Same(response, result);
    }

    [Fact]
    public async Task ThrowIfProblemJsonAsyncThrowsForProblemResponse()
    {
        using var response = CreateResponse(HttpStatusCode.Forbidden, ProblemJson, "application/problem+json");

        var exception = await Assert.ThrowsAsync<ProblemDetailsException>(
            () => response.ThrowIfProblemJsonAsync(CancellationToken.None));

        Assert.Equal("Your current balance is 30.", exception.Message);
        Assert.NotNull(exception.ProblemDetails);
        Assert.Equal(403, exception.ProblemDetails.Status);
    }

    [Fact]
    public async Task ThrowIfProblemJsonAsyncThrowsWhenResponseIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ((HttpResponseMessage)null!).ThrowIfProblemJsonAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ThrowIfProblemJsonAsyncWithOptionsThrowsForProblemResponse()
    {
        using var response = CreateResponse(HttpStatusCode.Forbidden, ProblemJson, "application/problem+json");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var exception = await Assert.ThrowsAsync<ProblemDetailsException>(
            () => response.ThrowIfProblemJsonAsync(options, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(403, exception.ProblemDetails!.Status);
    }
}
