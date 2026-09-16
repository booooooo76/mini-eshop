# Mini e-shop

Навчальний проєкт для лабораторних робіт з предмету **"Технології DevOps"**. Спрощений інтернет-магазин, побудований як набір незалежних сервісів (мікросервісів), щоб послідовно розкривати теми курсу: контейнеризація, оркестрація, CI/CD.

## Архітектура

```
                            ┌─────────────┐
                клієнт ───▶ │   Gateway   │  (YARP reverse proxy, :8080)
                            │  + агрегований Swagger  │
                            └──────┬──────┘
        ┌───────────────┬──────────┴───────┬───────────────────┐
        ▼                ▼                  ▼                   ▼
┌───────────────┐ ┌───────────────┐ ┌───────────────────┐ ┌───────────────┐
│ Catalog.Api    │ │ Orders.Api     │ │ Notifications.Api  │ │ Reviews.Api    │
│   (:8081)      │ │   (:8082)      │ │    (:8083)          │ │   (:8084)      │
└───┬───────┬────┘ └───────┬────────┘ └──────────┬──────────┘ └───────┬───────┘
    │       │              │                     │                    │
    ▼       ▼              ▼                     ▼                    ▼
PostgreSQL Redis      PostgreSQL              PostgreSQL           PostgreSQL
(catalog_db)(кеш)    (orders_db)         (notifications_db)      (reviews_db)
    ▲                      │                     ▲
    │        RabbitMQ "orders" fanout exchange    │
    └──────────────────────┴─────────────────────┘
```

- **Catalog.Api** — каталог товарів. Список товарів кешується в Redis (TTL 1 хв). Слухає чергу `catalog.order-created` і зменшує залишки (`stock`) після кожного нового замовлення.
- **Orders.Api** — створення та перегляд замовлень. При створенні замовлення публікує подію `OrderCreatedEvent` у fanout-exchange `orders`.
- **Notifications.Api** — незалежний консюмер тієї ж події (`notifications.order-created`): записує "сповіщення" в свою БД і віддає історію через REST. Показує, що один exchange може мати кілька незалежних підписників.
- **Reviews.Api** — окремий bounded context без месенджингу: CRUD відгуків на товари (ProductId, Rating 1-5, Comment). Демонструє, що не кожен мікросервіс мусить бути event-driven.
- **Gateway** — єдина точка входу (YARP) + агрегований Swagger UI з перемикачем між усіма чотирма сервісами.
- **Shared.Contracts** — спільна бібліотека з DTO подій (`OrderCreatedEvent`), на яку посилаються Catalog.Api, Orders.Api і Notifications.Api.

Кожен сервіс має власну базу даних (database-per-service) і власний `Dockerfile`.

## Технологічний стек

- **Backend:** ASP.NET Core Web API (.NET 10)
- **База даних:** PostgreSQL (через Npgsql.EntityFrameworkCore.PostgreSQL), окрема БД на сервіс
- **Кеш:** Redis (StackExchangeRedis)
- **Брокер повідомлень:** RabbitMQ (RabbitMQ.Client 7, async API)
- **Gateway:** YARP (Yarp.ReverseProxy)
- **Документація:** Microsoft.AspNetCore.OpenApi + Swashbuckle.AspNetCore.SwaggerUI, з агрегацією всіх 4 визначень на Gateway
- **Тести:** xUnit, EF Core InMemory provider, `WebApplicationFactory` для інтеграційних тестів
- **Контейнеризація:** multi-stage Dockerfile на сервіс + docker-compose

## Структура проєкту

```
mini-eshop/
├── src/
│   ├── Catalog.Api/          # каталог товарів (REST + Redis-кеш + консюмер RabbitMQ)
│   ├── Orders.Api/           # замовлення (REST + продюсер RabbitMQ)
│   ├── Notifications.Api/    # консюмер подій замовлень + REST-історія сповіщень
│   ├── Reviews.Api/          # відгуки на товари (проста REST CRUD, без месенджингу)
│   ├── Gateway/              # YARP reverse proxy + агрегований Swagger
│   └── Shared.Contracts/     # спільні DTO подій
├── tests/                    # юніт + інтеграційні тести на кожен сервіс
├── infra/postgres/           # init-скрипт для створення кількох БД в одному Postgres
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
| **Агрегований Swagger (усі 4 сервіси)** | **http://localhost:8080/swagger** |
| Catalog.Api напряму | http://localhost:8081 |
| Orders.Api напряму | http://localhost:8082 |
| Notifications.Api напряму | http://localhost:8083 |
| Reviews.Api напряму | http://localhost:8084 |
| RabbitMQ Management UI | http://localhost:15672 (guest/guest) |
| PostgreSQL | localhost:5433 (postgres/postgres; порт 5433, а не стандартний 5432 — див. "Відомі обмеження") |

Зупинити: `docker compose down` (додати `-v`, щоб видалити дані Postgres).

## Swagger — перемикач між мікросервісами

На `http://localhost:8080/swagger` (Gateway) є випадаючий список **"Select a definition"** у верхньому правому куті — дозволяє перемикатись між Catalog.Api / Orders.Api / Notifications.Api / Reviews.Api, не відкриваючи чотири окремі вкладки. Технічно це стандартний Swagger UI з кількома зареєстрованими `SwaggerEndpoint`, що вказують напряму на порт кожного сервіса (список — у `src/Gateway/appsettings.json`, секція `SwaggerAggregator`). "Try it out" при цьому б'є напряму в сервіс (обходячи Gateway-проксі), тому працює коректно для кожного визначення незалежно від того, з якої вкладки його відкрито.

У кожного сервіса є й власний Swagger на своєму порту (наприклад, http://localhost:8081/swagger) — з тим самим переліком, просто зручніше дивитись усе з одного місця через Gateway.

## API

| Сервіс | Префікс через Gateway | Напряму |
|---|---|---|
| Catalog.Api | `/catalog/api/products` | `:8081/api/products` |
| Orders.Api | `/orders/api/orders` | `:8082/api/orders` |
| Notifications.Api | `/notifications/api/notifications` | `:8083/api/notifications` |
| Reviews.Api | `/reviews/api/reviews` | `:8084/api/reviews` |

- **Catalog:** `GET /api/products`, `GET /api/products/{id}`, `POST /api/products`
- **Orders:** `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders` (тіло: `{"items":[{"productId":"...","quantity":2}]}`) — публікує подію, яку забирають Catalog і Notifications
- **Notifications:** `GET /api/notifications`, `GET /api/notifications/{id}` (лише читання — записи створюються консюмером)
- **Reviews:** `GET /api/reviews`, `GET /api/reviews/product/{productId}`, `POST /api/reviews` (тіло: `{"productId":"...","rating":1-5,"comment":"..."}`)

## Локальний запуск без Docker

Потрібні локальні PostgreSQL, Redis, RabbitMQ (або `docker compose up postgres redis rabbitmq`), далі в окремих терміналах:

```bash
dotnet run --project src/Catalog.Api
dotnet run --project src/Orders.Api
dotnet run --project src/Notifications.Api
dotnet run --project src/Reviews.Api
dotnet run --project src/Gateway
```

Кожен сервіс має свій Swagger на своєму `launchSettings`-порту (`.../swagger`). Конфігурація підключень — у `appsettings.json` кожного сервісу, для docker-compose вона перевизначається через змінні середовища.

## Тести

```bash
dotnet test
```

Юніт-тести бізнес-логіки використовують EF Core InMemory provider і не потребують запущених контейнерів. Інтеграційні тести піднімають застосунок через `WebApplicationFactory`, підміняючи БД на InMemory, Redis-кеш — на in-memory реалізацію, а publisher/consumer RabbitMQ — на фейк, тому теж працюють без Docker.

## Відомі обмеження

- Схема БД створюється через `EnsureCreated()`, а не EF Core-міграції (достатньо для лабораторної, але не для продакшену).
- `Microsoft.AspNetCore.OpenApi` 10.0.6 тягне вразливу транзитивну залежність `Microsoft.OpenApi` 2.0.0 (NU1903, ReDoS у парсингу). Оновлення до патченої 3.x ламає сумісність з генератором ASP.NET Core (breaking change API). Не є проблемою на практиці — пакет використовується лише для генерації `/openapi` документації, а не для обробки недовіреного вводу.
- CORS на всіх чотирьох API дозволений з будь-якого origin (`AllowAnyOrigin`) — потрібно, щоб агрегований Swagger на Gateway міг підвантажити `/openapi/v1.json` з інших портів. Прийнятно для локальної лабораторної роботи, не для продакшену.
- Postgres опублікований на хостовому порту **5433**, а не стандартному 5432 — на машині розробника вже може працювати нативний (не-Docker) Postgres-сервер на 5432, і Windows непередбачувано маршрутизує з'єднання між ними.
- `ASPNETCORE_ENVIRONMENT=Development` явно виставлений у docker-compose для всіх сервісів, щоб Swagger UI був доступний і в контейнерах (за замовчуванням .NET-контейнери піднімаються як Production, де Swagger вимкнено).
