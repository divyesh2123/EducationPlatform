using EventBus.Abstractions;

namespace EventBus.Events;

public record CourseCreatedIntegrationEvent(int CourseId, string Code, string Title, string Instructor, int Credits, int Capacity) : IntegrationEvent;

public record CourseUpdatedIntegrationEvent(int CourseId, string Code, string Title, string Instructor, int Credits, int Capacity) : IntegrationEvent;

public record CourseDeletedIntegrationEvent(int CourseId) : IntegrationEvent;
