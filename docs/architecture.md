# Архитектура

## Слои (Clean Architecture)

Зависимости текут только внутрь, к `Domain`. `Domain` не зависит ни от чего; внешние слои зависят от внутренних.

```mermaid
flowchart LR
    Api -->|depends on| Application
    Api -->|wires up DI| Infrastructure
    Infrastructure -->|implements interfaces| Application
    Application -->|depends on| Domain
    Infrastructure -->|uses types| Domain

    classDef core fill:#2d6,stroke:#093,color:#000
    class Domain core
```

- **Domain** — сущности, value objects, инварианты (ни от чего не зависит).
- **Application** — use cases (CQRS) + интерфейсы инфраструктуры.
- **Infrastructure** — реализации (EF / Redis / RabbitMQ / SMTP / JWT).
- **Api** — HTTP, middleware, composition root.

## Поток запроса (создание товара)

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as Controller
    participant M as MediatR (pipeline)
    participant H as CreateProductHandler
    participant DB as PostgreSQL

    C->>Ctrl: POST /api/v1/products (+ JWT, Idempotency-Key)
    Ctrl->>M: Send(CreateProductCommand)
    M->>M: Validation → Logging → Caching (behaviors)
    M->>H: handle
    H->>DB: продукт + outbox-строка (одна транзакция)
    H-->>Ctrl: id
    Ctrl-->>C: 201 Created
```

## Публикация событий (Outbox)

```mermaid
flowchart LR
    H[Handler] -->|продукт + outbox, 1 транзакция| PG[(PostgreSQL)]
    D[OutboxDispatcher] -->|poll WHERE published_at IS NULL| PG
    D -->|publish| MQ{{RabbitMQ}}
    MQ --> CO[Consumer]
    CO -->|идемпотентно| MG[(MongoDB)]
```

Гарантия доставки — at-least-once; консьюмер идемпотентен (дедуп по ключу).
