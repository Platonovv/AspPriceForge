# PriceForge

Бэкенд-сервис расчёта цен на товары с иерархическим составом (BOM). Учебно-боевой pet-проект на ASP.NET Core 8: чистая архитектура, CQRS, доменные паттерны ценообразования, полная обвязка надёжности и наблюдаемости, тесты и инфраструктура (Docker / Kubernetes / CI).

## Стек

- **.NET 8 / ASP.NET Core**, Clean Architecture (Domain / Application / Infrastructure / Api)
- **PostgreSQL** + EF Core 8 (closure table для дерева компонентов, recursive CTE), **Dapper** для отчётов
- **CQRS** через MediatR, валидация FluentValidation, pipeline behaviors (валидация/лог/кеш)
- **Redis** — кеш расчётов + backplane для SignalR
- **RabbitMQ** — события через Outbox pattern; **MongoDB** — event store
- **JWT** access + refresh токены, политики авторизации
- **Serilog** (+ ELK), **OpenTelemetry** → Jaeger / Prometheus / Grafana, health checks
- Уведомления: SignalR (realtime), Email (SMTP/MailKit), Push (FCM) через fan-out `INotificationChannel`
- Надёжность: транзакции/изоляция, идемпотентность (Idempotency-Key), Polly (retry/CB/timeout)
- Тесты: xUnit (unit), Testcontainers (integration), WebApplicationFactory (E2E)
- Инфра: multi-stage Docker, docker-compose, Kubernetes (Deployment/Service/ConfigMap/Secret, миграции через EF bundle Job, scale-out + Redis backplane), GitHub Actions CI

## Быстрый старт (Docker Compose)

```bash
# 1. Скопировать шаблон конфигурации и заполнить значения
cp .env.example .env
#    (задать DB_PASSWORD, JWT_SECRET и при необходимости Smtp__*/Firebase)

# 2. Поднять всё приложение + инфраструктуру
docker compose up -d --build

# 3. API доступен на http://localhost:8080
#    Health: http://localhost:8080/health/ready
```

Сервисы после старта: API (8080), PostgreSQL (5432), Redis (6379), RabbitMQ (5672 / UI 15672), MongoDB (27017), Elasticsearch (9200) + Kibana (5601), Jaeger (16686), Prometheus (9090), Grafana (3000).

## Тесты

```bash
dotnet test                              # все тесты
dotnet test --filter Category=Integration  # только интеграционные (нужен Docker — Testcontainers)
```

## Kubernetes (локально)

Манифесты — в `k8s/`. Развёртывание в локальный кластер (Docker Desktop / minikube / kind):

```bash
kubectl apply -f k8s/configmap.yaml -f k8s/secret.yaml
kubectl apply -f k8s/postgres.yaml -f k8s/redis.yaml -f k8s/rabbitmq.yaml -f k8s/mongo.yaml
kubectl apply -f k8s/migrate-job.yaml   # миграции (EF bundle), один раз
kubectl apply -f k8s/api.yaml           # API: 3 реплики + Service (LoadBalancer)
```

> `k8s/secret.yaml` не входит в репозиторий (содержит секреты). Создаётся локально по образцу остальных манифестов / через `kubectl create secret`.

## Структура

```
src/
  PriceForge.Domain          — сущности, value objects, доменные инварианты
  PriceForge.Application      — CQRS (команды/запросы), behaviors, фоновые сервисы
  PriceForge.Infrastructure   — EF Core, репозитории, интеграции (Redis/Rabbit/Mongo/SMTP/FCM)
  PriceForge.Api              — контроллеры, middleware, SignalR, composition root
tests/                        — Unit / Integration тесты
k8s/                          — манифесты Kubernetes
```

## Конфигурация

Все настройки — через переменные окружения (12-factor). Шаблон со списком ключей: [`.env.example`](.env.example). Секреты (пароли, JWT-ключ, SMTP, Firebase) в репозиторий не коммитятся.
