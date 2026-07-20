using System.Net;
using System.Net.Http.Json;

namespace ProblemJson.Tests;

public class ProblemDetailsExceptionTests
{
    [Fact]
    public void MessageUsesDetailWhenAvailable()
    {
        var problem = new ProblemDetails
        {
            Title = "You do not have enough credit.",
            Detail = "Your current balance is 30.",
        };

        var exception = new ProblemDetailsException(problem);

        Assert.Equal("Your current balance is 30.", exception.Message);
        Assert.Same(problem, exception.ProblemDetails);
    }

    [Fact]
    public void MessageFallsBackToTitleWhenDetailMissing()
    {
        var problem = new ProblemDetails { Title = "You do not have enough credit." };

        var exception = new ProblemDetailsException(problem);

        Assert.Equal("You do not have enough credit.", exception.Message);
    }

    [Fact]
    public void MessageUsesProvidedMessageWhenProblemHasNoText()
    {
        var problem = new ProblemDetails { Status = 400 };

        var exception = new ProblemDetailsException("explicit message", problem);

        Assert.Equal("explicit message", exception.Message);
    }

    [Fact]
    public void MessageDerivesFromProblemStatusWhenNoTextOrMessage()
    {
        var problem = new ProblemDetails { Status = 404 };

        var exception = new ProblemDetailsException(message: null, problem);

        Assert.Equal("404 NotFound", exception.Message);
    }

    [Fact]
    public void MessageUsesNumericStatusWhenStatusIsNotAValidHttpStatus()
    {
        var problem = new ProblemDetails { Status = 799 };

        var exception = new ProblemDetailsException(message: null, problem);

        Assert.Equal("799 799", exception.Message);
    }

    [Fact]
    public void DetailTakesPrecedenceOverProvidedMessage()
    {
        var problem = new ProblemDetails { Detail = "detail wins" };

        var exception = new ProblemDetailsException("explicit message", problem);

        Assert.Equal("detail wins", exception.Message);
    }

    [Fact]
    public void InnerExceptionIsPreserved()
    {
        var inner = new InvalidOperationException("boom");

        var exception = new ProblemDetailsException("message", inner, problemDetails: null);

        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void ProblemDetailsIsNullWhenNotProvided()
    {
        var exception = new ProblemDetailsException("message");

        Assert.Null(exception.ProblemDetails);
    }

    [Fact]
    public void StatusCodeConstructorUsesProblemStatusOverArgument()
    {
        var problem = new ProblemDetails { Status = 403 };

        var exception = new ProblemDetailsException(
            message: null,
            inner: null,
            statusCode: HttpStatusCode.BadRequest,
            problemDetails: problem);

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }

    [Fact]
    public void StatusCodeConstructorUsesArgumentWhenProblemHasNoStatus()
    {
        var exception = new ProblemDetailsException(
            message: null,
            inner: null,
            statusCode: HttpStatusCode.BadGateway,
            problemDetails: null);

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal("502 BadGateway", exception.Message);
    }
}
