using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Authentication;
using StudentFlow.Api.Models;
using StudentFlow.Core.Authentication;
using StudentFlow.Core.ProjectMembers;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API členů projektu (KAN-50).
/// </summary>
/// <remarks>
/// Volající se určuje jen ze serverem ověřené session. Členství a vedení se ověřuje při každém požadavku,
/// takže odebraný člen ztrácí přístup okamžitě. Nečlen dostane stejnou odpověď 404 jako u neexistujícího projektu.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/members")]
public sealed class ProjectMembersController(ProjectMemberService service) : ControllerBase
{
    /// <summary>
    /// Vrátí název týmu a aktuální členy projektu; smí každý člen, vedoucí i běžný.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <param name="cancellationToken">Token pro zrušení požadavku.</param>
    /// <returns>200 se seznamem, nebo 404 PROJECT_NOT_FOUND, pokud projekt neexistuje či volající není členem.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProjectMembersDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(int projectId, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(projectId, CallerId(), cancellationToken);
        if (result is not { Status: ListMembersStatus.Listed, TeamName: { } teamName })
            return ProjectNotFound();

        return Ok(new ProjectMembersDto(teamName, result.Members.Select(ProjectMemberDto.From).ToList()));
    }

    /// <summary>
    /// ID přihlášeného uživatele ze serverem ověřené session; ID zaslané klientem se nikdy nepoužívá.
    /// </summary>
    private long CallerId() => (HttpContext.Features.Get<SessionIdentity>()
        ?? throw new InvalidOperationException("Authenticated session is required.")).User.Id;

    /// <summary>
    /// Jednotná odpověď pro neexistující projekt i projekt, jehož volající není členem.
    /// </summary>
    private ObjectResult ProjectNotFound() => ApiProblems.Result(HttpContext, StatusCodes.Status404NotFound,
        "PROJECT_NOT_FOUND", "Projekt nenalezen", "Projekt neexistuje nebo k němu nemáte přístup.");
}
