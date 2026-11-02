using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StudentFlow.Api.Authentication;

public sealed class CsrfValidationFilter(IAntiforgery antiforgery) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.IsNullOrWhiteSpace(context.HttpContext.Request.Headers["X-CSRF-TOKEN"]))
        {
            context.Result = InvalidToken(context);
            return;
        }
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = InvalidToken(context);
            return;
        }
        await next();
    }
    private static Microsoft.AspNetCore.Mvc.ObjectResult InvalidToken(ActionExecutingContext context) =>
        ApiProblems.Result(context.HttpContext, 403, "CSRF_INVALID", "Ověření požadavku se nezdařilo", "Obnovte ověření a zkuste to znovu.");
}
