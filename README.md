# ProblemJson

`HttpResponseMessage` extensions for detecting, reading, and throwing on `application/problem+json`
responses, as defined by [RFC 9457 (Problem Details for HTTP APIs)](https://www.rfc-editor.org/rfc/rfc9457).

[![Build Project](https://github.com/loresoft/ProblemJson/actions/workflows/dotnet.yml/badge.svg)](https://github.com/loresoft/ProblemJson/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/loresoft/ProblemJson.svg)](https://github.com/loresoft/ProblemJson/blob/main/LICENSE)
[![Coverage Status](https://coveralls.io/repos/github/loresoft/ProblemJson/badge.svg?branch=main)](https://coveralls.io/github/loresoft/ProblemJson?branch=main)
[![NuGet](https://img.shields.io/nuget/v/ProblemJson.svg)](https://www.nuget.org/packages/ProblemJson/)

> [!NOTE]
> There is an outstanding .NET API proposal to add this functionality to the
> `System.Net.Http.Json` library: [dotnet/runtime#131046](https://github.com/dotnet/runtime/issues/131046).

## Features

- Detect problem responses via the `Content-Type` header (`application/problem+json`).
- Deserialize the response body into a strongly-typed `ProblemDetails` object, including extension members.
- Throw a `ProblemDetailsException` (derived from `HttpRequestException`) that carries the parsed problem details.
- Source-generated JSON serialization for trimming/AOT-friendly, allocation-conscious parsing.
- Broad target framework support: `.NET Framework 4.6.2`, `.NET Standard 2.0`, `.NET 8`, `.NET 9`, and `.NET 10`.

## Installation

Install from [NuGet](https://www.nuget.org/):

```shell
dotnet add package ProblemJson
```

## Usage

The extension methods live in the `System.Net.Http.Json` namespace, so they are available anywhere
`HttpResponseMessage` is already in scope.

### Detect a problem response

```csharp
using System.Net.Http.Json;

using var client = new HttpClient();
using var response = await client.GetAsync("https://api.example.com/orders/42");

if (response.IsProblemJson())
{
    // The body is application/problem+json
}
```

### Read the problem details

```csharp
ProblemDetails? problem = await response.ReadProblemJsonAsync();
if (problem is not null)
{
    Console.WriteLine($"{problem.Status} {problem.Title}: {problem.Detail}");

    // Access RFC 9457 extension members
    foreach (var extension in problem.Extensions)
        Console.WriteLine($"{extension.Key}: {extension.Value}");
}
```

You can also supply your own `JsonSerializerOptions`:

```csharp
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
ProblemDetails? problem = await response.ReadProblemJsonAsync(options);
```

Both overloads return `null` when the response is not an `application/problem+json` payload.

### Throw on a problem response

`ThrowIfProblemJsonAsync` throws a `ProblemDetailsException` when the body is a problem payload,
and returns the same response otherwise for chaining. It does not validate the status code on its own,
so continue to call `EnsureSuccessStatusCode` as needed.

```csharp
using var response = await client.GetAsync("https://api.example.com/orders/42");

await response.ThrowIfProblemJsonAsync();
response.EnsureSuccessStatusCode();
```

Handle the exception and inspect the parsed details:

```csharp
try
{
    await response.ThrowIfProblemJsonAsync();
}
catch (ProblemDetailsException ex)
{
    // ex.Message is derived from Detail, Title, or the HTTP status code
    ProblemDetails? details = ex.ProblemDetails;
}
```

## API

### `ProblemJsonExtensions`

| Method                      | Description                                                           |
| --------------------------- | --------------------------------------------------------------------- |
| `IsProblemJson()`           | Returns `true` when the `Content-Type` is `application/problem+json`. |
| `ReadProblemJsonAsync()`    | Reads the response body into `ProblemDetails`.                        |
| `ThrowIfProblemJsonAsync()` | Throws `ProblemDetailsException` when the body is a problem payload.  |

### `ProblemDetails`

A model for the RFC 9457 problem details object.

| Property     | JSON             | Description                                             |
| ------------ | ---------------- | ------------------------------------------------------- |
| `Type`       | `type`           | URI reference identifying the problem type.             |
| `Title`      | `title`          | Short, human-readable summary of the problem type.      |
| `Status`     | `status`         | HTTP status code for this occurrence of the problem.    |
| `Detail`     | `detail`         | Human-readable explanation specific to this occurrence. |
| `Instance`   | `instance`       | URI reference identifying the specific occurrence.      |
| `Extensions` | (extension data) | Additional problem-type-specific members.               |

### `ProblemDetailsException`

Derives from `HttpRequestException`. The message is derived from the problem's `Detail`, then `Title`,
then an explicitly supplied message, and finally the HTTP status code. The parsed payload is exposed
through the `ProblemDetails` property. On .NET 5+ the exception's `StatusCode` is populated from the response.
