using Adjudicate.Api.DTOs;
using Adjudicate.Infrastructure.Models;
using Adjudicate.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Adjudicate.Api.Examples;

namespace Adjudicate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ClaimsController : ControllerBase
{
    private readonly IClaimSubmissionService _submissionService;
    private readonly IClaimAdjudicationService _adjudicationService;
    private readonly IClaimQueryService _queryService;

    public ClaimsController(
        IClaimSubmissionService submissionService,
        IClaimAdjudicationService adjudicationService,
        IClaimQueryService queryService)
    {
        _submissionService = submissionService;
        _adjudicationService = adjudicationService;
        _queryService = queryService;
    }

    [HttpPost]
    [SwaggerRequestExample(typeof(SubmitClaimRequest), typeof(SubmitClaimRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(SubmitClaimResponseExample))]
    [ProducesResponseType(typeof(SubmitClaimResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromBody] SubmitClaimRequest request, CancellationToken ct)
    {
        var input = new ClaimSubmissionInput(
            request.MemberNumber,
            request.ServiceDate,
            request.Lines
                .Select(l => new ClaimLineInput(l.ServiceCode, l.ServiceType, l.Quantity, l.BilledAmount))
                .ToList());

        var result = await _submissionService.SubmitAsync(input, ct);

        var response = new SubmitClaimResponse(
            result.ClaimId,
            result.ClaimNumber,
            result.ServiceDate,
            result.Status);

        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, response);
    }

    [HttpPost("{id:guid}/adjudicate")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AdjudicateClaimResponseExample))]
    [ProducesResponseType(typeof(AdjudicateClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Adjudicate(Guid id, CancellationToken ct)
    {
        var outcome = await _adjudicationService.AdjudicateAsync(id, ct);

        var response = new AdjudicateClaimResponse(
            id,
            outcome.Decision.ToString(),
            outcome.AllowedAmount,
            outcome.DenialReason?.ToString());

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ClaimResponseExample))]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _queryService.GetByIdAsync(id, ct);

        var response = new ClaimResponse(
            result.Id,
            result.ClaimNumber,
            result.MemberId,
            result.ServiceDate,
            result.Status.ToString(),
            result.SubmittedAt,
            result.Lines
                .Select(l => new ClaimLineResponse(l.Id, l.ServiceCode, l.ServiceType.ToString(), l.Quantity, l.BilledAmount))
                .ToList(),
            result.Adjudication is null
                ? null
                : new AdjudicationResultResponse(
                    result.Adjudication.Decision.ToString(),
                    result.Adjudication.AllowedAmount,
                    result.Adjudication.DenialReason?.ToString(),
                    result.Adjudication.AdjudicatedAt));

        return Ok(response);
    }
}
