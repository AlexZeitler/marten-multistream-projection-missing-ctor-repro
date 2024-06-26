using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using MartenMultiStreamMissingCtorRepro.Tests.TestSetup;

namespace MartenMultiStreamMissingCtorRepro.Tests;

public record IncidentChatMessage
{
  public string Id { get; set; }
  public string Context { get; set; }
  public string Text { get; set; }
  public string From { get; set; }
  public DateTimeOffset On { get; set; }
}

public record IncidentLogged(
  string Id,
  string Context,
  string Title,
  string Message,
  string ContactPerson,
  DateTimeOffset ReceivedOn
);

public record RepliedToIncident(
  string Id,
  string Context,
  string Title,
  string Message,
  string ContactPerson,
  DateTimeOffset ReceivedOn
);

public record Incident(
  string Id,
  string Title,
  string Message,
  string ContactPerson,
  DateTimeOffset ReceivedOn
);

public class IncidentProjection : SingleStreamProjection<Incident>
{
  public static Incident Create(
    IncidentLogged incidentLogged
  )
  {
    return new Incident(
      incidentLogged.Id,
      incidentLogged.ContactPerson,
      incidentLogged.Message,
      incidentLogged.ContactPerson,
      incidentLogged.ReceivedOn
    );
  }
}

public class IncidentChatMessageProjection : MultiStreamProjection<IncidentChatMessage, string>
{
  public IncidentChatMessageProjection()
  {
    Identity<IncidentLogged>(x => $"{x.Context}-{Guid.NewGuid()}");
    Identity<RepliedToIncident>(x => $"{x.Context}-{Guid.NewGuid()}");
    IncludeType<IncidentLogged>();
    IncludeType<RepliedToIncident>();
  }

  public IncidentChatMessage Create(
    IncidentLogged incidentLogged
  )
  {
    return new IncidentChatMessage
    {
      Id = $"{incidentLogged.Context}-{Guid.NewGuid()}",
      Context = incidentLogged.ContactPerson,
      From = incidentLogged.ContactPerson,
      On = incidentLogged.ReceivedOn,
      Text = incidentLogged.Message
    };
  }

  public IncidentChatMessage Create(
    RepliedToIncident repliedToIncident
  )
  {
    return new IncidentChatMessage
    {
      Id = $"{repliedToIncident.Context}-{Guid.NewGuid()}",
      Context = repliedToIncident.ContactPerson,
      From = repliedToIncident.ContactPerson,
      On = repliedToIncident.ReceivedOn,
      Text = repliedToIncident.Message
    };
  }
}

[TestFixture]
public class When_incident_is_logged
{
  private TestEventStore _testEventStore;
  private IDocumentStore _store;
  private string _streamId;

  [SetUp]
  public async Task InitializeAsync()
  {
    _testEventStore = await TestEventStore.InitializeAsync(
      options =>
      {
        options.Events.StreamIdentity = StreamIdentity.AsString;
        options.Projections.Add<IncidentProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<IncidentChatMessageProjection>(ProjectionLifecycle.Inline);
      }
    );
    _store = _testEventStore.Store;

    _streamId = Guid.NewGuid()
      .ToString();

    var incidentLogged = new IncidentLogged(
      _streamId,
      $"incident-chat-{_streamId}",
      "Title",
      "Message",
      "ContactPerson",
      DateTimeOffset.UtcNow
    );

    var repliedToIncident = new RepliedToIncident(
      _streamId,
      $"incident-chat-{_streamId}",
      "Reply-Title",
      "Reply-Message",
      "ContactPerson",
      DateTimeOffset.UtcNow
    );

    await using var session = _store.LightweightSession();
    session.Events.Append(
      _streamId,
      incidentLogged,
      repliedToIncident
    );
    await session.SaveChangesAsync();
  }

  [Test]
  public async Task should_create_incident_projection()
  {
    await using var session = _store.QuerySession();
    var incident = await session.LoadAsync<Incident>(_streamId);

    incident.ShouldNotBeNull();
  }


  [Test]
  public async Task should_add_incident_chat_message()
  {
    await using var session = _store.QuerySession();
    var chatMessages = await session.Query<IncidentChatMessage>()
      .ToListAsync();
    chatMessages.Count.ShouldBe(2);
  }

  [TearDown]
  public async Task DisposeAsync()
  {
    // await _testEventStore.DisposeAsync();
  }
}
