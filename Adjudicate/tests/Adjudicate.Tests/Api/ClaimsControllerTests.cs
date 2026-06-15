using System.Net;
using System.Net.Http.Json;
using Adjudicate.Api.DTOs;
using Adjudicate.Domain.Entities;
using Adjudicate.Domain.Enums;
using Adjudicate.Infrastructure.Persistence;
using Adjudicate.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adjudicate.Tests.Api;

[Collection(SqlServerCollection.Name)]
public class ClaimsControllerTests
{
    private static readonly DateOnly ServiceDate = new(2024, 6, 15);
    private static readonly DateOnly Dob = new(1985, 1, 1);
    private static readonly DateOnly ActiveFrom = new(2024, 1, 1);

    private readonly MsSqlContainerFixture _fixture;

    public ClaimsControllerTests(MsSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private AdjudicateDbContext CreateDb() => _fixture.CreateDb();

    private HttpClient CreateClient() =>
        new AdjudicateApiFactory(_fixture.ConnectionString).CreateClient();

    private static string Code() => Guid.NewGuid().ToString("N")[..8].ToUpper();

    private static async Task ResetAsync(AdjudicateDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("DELETE FROM AdjudicationResults");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM ClaimLines");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Claims");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Coverages");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Members");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM Plans");
    }

    private static async Task<string> SeedMemberAsync(AdjudicateDbContext db, DateOnly? termDate = null)
    {
        var plan = Plan.Create($"P-{Code()}", "Test Plan");
        plan.AddCoverage(ServiceType.OfficeVisit, true);
        plan.AddCoverage(ServiceType.Laboratory, true);

        var memberNumber = $"M-{Code()}";
        var member = Member.Create(memberNumber, "Test", "Patient", Dob, ActiveFrom, termDate, plan.Id);

        db.Plans.Add(plan);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        return memberNumber;
    }

    // ── POST /api/claims ──────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_Returns201_WithValidRequest()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db);

        using var client = CreateClient();
        var request = new SubmitClaimRequest(
            memberNumber,
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var response = await client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SubmitClaimResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.ClaimId);
        Assert.StartsWith("CLM-", body.ClaimNumber);
        Assert.Equal("Submitted", body.Status);
    }

    [Fact]
    public async Task Submit_Returns404_WhenMemberNotFound()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        using var client = CreateClient();
        var request = new SubmitClaimRequest(
            "M-NOTEXIST",
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var response = await client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Submit_Returns400_WhenFutureServiceDate()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db);

        using var client = CreateClient();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var request = new SubmitClaimRequest(
            memberNumber,
            futureDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var response = await client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── POST /api/claims/{id}/adjudicate ──────────────────────────────────────

    [Fact]
    public async Task Adjudicate_Returns200Approved_WhenEligibleAndCovered()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db);

        using var client = CreateClient();
        var submitRequest = new SubmitClaimRequest(
            memberNumber,
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var submitResponse = await client.PostAsJsonAsync("/api/claims", submitRequest);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitClaimResponse>();

        var adjResponse = await client.PostAsync($"/api/claims/{submitted!.ClaimId}/adjudicate", null);

        Assert.Equal(HttpStatusCode.OK, adjResponse.StatusCode);

        var body = await adjResponse.Content.ReadFromJsonAsync<AdjudicateClaimResponse>();
        Assert.NotNull(body);
        Assert.Equal("Approved", body.Decision);
        Assert.Equal(150m, body.AllowedAmount);
        Assert.Null(body.DenialReason);
    }

    [Fact]
    public async Task Adjudicate_Returns200Denied_WhenMemberIneligible()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db, termDate: new DateOnly(2024, 6, 1));

        using var client = CreateClient();
        var submitRequest = new SubmitClaimRequest(
            memberNumber,
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var submitResponse = await client.PostAsJsonAsync("/api/claims", submitRequest);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitClaimResponse>();

        var adjResponse = await client.PostAsync($"/api/claims/{submitted!.ClaimId}/adjudicate", null);

        Assert.Equal(HttpStatusCode.OK, adjResponse.StatusCode);

        var body = await adjResponse.Content.ReadFromJsonAsync<AdjudicateClaimResponse>();
        Assert.NotNull(body);
        Assert.Equal("Denied", body.Decision);
        Assert.Equal("NotEligible", body.DenialReason);
    }

    [Fact]
    public async Task Adjudicate_Returns404_WhenClaimNotFound()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        using var client = CreateClient();
        var response = await client.PostAsync($"/api/claims/{Guid.NewGuid()}/adjudicate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Adjudicate_Returns409_WhenAlreadyAdjudicated()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db);

        using var client = CreateClient();
        var submitRequest = new SubmitClaimRequest(
            memberNumber,
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var submitResponse = await client.PostAsJsonAsync("/api/claims", submitRequest);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitClaimResponse>();

        await client.PostAsync($"/api/claims/{submitted!.ClaimId}/adjudicate", null);
        var secondResponse = await client.PostAsync($"/api/claims/{submitted.ClaimId}/adjudicate", null);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    // ── GET /api/claims/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns200_WithClaimDetails()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db);

        using var client = CreateClient();
        var submitRequest = new SubmitClaimRequest(
            memberNumber,
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var submitResponse = await client.PostAsJsonAsync("/api/claims", submitRequest);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitClaimResponse>();

        var getResponse = await client.GetAsync($"/api/claims/{submitted!.ClaimId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.NotNull(body);
        Assert.Equal(submitted.ClaimId, body.Id);
        Assert.Single(body.Lines);
        Assert.Equal("99213", body.Lines[0].ServiceCode);
        Assert.Null(body.Adjudication);
    }

    [Fact]
    public async Task GetById_IncludesAdjudication_AfterAdjudicate()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        string memberNumber;
        await using (var db = CreateDb())
            memberNumber = await SeedMemberAsync(db);

        using var client = CreateClient();
        var submitRequest = new SubmitClaimRequest(
            memberNumber,
            ServiceDate,
            [new ClaimLineRequest("99213", ServiceType.OfficeVisit, 1, 150m)]);

        var submitResponse = await client.PostAsJsonAsync("/api/claims", submitRequest);
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitClaimResponse>();

        await client.PostAsync($"/api/claims/{submitted!.ClaimId}/adjudicate", null);

        var getResponse = await client.GetAsync($"/api/claims/{submitted.ClaimId}");
        var body = await getResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        Assert.NotNull(body);
        Assert.NotNull(body.Adjudication);
        Assert.Equal("Approved", body.Adjudication.Decision);
        Assert.Equal(150m, body.Adjudication.AllowedAmount);
        Assert.Equal("Approved", body.Status);
    }

    [Fact]
    public async Task GetById_Returns404_WhenClaimNotFound()
    {
        await using (var db = CreateDb()) await ResetAsync(db);

        using var client = CreateClient();
        var response = await client.GetAsync($"/api/claims/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
