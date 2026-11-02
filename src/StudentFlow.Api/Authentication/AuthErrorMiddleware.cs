using Npgsql;

namespace StudentFlow.Api.Authentication;

public sealed class AuthErrorMiddleware(RequestDelegate next, ILogger<AuthErrorMiddleware> logger)
{
    public const int MaxAuthBytes = 16 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }
        context.Response.Headers.CacheControl = "no-store";
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        try
        {
            if ((context.Request.Path == "/api/auth/register" || context.Request.Path == "/api/auth/login") && context.Request.ContentLength > MaxAuthBytes)
            {
                await WriteTooLargeAsync(context);
                return;
            }
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (BadHttpRequestException error) when (error.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            await WriteTooLargeAsync(context);
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            // Nelogovat výjimku ani vstupní DTO: mohou obsahovat hodnoty z DB/heslo.
            logger.LogError("Auth request failed. TraceId: {TraceId}; failure type: {FailureType}",
                context.TraceIdentifier, error.GetType().Name);
            context.Response.Clear();
            if (error is NpgsqlException { IsTransient: true } or TimeoutException)
                await ApiProblems.WriteAsync(context, 503, "SERVICE_UNAVAILABLE", "Služba není dostupná", "Operaci se nepodařilo potvrdit. Zkuste to později.");
            else
                await ApiProblems.WriteAsync(context, 500, "INTERNAL_ERROR", "Operaci se nepodařilo dokončit", "Operaci se nepodařilo potvrdit. Zkuste to později.");
        }
    }

    public static Task WriteTooLargeAsync(HttpContext context) => ApiProblems.WriteAsync(context, 413,
        "PAYLOAD_TOO_LARGE", "Požadavek je příliš velký", "Zadané údaje překračují povolenou velikost.");
}
