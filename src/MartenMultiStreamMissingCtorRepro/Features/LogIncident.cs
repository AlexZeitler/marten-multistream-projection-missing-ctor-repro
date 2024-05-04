using Marten.Events.Aggregation;
using Marten.Events.Projections;

namespace MartenMultiStreamMissingCtorRepro.Features;


public record IncidentChatMessage
{
  public Guid Id { get; set; }
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
    Identity<IncidentLogged>(x => $"{x.Context}-{x.ReceivedOn.ToUnixTimeMilliseconds()}");
    IncludeType<IncidentLogged>();
  }

  public IncidentChatMessage Create(
    IncidentLogged incidentLogged
  )
  {
    return new IncidentChatMessage
    {
      Context = incidentLogged.ContactPerson,
      From = incidentLogged.ContactPerson,
      On = incidentLogged.ReceivedOn,
      Text = incidentLogged.Message
    };
  }
}
