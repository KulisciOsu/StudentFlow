using Microsoft.AspNetCore.Mvc.Filters;

namespace StudentFlow.Api.Authentication;

/// <summary>Před model bindingem ověří typ a skutečnou velikost JSON těla, i bez hlavičky Content-Length.</summary>
public sealed class AuthBodyLimitFilter : IAsyncResourceFilter
{
    /// <inheritdoc />
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!string.Equals(request.GetTypedHeaders().ContentType?.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = ApiProblems.Result(context.HttpContext, 415, "UNSUPPORTED_MEDIA_TYPE", "Nepodporovaný formát", "Pošlete požadavek ve formátu application/json.");
            return;
        }
        var limit = AuthErrorMiddleware.MaxAuthBytes;
        request.EnableBuffering(bufferThreshold: limit + 1);
        var buffer = new byte[4096];
        var total = 0;
        while (total <= limit)
        {
            var read = await request.Body.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, limit + 1 - total)), context.HttpContext.RequestAborted);
            if (read == 0) break;
            total += read;
        }
        request.Body.Position = 0;
        if (total > limit)
        {
            context.Result = ApiProblems.Result(context.HttpContext, 413, "PAYLOAD_TOO_LARGE",
                "Požadavek je příliš velký", "Zadané údaje překračují povolenou velikost.");
            return;
        }
        await next();
    }
}
