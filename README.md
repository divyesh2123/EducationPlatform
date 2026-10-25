# Education Platform – Event-Driven Microservices (.NET 9 + RabbitMQ + Ocelot)

A learning project showing an event-driven microservice architecture for an **education domain**:

- **.NET 9** ASP.NET Core Web APIs
- **RabbitMQ** as the message broker (custom event bus on `RabbitMQ.Client`)
- **Ocelot** API Gateway (routing, aggregation, rate limiting)
- **EF Core code-first** with **SQL Server LocalDB**, one database per service

## Architecture

```
                        ┌───────────────────────────┐
     Client ──HTTP──▶   │  API Gateway (Ocelot)     │  http://localhost:5000
                        └───────┬───────┬───────┬───┴──────────┐
                                │       │       │              │
                   ┌────────────▼┐ ┌────▼──────┐ ┌▼────────────┐ ┌▼───────────────┐
                   │ Student     │ │ Course    │ │ Enrollment  │ │ Notification   │
                   │ :5101       │ │ :5102     │ │ :5103       │ │ :5104          │
                   │ StudentDb   │ │ CourseDb  │ │ EnrollmentDb│ │ NotificationDb │
                   └──────┬──────┘ └────┬──────┘ └──▲───────┬──┘ └──────▲─────────┘
                  publish │     publish │   consume │       │ publish   │ consume
                          ▼             ▼           │       ▼           │
                ┌───────────────────────────────────┴───────────────────┴───┐
                │  RabbitMQ  exchange: education_event_bus (direct)         │
                │  queues:   enrollment_service, notification_service (+DLQ)│
                └───────────────────────────────────────────────────────────┘
```

| Service | Responsibility | Publishes | Consumes |
|---|---|---|---|
| **StudentService.API** | Student CRUD | `StudentCreated`, `StudentUpdated`, `StudentDeleted` | – |
| **CourseService.API** | Course CRUD | `CourseCreated`, `CourseUpdated`, `CourseDeleted` | – |
| **EnrollmentService.API** | Enroll / cancel, capacity & duplicate rules | `StudentEnrolled`, `EnrollmentCancelled` | all Student & Course events |
| **NotificationService.API** | Stores/"sends" emails | – | `StudentCreated`, `CourseCreated`, `StudentEnrolled`, `EnrollmentCancelled` |
| **ApiGateway** | Single entry point (Ocelot) | – | – |

### Event-driven concepts demonstrated

1. **Database per service** – each service owns its own LocalDB database; no shared tables.
2. **Local read models (data replication)** – Enrollment service keeps its own `Students` and `Courses` tables, filled from events. Enrolling does **not** call Student/Course services over HTTP, so it keeps working if they are down.
3. **Eventual consistency** – after creating a student there is a short delay before Enrollment knows about it. See `GET /api/enrollments/read-models/students`.
4. **Choreography** – deleting a student triggers a chain with no central coordinator:
   `StudentDeleted` → Enrollment cancels active enrollments → `EnrollmentCancelled` → Notification emails the student.
5. **Idempotent consumers** – Enrollment handlers upsert; Notification stores the event `Id` with a unique index so redelivered messages are skipped.
6. **Reliable delivery** – durable exchange/queues, persistent messages, manual `ack`, `prefetch=10`; failed messages are `nack`ed to a **dead-letter queue** (`<queue>.dlq`).
7. **Connection resilience** – exponential back-off retry while connecting to RabbitMQ.

## Project structure

```
EducationPlatform.sln
docker-compose.yml                 # RabbitMQ (optional – if you don't already have it installed)
EducationPlatform.http             # Ready-made requests through the gateway
src/
  ApiGateway/                      # Ocelot – ocelot.json
  BuildingBlocks/EventBus/
    Abstractions/                  # IntegrationEvent, IEventBus, IIntegrationEventHandler<T>
    Events/                        # Shared event contracts (the only thing services share)
    RabbitMQ/                      # Connection, publisher + consumer (BackgroundService)
    Extensions/                    # AddRabbitMQEventBus().AddSubscription<TEvent, THandler>()
  Services/
    StudentService.API/            # Models, Data (DbContext + Migrations), DTOs, Controllers
    CourseService.API/
    EnrollmentService.API/         # + IntegrationEvents/ (handlers) and read models
    NotificationService.API/       # + IntegrationEvents/ and Services/NotificationSender
```

## Prerequisites

- .NET 9 SDK
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, installed with Visual Studio)
- RabbitMQ on `localhost:5672` (guest/guest). Either a local install, or:
  ```bash
  docker compose up -d       # Management UI: http://localhost:15672
  ```

## Run

Databases are created automatically on start-up from the EF Core migrations (`Database.Migrate()`).

**Visual Studio:** right-click the solution → *Configure Startup Projects* → *Multiple startup projects* → set all 5 projects to *Start*.

**CLI** (one terminal each):
```bash
dotnet run --project src/Services/StudentService.API
dotnet run --project src/Services/CourseService.API
dotnet run --project src/Services/EnrollmentService.API
dotnet run --project src/Services/NotificationService.API
dotnet run --project src/ApiGateway
```

| | URL |
|---|---|
| Gateway | http://localhost:5000 |
| Student Swagger | http://localhost:5101/swagger |
| Course Swagger | http://localhost:5102/swagger |
| Enrollment Swagger | http://localhost:5103/swagger |
| Notification Swagger | http://localhost:5104/swagger |
| RabbitMQ UI | http://localhost:15672 |

Then run the requests in `EducationPlatform.http` (VS / VS Code REST Client / Rider) in order.

## Gateway routes (ocelot.json)

| Upstream (gateway) | Downstream |
|---|---|
| `/api/students`, `/api/students/{id}` | Student :5101 |
| `/api/courses`, `/api/courses/{everything}` | Course :5102 |
| `/api/enrollments`, `/api/enrollments/{everything}` | Enrollment :5103 (rate-limited: 20 req / 10s) |
| `/api/notifications`, `/api/notifications/{everything}` | Notification :5104 |
| `/api/dashboard/students/{id}` | **Aggregate** of student + student enrollments |

## Code-first migrations

```bash
dotnet tool restore
# after changing a model, e.g. in StudentService:
dotnet ef migrations add AddStudentAddress --project src/Services/StudentService.API --output-dir Data/Migrations
# applied automatically on next start, or manually:
dotnet ef database update --project src/Services/StudentService.API
```

## Adding a new event (walkthrough)

1. Add a record in `BuildingBlocks/EventBus/Events`, deriving from `IntegrationEvent`.
2. Publish it: `eventBus.Publish(new MyEvent(...))` after `SaveChangesAsync()`.
3. In the consuming service, implement `IIntegrationEventHandler<MyEvent>`.
4. Register: `.AddSubscription<MyEvent, MyEventHandler>()` in `Program.cs`. The queue binding is created automatically.

## Known simplifications (next steps for production)

- **Dual write:** the DB save and the publish are two separate steps. If RabbitMQ is down right after `SaveChanges`, the event is lost. The production fix is the **Transactional Outbox** pattern (or MassTransit's outbox).
- Messages in a DLQ are not retried automatically; inspect/re-publish them from the RabbitMQ UI.
- No authentication – add JWT validation in Ocelot (`AuthenticationOptions`) and the services.
- No service discovery – downstream hosts are static in `ocelot.json` (Consul/Kubernetes could replace them).
