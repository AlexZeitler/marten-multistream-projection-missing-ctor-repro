// See https://aka.ms/new-console-template for more information

using Marten;
using Marten.Events;
using Marten.Events.Projections;
using MartenMultiStreamMissingCtorRepro.Features;
using Npgsql;


var connectionString = new NpgsqlConnectionStringBuilder
{
  Pooling = false,
  Port = 5435,
  Host = "localhost",
  CommandTimeout = 20,
  Database = "postgres",
  Password = "123456",
  Username = "postgres"
}.ToString();

var store = DocumentStore.For(
  options =>
  {
    options.Connection(connectionString);
    options.Events.StreamIdentity = StreamIdentity.AsString;
    options.Projections.Add<IncidentProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<IncidentChatMessageProjection>(ProjectionLifecycle.Inline);
  }
);

var streamId = Guid.NewGuid()
  .ToString();

var incidentLogged = new IncidentLogged(
  streamId,
  $"incident-chat-{streamId}",
  "Title",
  "Message",
  "ContactPerson",
  DateTimeOffset.UtcNow
);

await using var session = store.LightweightSession();
session.Events.Append(streamId, incidentLogged);
await session.SaveChangesAsync();
