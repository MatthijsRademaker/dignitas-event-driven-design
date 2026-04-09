using EDA.Server.CallCenter;
using EDA.Server.CallCenter.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CallCenterDbContext>(connectionName: "callcenter");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChatProjector>();
    x.AddConsumer<DashboardProjector>();
    x.AddConsumer<SuggestionProjector>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var rabbitMqUri = configuration.GetConnectionString("messaging")
            ?? configuration["MESSAGING_URI"];

        if (string.IsNullOrWhiteSpace(rabbitMqUri))
        {
            throw new InvalidOperationException(
                "RabbitMQ connection string not configured. Expecting ConnectionStrings:messaging or MESSAGING_URI.");
        }

        cfg.Host(new Uri(rabbitMqUri));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<TranscriptRecorder>();
builder.Services.AddScoped<OutboxDispatchRunner>();
builder.Services.AddSingleton<DemoSeeder>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DemoSeeder>());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api/demo");

api.MapGet("state", async (CallCenterDbContext db, Guid? callId, CancellationToken ct) =>
{
    var activeCallId = callId ?? DemoIds.ActiveCallId;

    var call = await db.CallSessions
        .AsNoTracking()
        .FirstOrDefaultAsync(session => session.Id == activeCallId, ct);

    var transcripts = await db.TranscriptSegments
        .AsNoTracking()
        .Where(segment => segment.CallId == activeCallId)
        .OrderBy(segment => segment.ReceivedAt)
        .ToListAsync(ct);

    var chatMessages = await db.ChatMessages
        .AsNoTracking()
        .Where(entry => entry.CallId == activeCallId)
        .OrderBy(entry => entry.ReceivedAt)
        .ToListAsync(ct);

    var dashboard = await db.AgentDashboards
        .AsNoTracking()
        .FirstOrDefaultAsync(projection => projection.CallId == activeCallId, ct);

    var suggestions = await db.Suggestions
        .AsNoTracking()
        .Where(entry => entry.CallId == activeCallId)
        .OrderByDescending(entry => entry.CreatedAt)
        .ToListAsync(ct);

    var outboxMessages = await db.OutboxMessages
        .AsNoTracking()
        .Where(message => message.Status == OutboxStatuses.Pending
            || message.Status == OutboxStatuses.Published
            || message.Status == OutboxStatuses.Failed)
        .ToListAsync(ct);

    var outbox = OutboxSnapshot.From(outboxMessages);
    var state = DemoState.From(call, transcripts, chatMessages, dashboard, suggestions, outbox);
    return Results.Ok(state);
});

api.MapPost("transcripts", async (TranscriptRequest request, TranscriptRecorder recorder, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { message = "Transcript text is required." });
    }

    var result = await recorder.RecordAsync(request, ct);
    return result is null
        ? Results.NotFound(new { message = "Call not found." })
        : Results.Ok(result);
});

api.MapPost("reset", async (DemoSeeder seeder, CancellationToken ct) =>
{
    await seeder.SeedAsync(reset: true, ct);
    return Results.NoContent();
});

api.MapPost("outbox/dispatch", async (OutboxDispatchRunner runner, CancellationToken ct) =>
{
    await runner.DispatchPendingAsync(ct);
    return Results.NoContent();
});

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
