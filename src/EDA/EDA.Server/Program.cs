using EDA.Server.CallCenter;
using EDA.Server.CallCenter.Consumers;
using EDA.Server.CallCenter.Contracts;
using EDA.Server.CallCenter.Sagas;
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
    x.AddSagaStateMachine<CallResolutionSaga, CallResolutionState>()
        .InMemoryRepository();

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
builder.Services.AddSingleton<DemoSeeder>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DemoSeeder>());
builder.Services.AddHostedService<OutboxDispatcher>();

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

var saga = api.MapGroup("saga");

saga.MapPost("start", async (
    CallStartRequest request,
    CallCenterDbContext db,
    IPublishEndpoint publishEndpoint,
    CancellationToken ct) =>
{
    var callId = request.CallId ?? DemoIds.ActiveCallId;
    var call = await db.CallSessions
        .AsNoTracking()
        .FirstOrDefaultAsync(session => session.Id == callId, ct);

    if (call is null)
    {
        return Results.NotFound(new { message = "Call not found." });
    }

    var started = new CallStarted(call.Id, call.AgentName, call.CallerName, call.StartedAt);
    await publishEndpoint.Publish(started, ct);
    return Results.Ok(started);
});

saga.MapPost("stream", async (
    TranscriptStreamRequest request,
    CallCenterDbContext db,
    IPublishEndpoint publishEndpoint,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { message = "Transcript text is required." });
    }

    var callId = request.CallId ?? DemoIds.ActiveCallId;
    var callExists = await db.CallSessions
        .AsNoTracking()
        .AnyAsync(session => session.Id == callId, ct);

    if (!callExists)
    {
        return Results.NotFound(new { message = "Call not found." });
    }

    var speaker = string.IsNullOrWhiteSpace(request.Speaker) ? "Caller" : request.Speaker.Trim();
    var text = request.Text.Trim();
    var streaming = new TranscriptStreaming(callId, Guid.NewGuid(), speaker, text, DateTimeOffset.UtcNow);

    await publishEndpoint.Publish(streaming, ct);
    return Results.Ok(streaming);
});

saga.MapPost("hold", async (
    CallHoldRequest request,
    CallCenterDbContext db,
    IPublishEndpoint publishEndpoint,
    CancellationToken ct) =>
{
    var callId = request.CallId ?? DemoIds.ActiveCallId;
    var callExists = await db.CallSessions
        .AsNoTracking()
        .AnyAsync(session => session.Id == callId, ct);

    if (!callExists)
    {
        return Results.NotFound(new { message = "Call not found." });
    }

    var reason = string.IsNullOrWhiteSpace(request.Reason)
        ? "Caller placed on hold."
        : request.Reason.Trim();
    var held = new CallHeld(callId, DateTimeOffset.UtcNow, reason);

    await publishEndpoint.Publish(held, ct);
    return Results.Ok(held);
});

saga.MapPost("resume", async (
    CallResumeRequest request,
    CallCenterDbContext db,
    IPublishEndpoint publishEndpoint,
    CancellationToken ct) =>
{
    var callId = request.CallId ?? DemoIds.ActiveCallId;
    var callExists = await db.CallSessions
        .AsNoTracking()
        .AnyAsync(session => session.Id == callId, ct);

    if (!callExists)
    {
        return Results.NotFound(new { message = "Call not found." });
    }

    var resumed = new CallResumed(callId, DateTimeOffset.UtcNow);

    await publishEndpoint.Publish(resumed, ct);
    return Results.Ok(resumed);
});

saga.MapPost("hangup", async (
    CallHangupRequest request,
    CallCenterDbContext db,
    IPublishEndpoint publishEndpoint,
    CancellationToken ct) =>
{
    var callId = request.CallId ?? DemoIds.ActiveCallId;
    var callExists = await db.CallSessions
        .AsNoTracking()
        .AnyAsync(session => session.Id == callId, ct);

    if (!callExists)
    {
        return Results.NotFound(new { message = "Call not found." });
    }

    var reason = string.IsNullOrWhiteSpace(request.Reason)
        ? "Caller ended the call."
        : request.Reason.Trim();
    var ended = new CallEnded(callId, DateTimeOffset.UtcNow, reason);

    await publishEndpoint.Publish(ended, ct);
    return Results.Ok(ended);
});

api.MapPost("reset", async (DemoSeeder seeder, CancellationToken ct) =>
{
    await seeder.SeedAsync(reset: true, ct);
    return Results.NoContent();
});

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
