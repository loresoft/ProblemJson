using System.Net.Http.Json;
using System.Text.Json;

namespace ProblemJson.Tests;

public class ProblemDetailsTests
{
    [Fact]
    public void ExtensionsDefaultsToEmptyDictionary()
    {
        var problem = new ProblemDetails();

        Assert.NotNull(problem.Extensions);
        Assert.Empty(problem.Extensions);
    }

    [Fact]
    public void SerializeWritesKnownMembersInExpectedOrder()
    {
        var problem = new ProblemDetails
        {
            Type = "https://example.com/probs/out-of-credit",
            Title = "You do not have enough credit.",
            Status = 403,
            Detail = "Your current balance is 30.",
            Instance = "/account/12345/msgs/abc",
        };

        var json = JsonSerializer.Serialize(problem, ProblemJsonSerializerContext.Default.ProblemDetails);

        var expected = "{\"type\":\"https://example.com/probs/out-of-credit\","
            + "\"title\":\"You do not have enough credit.\","
            + "\"status\":403,"
            + "\"detail\":\"Your current balance is 30.\","
            + "\"instance\":\"/account/12345/msgs/abc\"}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public void SerializeOmitsNullMembers()
    {
        var problem = new ProblemDetails { Status = 404 };

        var json = JsonSerializer.Serialize(problem, ProblemJsonSerializerContext.Default.ProblemDetails);

        Assert.Equal("{\"status\":404}", json);
    }

    [Fact]
    public void DeserializeReadsKnownMembers()
    {
        var json = "{\"type\":\"https://example.com/probs/out-of-credit\","
            + "\"title\":\"You do not have enough credit.\","
            + "\"status\":403,"
            + "\"detail\":\"Your current balance is 30.\","
            + "\"instance\":\"/account/12345/msgs/abc\"}";

        var problem = JsonSerializer.Deserialize(json, ProblemJsonSerializerContext.Default.ProblemDetails);

        Assert.NotNull(problem);
        Assert.Equal("https://example.com/probs/out-of-credit", problem.Type);
        Assert.Equal("You do not have enough credit.", problem.Title);
        Assert.Equal(403, problem.Status);
        Assert.Equal("Your current balance is 30.", problem.Detail);
        Assert.Equal("/account/12345/msgs/abc", problem.Instance);
    }

    [Fact]
    public void DeserializeCapturesUnknownMembersAsExtensions()
    {
        var json = "{\"status\":403,\"balance\":30,\"accounts\":[\"/account/12345\"]}";

        var problem = JsonSerializer.Deserialize(json, ProblemJsonSerializerContext.Default.ProblemDetails);

        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("balance"));
        Assert.Equal(30, problem.Extensions["balance"].GetInt32());
        Assert.True(problem.Extensions.ContainsKey("accounts"));
    }

    [Fact]
    public void RoundTripPreservesExtensions()
    {
        var json = "{\"status\":403,\"balance\":30}";

        var problem = JsonSerializer.Deserialize(json, ProblemJsonSerializerContext.Default.ProblemDetails);
        var serialized = JsonSerializer.Serialize(problem!, ProblemJsonSerializerContext.Default.ProblemDetails);

        Assert.Equal(json, serialized);
    }
}
