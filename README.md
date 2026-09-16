# Mini e-shop

Навчальний проєкт для лабораторних робіт з предмету **"Технології DevOps"**. Спрощений інтернет-магазин, побудований як набір незалежних сервісів (мікросервісів), щоб послідовно розкривати теми курсу: контейнеризація, оркестрація, CI/CD.

## Архітектура

```
                    ┌─────────────┐
        клієнт ───▶ │   Gateway   │  (YARP reverse proxy, :8080)
                    └──────┬──────┘
              ┌────────────┴────────────┐
              ▼                         ▼
      ┌───────────────┐         ┌───────────────┐
      │  Catalog.Api   │         │  Orders.Api    │
      │    (:8081)     │         │    (:8082)     │
      └───┬───────┬────┘         └───────┬────────┘
          │       │                      │
          ▼       ▼                      ▼
     PostgreSQL  Redis              PostgreSQL
    (catalog_db) (кеш)             (orders_db)
          ▲                              │
          │         RabbitMQ             │
          └────── "orders" exchange ◀────┘
```

- **Catalog.Api** — каталог товарів. Список товарів кешується в Redis (TTL 1 хв). Слухає чергу RabbitMQ `catalog.order-created` і зменшує залишки (`stock`) після кожного нового замовлення.
- **Orders.Api** — створення та перегляд замовлень. При створенні замовлення публікує подію `OrderCreatedEvent` у fanout-exchange `orders`.
- **Gateway** — єдина точка входу (YARP). `/catalog/**` → Catalog.Api, `/orders/**` → Orders.Api.
- **Shared.Contracts** — спільна бібліотека з DTO подій (`OrderCreatedEvent`), на яку посилаються обидва сервіси.

Кожен сервіс має власну базу даних (database-per-service) і власний `Dockerfile`.

## Технологічний стек

- **Backend:** ASP.NET Core Web API (.NET 10)
- **База даних:** PostgreSQL (через Npgsql.EntityFrameworkCore.PostgreSQL), окрема БД на сервіс
- **Кеш:** Redis (StackExchangeRedis)
- **Брокер повідомлень:** RabbitMQ (RabbitMQ.Client 7, async API)
- **Gateway:** YARP (Yarp.ReverseProxy)
- **Тести:** xUnit, EF Core InMemory provider, `WebApplicationFactory` для інтеграційних тестів
- **Контейнеризація:** multi-stage Dockerfile на сервіс + docker-compose

## Структура проєкту

```
mini-eshop/
├── src/
│   ├── Catalog.Api/        # каталог товарів (REST + Redis-кеш + консюмер RabbitMQ)
│   ├── Orders.Api/         # замовлення (REST + продюсер RabbitMQ)
│   ├── Gateway/             # YARP reverse proxy
│   └── Shared.Contracts/    # спільні DTO подій
├── tests/
│   ├── Catalog.Api.Tests/   # юніт + інтеграційні тести Catalog.Api
│   └── Orders.Api.Tests/    # юніт + інтеграційні тести Orders.Api
├── infra/postgres/          # init-скрипт для створення кількох БД в одному Postgres
├── docker-compose.yml
└── MiniEshop.slnx
```

## Запуск через Docker Compose

Вимагає Docker Desktop.

```bash
docker compose up -d --build
```

Після старту (Postgres/Redis/RabbitMQ мають пройти healthcheck, це займає кілька секунд):

| Сервіс | Адреса |
|---|---|
| Gateway (єдина точка входу) | http://localhost:8080 |
| Catalog.Api напряму | http://localhost:8081 |
| Orders.Api напряму | http://localhost:8082 |
| RabbitMQ Management UI | http://localhost:15672 (guest/guest) |
| PostgreSQL | localhost:5432 (postgres/postgres) |

Зупинити: `docker compose down` (додати `-v`, щоб видалити дані Postgres).

## API

**Каталог** (`/catalog/api/products` через gateway, або напряму `:8081/api/products`)

- `GET /api/products` — список товарів (кешується в Redis)
- `GET /api/products/{id}` — товар за id
- `POST /api/products` — створити товар

**Замовлення** (`/orders/api/orders` через gateway, або напряму `:8082/api/orders`)

- `GET /api/orders` — список замовлень
- `GET /api/orders/{id}` — замовлення за id
- `POST /api/orders` — створити замовлення (тіло: `{"items":[{"productId":"...","quantity":2}]}`).
  Публікує подію в RabbitMQ, яку забирає Catalog.Api і зменшує stock.

## Локальний запуск без Docker

Потрібні локальні PostgreSQL, Redis, RabbitMQ (або застосуй `docker compose up postgres redis rabbitmq`), далі:

```bash
dotnet run --project src/Catalog.Api
dotnet run --project src/Orders.Api
dotnet run --project src/Gateway
```

Конфігурація підключень — у `appsettings.json` кожного сервісу (`ConnectionStrings`, `Redis:Connection`, `RabbitMQ:Host`), для docker-compose вони перевизначаються через змінні середовища.

## Тести

```bash
dotnet test
```

Юніт-тести бізнес-логіки використовують EF Core InMemory provider і не потребують запущених контейнерів. Інтеграційні тести піднімають застосунок через `WebApplicationFactory`, підміняючи БД на InMemory, Redis-кеш — на in-memory реалізацію, а publisher/consumer RabbitMQ — на фейк, тому теж працюють без Docker.

## Відомі обмеження

- Схема БД створюється через `EnsureCreated()`, а не EF Core-міграції (достатньо для лабораторної, але не для продакшену).
- `Microsoft.AspNetCore.OpenApi` 10.0.6 тягне вразливу транзитивну залежність `Microsoft.OpenApi` 2.0.0 (NU1903, ReDoS у парсингу). Оновлення до патченої 3.x ламає сумісність з генератором ASP.NET Core (breaking change API). Не є проблемою на практиці — пакет використовується лише для генерації `/openapi` документації, а не для обробки недовіреного вводу.
