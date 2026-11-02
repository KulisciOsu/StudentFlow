using Microsoft.AspNetCore.Mvc;

namespace StudentFlow.Api.Authentication;

public static class ApiProblems
{
    public static ProblemDetails Create(HttpContext context, int status, string code, string title, string detail,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails { Type = "about:blank", Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (errors is not null) problem.Extensions["errors"] = errors;
        return problem;
    }

    public static ObjectResult Result(HttpContext context, int status, string code, string title, string detail,
        IDictionary<string, string[]>? errors = null) => new(Create(context, status, code, title, detail, errors))
    {
        StatusCode = status,
        ContentTypes = { "application/problem+json" }
    };

    public static Task WriteAsync(HttpContext context, int status, string code, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(Create(context, status, code, title, detail),
            options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
