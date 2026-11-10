using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Přihlášení uživatelé pro testy controllerů. Odpovídají členům v <see cref="FakeKanbanBoardRepository"/>.
/// </summary>
public static class TestUsers
{
    /// <summary>
    /// Vedoucí projektu 1 (Petra Svobodová).
    /// </summary>
    public const long Leader = 1;

    /// <summary>
    /// Člen projektu 1 (Jan Novák).
    /// </summary>
    public const long Member = 2;

    /// <summary>
    /// Člen projektu 2, který do projektu 1 nepatří (Karel Malý).
    /// </summary>
    public const long Outsider = 6;

    /// <summary>
    /// Vyučující, který není členem žádného projektu.
    /// </summary>
    public const long Teacher = 50;

    /// <summary>
    /// Nastaví controlleru přihlášeného uživatele.
    /// </summary>
    /// <param name="controller">Testovaný controller.</param>
    /// <param name="userId">Identifikátor uživatele.</param>
    /// <returns>Stejný controller.</returns>
    public static T SignIn<T>(T controller, long userId) where T : ControllerBase
    {
        var accountType = "STUDENT";

        if (userId == Teacher)
        {
            accountType = "TEACHER";
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Role, accountType),
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")),
            },
        };

        return controller;
    }
}
