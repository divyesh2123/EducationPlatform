using EventBus.Abstractions;

namespace EventBus.Events;

// Shared contracts: the message schema between services. Each service only shares these, never its entities or DbContext.

public record StudentCreatedIntegrationEvent(int StudentId, string FirstName, string LastName, string Email) : IntegrationEvent;

public record StudentUpdatedIntegrationEvent(int StudentId, string FirstName, string LastName, string Email) : IntegrationEvent;

public record StudentDeletedIntegrationEvent(int StudentId) : IntegrationEvent;
