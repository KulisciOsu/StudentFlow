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
    /// Přidá registrovaného aktivního studenta podle e-mailu jako běžného člena; smí pouze vedoucí týmu.
    /// </summary>
    /// <remarks>Stejně jako registrace vyžaduje CSRF token a JSON tělo do 16 KiB bez neznámých polí.</remarks>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <param name="request">E-mail přidávaného studenta.</param>
    /// <param name="cancellationToken">Token pro zrušení požadavku.</param>
    /// <returns>201 s novým členem, nebo chyba podle pravidel členství.</returns>
    [HttpPost]
    [RequestSizeLimit(AuthErrorMiddleware.MaxAuthBytes)]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    [ServiceFilter(typeof(AuthBodyLimitFilter))]
    [ProducesResponseType(typeof(ProjectMemberAddedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(int projectId, AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddByEmailAsync(projectId, CallerId(), request.Email, cancellationToken);
        return result.Status switch
        {
            AddMemberStatus.Added when result.Member is { } member =>
                StatusCode(StatusCodes.Status201Created, new ProjectMemberAddedDto(ProjectMemberDto.From(member))),
            AddMemberStatus.InvalidEmail => ApiProblems.Result(HttpContext, StatusCodes.Status400BadRequest,
                "VALIDATION_FAILED", "Neplatné vstupní údaje", "Opravte označená pole.",
                new Dictionary<string, string[]> { ["email"] = [AccountEmailRules.InvalidMessage] }),
            AddMemberStatus.ProjectNotFound => ProjectNotFound(),
            AddMemberStatus.NotLeader => LeaderRequired(),
            AddMemberStatus.AccountNotFound => ApiProblems.Result(HttpContext, StatusCodes.Status422UnprocessableEntity,
                "ACCOUNT_NOT_FOUND", "Účet nenalezen", "Účet s tímto e-mailem v StudentFlow neexistuje."),
            AddMemberStatus.AccountNotStudent => ApiProblems.Result(HttpContext, StatusCodes.Status422UnprocessableEntity,
                "ACCOUNT_NOT_STUDENT", "Účet nelze přidat", "Do týmu lze přidat jen účet studenta."),
            AddMemberStatus.AlreadyMember => ApiProblems.Result(HttpContext, StatusCodes.Status409Conflict,
                "MEMBER_ALREADY_EXISTS", "Člen již existuje", "Tento uživatel už je členem týmu."),
            _ => throw new InvalidOperationException($"Unexpected add member result {result.Status}.")
        };
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

    /// <summary>
    /// Odpověď pro člena projektu, který není vedoucím a chce měnit členy.
    /// </summary>
    private ObjectResult LeaderRequired() => ApiProblems.Result(HttpContext, StatusCodes.Status403Forbidden,
        "PROJECT_LEADER_REQUIRED", "Nedostatečné oprávnění", "Členy projektu může spravovat jen vedoucí týmu.");
}
