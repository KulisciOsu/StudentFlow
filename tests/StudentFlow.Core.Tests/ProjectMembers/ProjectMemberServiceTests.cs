using StudentFlow.Core.Authentication;
using StudentFlow.Core.ProjectMembers;

namespace StudentFlow.Core.Tests.ProjectMembers;

/// <summary>
/// Jednotkové testy pravidel zobrazení, přidání a odebrání členů projektu.
/// </summary>
public class ProjectMemberServiceTests
{
    private const int ProjectId = 7;
    private const int MissingProjectId = 999;
    private const long LeaderId = 1;
    private const long MemberId = 2;
    private const long OutsiderId = 3;
    private const long TeacherId = 4;
    private const long InactiveId = 5;
    private const long CandidateId = 6;
    private const long UnknownUserId = 12345;

    private readonly InMemoryProjectMemberStore store = new();
    private readonly ProjectMemberService service;

    /// <summary>
    /// Každý test dostane vlastní projekt s vedoucím a jedním běžným členem.
    /// </summary>
    public ProjectMemberServiceTests()
    {
        store.AddAccount(LeaderId, "Petra Svobodová", "petra@example.test", AccountType.Member);
        store.AddAccount(MemberId, "Jan Novák", "jan@example.test", AccountType.Member);
        store.AddAccount(OutsiderId, "Lucie Černá", "lucie@example.test", AccountType.Member);
        store.AddAccount(TeacherId, "Eva Horáková", "eva@example.test", AccountType.Teacher);
        store.AddAccount(InactiveId, "Martin Král", "martin@example.test", AccountType.Member, isActive: false);
        store.AddAccount(CandidateId, "Tomáš Dvořák", "tomas@example.test", AccountType.Member);
        store.AddMembership(ProjectId, LeaderId, isLeader: true);
        store.AddMembership(ProjectId, MemberId, isLeader: false);
        service = new ProjectMemberService(store);
    }

    /// <summary>
    /// Seznam smí číst každý aktuální člen, vedoucí i běžný člen.
    /// </summary>
    [Theory]
    [InlineData(LeaderId)]
    [InlineData(MemberId)]
    public async Task List_AnyCurrentMember_ReturnsAllMembers(long callerId)
    {
        var result = await service.ListAsync(ProjectId, callerId);

        Assert.Equal(ListMembersStatus.Listed, result.Status);
        Assert.Equal(new[] { LeaderId, MemberId }, result.Members.Select(member => member.UserId));
    }

    /// <summary>
    /// Nečlen existujícího projektu dostane stejný výsledek jako u neexistujícího projektu.
    /// </summary>
    [Theory]
    [InlineData(ProjectId, OutsiderId)]
    [InlineData(MissingProjectId, LeaderId)]
    public async Task List_NonMemberOrMissingProject_ReturnsProjectNotFoundWithoutMembers(int projectId, long callerId)
    {
        var result = await service.ListAsync(projectId, callerId);

        Assert.Equal(ListMembersStatus.ProjectNotFound, result.Status);
        Assert.Empty(result.Members);
    }

    /// <summary>
    /// Vedoucí přidá aktivního studenta jako běžného člena; okolní mezery e-mailu nevadí.
    /// </summary>
    [Fact]
    public async Task AddByEmail_LeaderAddsActiveStudent_AddsRegularMember()
    {
        var result = await service.AddByEmailAsync(ProjectId, LeaderId, "  tomas@example.test ");

        Assert.Equal(AddMemberStatus.Added, result.Status);
        Assert.NotNull(result.Member);
        Assert.Equal(CandidateId, result.Member.UserId);
        Assert.False(result.Member.IsLeader);
        Assert.Equal(LeaderId, store.AddedBy[(ProjectId, CandidateId)]);
        Assert.Equal(3, store.CountMembers(ProjectId));
    }

    /// <summary>
    /// Prázdný nebo neplatný e-mail se odmítne dříve, než se služba obrátí na úložiště.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tomas.example.test")]
    [InlineData("tomas@@example.test")]
    [InlineData("tomas dvorak@example.test")]
    [InlineData("@example.test")]
    public async Task AddByEmail_InvalidEmail_ReturnsInvalidEmailWithoutTouchingStore(string? email)
    {
        var result = await service.AddByEmailAsync(ProjectId, LeaderId, email);

        Assert.Equal(AddMemberStatus.InvalidEmail, result.Status);
        Assert.Null(result.Member);
        Assert.Equal(0, store.CallCount);
    }

    /// <summary>
    /// Hranice délky e-mailu je 320 znaků stejně jako při registraci.
    /// </summary>
    [Theory]
    [InlineData(320, AddMemberStatus.AccountNotFound)]
    [InlineData(321, AddMemberStatus.InvalidEmail)]
    public async Task AddByEmail_EmailLengthBoundary_RejectsOnlyLongerThan320(int length, AddMemberStatus expected)
    {
        const string domain = "@example.test";
        var email = new string('a', length - domain.Length) + domain;

        var result = await service.AddByEmailAsync(ProjectId, LeaderId, email);

        Assert.Equal(expected, result.Status);
    }

    /// <summary>
    /// Nečlen ani volající u neexistujícího projektu nemůže přidávat a projekt se mu neprozradí.
    /// </summary>
    [Theory]
    [InlineData(ProjectId, OutsiderId)]
    [InlineData(MissingProjectId, LeaderId)]
    public async Task AddByEmail_NonMemberOrMissingProject_ReturnsProjectNotFound(int projectId, long callerId)
    {
        var result = await service.AddByEmailAsync(projectId, callerId, "tomas@example.test");

        Assert.Equal(AddMemberStatus.ProjectNotFound, result.Status);
        Assert.False(store.IsMember(projectId, CandidateId));
    }

    /// <summary>
    /// Běžný člen projektu nesmí přidávat další členy.
    /// </summary>
    [Fact]
    public async Task AddByEmail_RegularMember_ReturnsNotLeaderAndAddsNobody()
    {
        var result = await service.AddByEmailAsync(ProjectId, MemberId, "tomas@example.test");

        Assert.Equal(AddMemberStatus.NotLeader, result.Status);
        Assert.Equal(2, store.CountMembers(ProjectId));
    }

    /// <summary>
    /// Neexistující i neaktivní účet se hlásí stejně jako účet, který v aplikaci není.
    /// </summary>
    [Theory]
    [InlineData("nikdo@example.test")]
    [InlineData("martin@example.test")]
    public async Task AddByEmail_UnknownOrInactiveAccount_ReturnsAccountNotFound(string email)
    {
        var result = await service.AddByEmailAsync(ProjectId, LeaderId, email);

        Assert.Equal(AddMemberStatus.AccountNotFound, result.Status);
        Assert.Equal(2, store.CountMembers(ProjectId));
    }

    /// <summary>
    /// Účet vyučujícího nelze přidat jako člena; vyučující dostávají přístup jinak (UC09).
    /// </summary>
    [Fact]
    public async Task AddByEmail_TeacherAccount_ReturnsAccountNotStudent()
    {
        var result = await service.AddByEmailAsync(ProjectId, LeaderId, "eva@example.test");

        Assert.Equal(AddMemberStatus.AccountNotStudent, result.Status);
        Assert.False(store.IsMember(ProjectId, TeacherId));
    }

    /// <summary>
    /// Existující člen, včetně samotného vedoucího, se znovu nepřidá a duplicita nevznikne.
    /// </summary>
    [Theory]
    [InlineData("jan@example.test")]
    [InlineData("petra@example.test")]
    public async Task AddByEmail_ExistingMember_ReturnsAlreadyMemberWithoutDuplicate(string email)
    {
        var result = await service.AddByEmailAsync(ProjectId, LeaderId, email);

        Assert.Equal(AddMemberStatus.AlreadyMember, result.Status);
        Assert.Equal(2, store.CountMembers(ProjectId));
        Assert.True(store.IsLeader(ProjectId, LeaderId));
    }

    /// <summary>
    /// Vedoucí odebere běžného člena; ostatní členství zůstávají.
    /// </summary>
    [Fact]
    public async Task Remove_LeaderRemovesMember_RemovesOnlyThatMembership()
    {
        var status = await service.RemoveAsync(ProjectId, LeaderId, MemberId);

        Assert.Equal(RemoveMemberStatus.Removed, status);
        Assert.False(store.IsMember(ProjectId, MemberId));
        Assert.True(store.IsLeader(ProjectId, LeaderId));
    }

    /// <summary>
    /// Vedoucí nemůže odebrat sám sebe; projekt musí mít vždy vedoucího.
    /// </summary>
    [Fact]
    public async Task Remove_LeaderRemovesSelf_ReturnsLeaderCannotBeRemoved()
    {
        var status = await service.RemoveAsync(ProjectId, LeaderId, LeaderId);

        Assert.Equal(RemoveMemberStatus.LeaderCannotBeRemoved, status);
        Assert.True(store.IsLeader(ProjectId, LeaderId));
    }

    /// <summary>
    /// Odebrání uživatele, který není členem projektu, skončí jako nenalezené členství.
    /// </summary>
    [Theory]
    [InlineData(OutsiderId)]
    [InlineData(UnknownUserId)]
    public async Task Remove_UserWhoIsNotMember_ReturnsMemberNotFound(long memberUserId)
    {
        var status = await service.RemoveAsync(ProjectId, LeaderId, memberUserId);

        Assert.Equal(RemoveMemberStatus.MemberNotFound, status);
        Assert.Equal(2, store.CountMembers(ProjectId));
    }

    /// <summary>
    /// Běžný člen nesmí odebrat nikoho, ani vedoucího, ani sebe.
    /// </summary>
    [Theory]
    [InlineData(LeaderId)]
    [InlineData(MemberId)]
    public async Task Remove_RegularMember_ReturnsNotLeaderAndRemovesNobody(long memberUserId)
    {
        var status = await service.RemoveAsync(ProjectId, MemberId, memberUserId);

        Assert.Equal(RemoveMemberStatus.NotLeader, status);
        Assert.Equal(2, store.CountMembers(ProjectId));
    }

    /// <summary>
    /// Nečlen ani volající u neexistujícího projektu nemůže odebírat a projekt se mu neprozradí.
    /// </summary>
    [Theory]
    [InlineData(ProjectId, OutsiderId)]
    [InlineData(MissingProjectId, LeaderId)]
    public async Task Remove_NonMemberOrMissingProject_ReturnsProjectNotFound(int projectId, long callerId)
    {
        var status = await service.RemoveAsync(projectId, callerId, MemberId);

        Assert.Equal(RemoveMemberStatus.ProjectNotFound, status);
        Assert.True(store.IsMember(ProjectId, MemberId));
    }

    /// <summary>
    /// Odebraný člen okamžitě ztratí přístup: další požadavky se vyhodnotí jako u cizího projektu.
    /// </summary>
    [Fact]
    public async Task Remove_RemovedMember_LosesAccessImmediately()
    {
        await service.RemoveAsync(ProjectId, LeaderId, MemberId);

        Assert.Equal(ListMembersStatus.ProjectNotFound, (await service.ListAsync(ProjectId, MemberId)).Status);
        Assert.Equal(AddMemberStatus.ProjectNotFound,
            (await service.AddByEmailAsync(ProjectId, MemberId, "tomas@example.test")).Status);
        Assert.Equal(RemoveMemberStatus.ProjectNotFound, await service.RemoveAsync(ProjectId, MemberId, LeaderId));
    }

    /// <summary>
    /// Pokud člena mezi kontrolou a zápisem odebere souběžný požadavek, výsledek je nenalezené členství.
    /// </summary>
    [Fact]
    public async Task Remove_MemberRemovedConcurrently_ReturnsMemberNotFound()
    {
        store.SimulateConcurrentRemoval = true;

        var status = await service.RemoveAsync(ProjectId, LeaderId, MemberId);

        Assert.Equal(RemoveMemberStatus.MemberNotFound, status);
        Assert.False(store.IsMember(ProjectId, MemberId));
    }
}
