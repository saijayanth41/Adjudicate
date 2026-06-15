using System.Text.Json.Serialization;
using Adjudicate.Api.Middleware;
using Adjudicate.Domain.Adjudication;
using Adjudicate.Domain.Adjudication.Rules;
using Adjudicate.Infrastructure.Persistence;
using Adjudicate.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AdjudicateDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly(typeof(AdjudicateDbContext).Assembly.GetName().Name)));

builder.Services.AddScoped<IAdjudicationRule, MemberEligibilityRule>();
builder.Services.AddScoped<IAdjudicationRule, PlanCoverageRule>();
builder.Services.AddScoped<IAdjudicationRule, DuplicateClaimRule>();
builder.Services.AddScoped<IAdjudicationEngine>(sp =>
    new AdjudicationEngine(sp.GetRequiredService<IEnumerable<IAdjudicationRule>>()));

builder.Services.AddScoped<IClaimSubmissionService, ClaimSubmissionService>();
builder.Services.AddScoped<IClaimAdjudicationService, ClaimAdjudicationService>();
builder.Services.AddScoped<IClaimQueryService, ClaimQueryService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.ExampleFilters());
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
