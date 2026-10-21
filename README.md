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
- **Оркестрація:** Kubernetes (minikube), маніфести в `k8s/`
- **Пакування:** Helm 3, чарт у `helm/mini-eshop/`

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
├── k8s/                      # маніфести Kubernetes (по одному файлу на об'єкт)
├── helm/mini-eshop/          # Helm-чарт: ті самі об'єкти у вигляді шаблонів
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

- **Catalog:**
  - `GET /api/products`, `GET /api/products/{id}`
  - `POST /api/products` — тіло: `{"name":"...","description":"...","price":9.99,"stock":10}`
  - `PUT /api/products/{id}` — повне оновлення (`UpdateProductRequest`), 404 якщо не знайдено
  - `DELETE /api/products/{id}` — 204/404
  - 400 при від'ємній ціні/залишку чи порожній назві (і на створенні, і на оновленні)
- **Orders:**
  - `GET /api/orders`, `GET /api/orders/{id}`
  - `POST /api/orders` — тіло: `{"items":[{"productId":"...","quantity":2}]}` — публікує подію, яку забирають Catalog і Notifications
  - `PUT /api/orders/{id}/status` — тіло: `{"status":0|1|2}` (`Pending`/`Confirmed`/`Cancelled`), 404 якщо не знайдено
  - `DELETE /api/orders/{id}` — 204/404 (каскадно видаляє items)
  - 400 при `quantity <= 0` чи порожньому `productId`
- **Notifications:**
  - `GET /api/notifications`, `GET /api/notifications/{id}` (записи створюються лише консюмером RabbitMQ)
  - `PATCH /api/notifications/{id}/read` — позначити прочитаним, 204/404
  - `DELETE /api/notifications/{id}` — 204/404
- **Reviews:**
  - `GET /api/reviews`, `GET /api/reviews/product/{productId}`, `GET /api/reviews/product/{productId}/summary` (кількість + середній рейтинг)
  - `POST /api/reviews` — тіло: `{"productId":"...","rating":1-5,"comment":"..."}`
  - `PUT /api/reviews/{id}` — тіло: `{"rating":1-5,"comment":"..."}` (тільки rating/comment, productId незмінний), 404 якщо не знайдено
  - `DELETE /api/reviews/{id}` — 204/404
  - 400 при рейтингу поза 1-5 чи порожньому productId (на створенні)

Кожен ендпоінт зі вхідними даними валідує їх на рівні сервісу й повертає `400 Bad Request` з описом помилки замість необробленого винятку. `GET /health` на кожному сервісі реально перевіряє з'єднання з його БД (`AddDbContextCheck`), а не просто повертає статичний "healthy".

## Тестові дані (seed)

Усі чотири сервіси стартують із заздалегідь заповненими даними (через EF Core `HasData`, застосовується при `EnsureCreated()`), узгодженими між собою за Guid — зручно одразу тицяти в Swagger, не створюючи все руками:

| Сервіс | Дані |
|---|---|
| Catalog | 3 товари: Keyboard, Mouse, Monitor |
| Orders | 2 замовлення (Confirmed, Cancelled), прив'язані до Keyboard/Mouse |
| Notifications | 2 сповіщення, по одному на кожне seed-замовлення (одне вже `isRead: true`) |
| Reviews | 3 відгуки: 2 на Keyboard (rating 5 і 3), 1 на Mouse (rating 4) |

Ці Guid — фіксовані константи в `OnModelCreating` кожного `DbContext`, а не `Guid.NewGuid()` (EF Core вимагає статичні значення для seed-даних). Якщо вже піднімав стек раніше зі старим volume Postgres — `EnsureCreated()` не застосує нову схему/seed до наявної БД, тому після оновлення знадобиться `docker compose down -v` (видаляє том Postgres) перед повторним `up`.

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

## CI/CD

![CI](https://github.com/booooooo76/mini-eshop/actions/workflows/ci.yml/badge.svg)

Пайплайн GitHub Actions ([.github/workflows/ci.yml](.github/workflows/ci.yml)) запускається на кожен Pull Request у `master`, на push у `master` і на push git-тегу виду `vX.Y.Z`:

| Job | Що робить |
|---|---|
| `lint` | `dotnet format` (стиль за `.editorconfig`) і збірка з аналізаторами Roslyn, де будь-яке попередження вважається помилкою |
| `build-test` | Для кожного з 5 сервісів паралельно й незалежно: restore, build, test (кеш NuGet) |
| `images` | Після успіху двох попередніх: збірка Docker-образу кожного сервісу (без публікації) і сканування Trivy, що блокує знайдені HIGH/CRITICAL вразливості з наявним виправленням |
| `publish` | Лише на push (не на Pull Request): публікація образів у GitHub Container Registry з тегами `sha-<коміт>`, `latest` (лише з `master`) і `X.Y.Z` (лише для git-тегу `vX.Y.Z`) |

Образи лежать у `ghcr.io/booooooo76/mini-eshop-<сервіс>`: `catalog-api`, `orders-api`, `notifications-api`, `reviews-api`, `gateway`.

### Запуск із готових образів

Замість локальної збірки Docker бере образи з реєстру:

```bash
docker compose -f docker-compose.yml -f docker-compose.registry.yml up -d
```

Конкретна збірка за тегом коміту: `IMAGE_TAG=sha-abc1234 docker compose -f docker-compose.yml -f docker-compose.registry.yml up -d`.

### Версії образів

Версійні образи з'являються, коли в репозиторій запушено git-тег: `git tag v1.0.0 && git push origin v1.0.0` дає образи `ghcr.io/booooooo76/mini-eshop-<сервіс>:1.0.0`. Версія вшита в образ (build-arg `APP_VERSION`) і видна в `GET /info` кожного сервісу разом з іменем пода (`{"service":"gateway","version":"1.0.0","pod":"..."}`). З версії `1.2.0` є ще `GET /version` лише з назвою сервісу й версією (`{"service":"gateway","version":"1.2.0"}`). Для Kubernetes (Лаба №3) використовуються лише такі версійні теги, `latest` не застосовується.

## Розгортання в Kubernetes (minikube)

Той самий застосунок розгортається в локальному кластері minikube. Усі маніфести лежать у [k8s/](k8s/), по одному файлу на об'єкт. Образи беруться з `ghcr.io/booooooo76/mini-eshop-<сервіс>` за версійним тегом (`1.0.0`, `1.1.0`); тег `latest` не використовується.

### Підготовка кластера

Потрібні Docker Desktop, [minikube](https://minikube.sigs.k8s.io/docs/start/) і kubectl.

```bash
minikube start --driver=docker --cpus=4 --memory=6144
minikube addons enable metrics-server
minikube addons enable ingress
```

### Розгортання з нуля однією командою

```bash
kubectl apply -f k8s/
kubectl get all -n mini-eshop
```

`kubectl apply` обробляє файли за алфавітом, тому порядок вшито в номери: `00` namespace, `10`–`13` конфігурація й секрет, `20`–`41` Postgres, RabbitMQ і Redis, `50`–`59` сервіси, `90` Ingress. Перший запуск довший, бо minikube завантажує образи; усі поди мають стати `Running` приблизно за 1–2 хвилини.

Доступ ззовні йде через Ingress. У Windows з драйвером docker адреса кластера з хоста недоступна, тому в окремому вікні запустіть і не закривайте:

```bash
minikube tunnel
```

Після цього застосунок доступний на `http://127.0.0.1`:

| Адреса | Що це |
|---|---|
| `/info` | Gateway: версія образу та ім'я пода, що відповів |
| `/version` | Gateway: лише версія образу (з `1.2.0`); `/catalog/version` тощо для інших сервісів |
| `/catalog/api/products`, `/orders/api/orders`, `/notifications/api/notifications`, `/reviews/api/reviews` | API сервісів через Gateway |
| `/swagger` | агрегований Swagger (документація завантажується, «Try it out» не працює, див. нижче) |

Запасний варіант без тунелю: `kubectl port-forward svc/gateway 8080:8080` (застосунок на `http://localhost:8080`, але запити йдуть в один под, тож розподіл між репліками не видно). RabbitMQ UI: `kubectl port-forward svc/rabbitmq 15672:15672` і `http://localhost:15672` (guest/guest).

Видалити все разом з даними: `kubectl delete namespace mini-eshop`.

### Параметри ConfigMap і Secret

ConfigMap `eshop-config` підключається до всіх сервісів через `envFrom`:

| Ключ | Значення | Навіщо |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | вмикає Swagger і OpenAPI |
| `RabbitMQ__Host` | `rabbitmq` | ім'я Service з RabbitMQ |
| `Redis__Connection` | `redis:6379` | адреса Redis для кешу Catalog |

ConfigMap `gateway-config` (лише Gateway) підміняє зашиті адреси Swagger `localhost:808x` на шляхи через сам Gateway: `SwaggerAggregator__Endpoints__0..3__Url`. ConfigMap `postgres-init` містить скрипт, який створює бази `orders_db`, `notifications_db` і `reviews_db`.

Secret `eshop-secrets` (кожен сервіс бере через `secretKeyRef` лише свій рядок підключення):

| Ключ | Хто використовує |
|---|---|
| `POSTGRES_USER`, `POSTGRES_PASSWORD` | Postgres |
| `ConnectionStrings__Catalog`, `__Orders`, `__Notifications`, `__Reviews` | відповідний API |

Значення в Secret тестові (`eshop-lab-pass`), у реальних проєктах Secret у репозиторій не комітять. Змінні оточення зчитуються при старті контейнера, тому після зміни ConfigMap чи Secret потрібен `kubectl rollout restart deployment/<ім'я>`.

### Життєвий цикл

```bash
kubectl delete pod <под>                                   # самовідновлення: Deployment створить новий
kubectl scale deployment gateway --replicas=4              # масштабування
kubectl set image deployment/gateway gateway=ghcr.io/booooooo76/mini-eshop-gateway:1.1.0   # rolling update
kubectl rollout status deployment/gateway
kubectl rollout history deployment/gateway                 # історія ревізій
kubectl rollout undo deployment/gateway                    # відкат до попередньої ревізії
```

Версію працюючого образу показує `curl http://127.0.0.1/info`. Під час оновлення `maxUnavailable: 0` лишає всі поди готовими, доки не піднімуться нові, а `preStop`-пауза в 10 с дає ingress-контролеру встигнути прибрати под зі свого списку. Без цієї паузи частина запитів під час оновлення зависала (у вимірюванні 2 із ~70).

### Діагностика

```bash
kubectl set image deployment/reviews-api reviews-api=ghcr.io/booooooo76/mini-eshop-reviews-api:9.9.9   # неіснуючий тег
kubectl get pods -l app=reviews-api        # новий под у ImagePullBackOff, старі поди працюють
kubectl describe pod <под>                 # розділ Events: "... 9.9.9: not found"
kubectl logs <под> -c reviews-api          # логи контейнера
kubectl exec -it <под> -c reviews-api -- bash
kubectl rollout undo deployment/reviews-api
```

Прапорець `-c` потрібен, бо в подів є ще init-контейнер `wait-for-deps`.

### Особливості та обмеження

- Postgres, RabbitMQ і Redis працюють в одній репліці (базу не масштабують збільшенням `replicas`); дані Postgres і RabbitMQ лежать на PVC і переживають видалення пода, Redis (кеш) без PVC.
- Сервіси мають init-контейнер `wait-for-deps`, який чекає на порти Postgres, RabbitMQ і Redis, бо в Kubernetes немає `depends_on`.
- Схема БД створюється через `EnsureCreated()`. Якщо дві репліки одного сервісу стартують одночасно на порожній базі, одна з них може впасти з помилкою `42P07 relation already exists` і після автоматичного перезапуску працює (так було з Notifications і Reviews під час розгортання з нуля).
- Liveness-проба API перевіряє лише відкритий порт, а не `/health`: інакше падіння Postgres перезапускало б усі поди. Readiness-проба йде на `/health` з перевіркою БД.
- У Swagger через Gateway документація всіх сервісів завантажується, але «Try it out» не працює: у специфікації адреса сервера `http://catalog-api:8080/`, яку знає лише кластер.
- Service розподіляє навантаження за з'єднаннями, а не за запитами, тому між двома репліками Catalog розкид нерівномірний (приблизно 2:1 у вимірюванні).
- minikube встановив Kubernetes 1.37, а kubectl має версію 1.34, тобто поза офіційним допуском ±1; для наших команд це не заважало.

## Helm-чарт

Ті самі об'єкти Kubernetes, що в `k8s/`, пакуються в Helm-чарт [helm/mini-eshop/](helm/mini-eshop/): жорстко прописані значення (кількість реплік, тег образу, ресурси) винесені в `values.yaml`, а імена об'єктів будуються від імені релізу, тож чарт можна встановити кілька разів в один кластер. Потрібен Helm 3.

### Версії: `version` і `appVersion`

У `Chart.yaml` є два номери, і вони змінюються незалежно:

| Поле | Що означає | Приклад у цьому проєкті |
|---|---|---|
| `version` | версія самого чарта, тобто «упаковки»: шаблонів, значень, структури. Це SemVer; піднімається, коли змінюється чарт | `0.1.0` |
| `appVersion` | версія застосунку, який чарт розгортає, тобто тег образів `mini-eshop-*` у ghcr.io | `1.1.0` |

Приклад: щоб випустити нову версію застосунку (`1.2.0`) без змін у шаблонах, міняється лише `appVersion` (чарт при цьому теж отримує нову `version`, бо змінився його вміст). Навпаки, якщо в шаблоні виправлена помилка, а застосунок той самий, піднімається `version`, а `appVersion` лишається `1.1.0`. За замовчуванням чарт бере тег образів із `appVersion`. Колонка `APP VERSION` у `helm list` і `helm history` показує саме `appVersion` чарта, а не тег, що реально працює (він видно в `GET /info`).

### Склад чарта

```
helm/mini-eshop/
├── Chart.yaml              # назва, опис, version, appVersion
├── values.yaml             # значення за замовчуванням (відповідають розробці)
├── values-dev.yaml         # середовище розробки
├── values-prod.yaml        # «промислове» середовище
├── files/init-multiple-dbs.sh   # скрипт створення баз (копія infra/postgres/)
└── templates/
    ├── _helpers.tpl        # імена, мітки, образ, init-контейнер wait-for-deps
    ├── NOTES.txt           # підказка після встановлення
    ├── configmap-*.yaml, secret.yaml
    ├── postgres-*.yaml, rabbitmq-*.yaml, redis-*.yaml
    ├── catalog-api-*.yaml, orders-api-*.yaml, notifications-api-*.yaml, reviews-api-*.yaml, gateway-*.yaml
    ├── ingress.yaml        # створюється лише при ingress.enabled
    ├── hooks/pre-upgrade-job.yaml    # хук: перевірка БД перед оновленням
    └── tests/test-connection.yaml    # helm test
```

Чарт дає ті самі 23 об'єкти, що й `k8s/` (без Namespace: його створює `--create-namespace`). Імена об'єктів будуються від імені релізу (`<реліз>-mini-eshop-<компонент>`), тож чарт можна поставити кілька разів в один кластер.

### Встановлення

Пароль Postgres у репозиторії не зберігається: чарт відмовляється працювати без `secrets.postgresPassword`. Його передають при встановленні (у реальному проєкті з менеджера секретів, тут для навчального кластера просто значення).

Середовище розробки (1 репліка, образи `1.1.0`, без Ingress):

```bash
helm install dev helm/mini-eshop -f helm/mini-eshop/values-dev.yaml --set secrets.postgresPassword=<пароль> -n eshop-dev --create-namespace --wait
```

Промислове середовище (2 репліки, образи `1.0.0`, з Ingress, `Production`):

```bash
helm install prod helm/mini-eshop -f helm/mini-eshop/values-prod.yaml --set secrets.postgresPassword=<пароль> -n eshop-prod --create-namespace --wait
```

Обидва можна тримати в одному кластері одночасно. Доступ: у `prod` через Ingress (`minikube tunnel`, потім `http://127.0.0.1/info`), у `dev` через `kubectl port-forward -n eshop-dev svc/dev-mini-eshop-gateway 8080:8080`. Підказку з точними командами Helm друкує після встановлення (`helm status <реліз> -n <namespace>`). Два Ingress з однаковим хостом і шляхом `/` можуть конфліктувати (ingress-nginx може відхилити другий), тому в `dev` Ingress вимкнений, а за потреби йому задають окремий `ingress.host`.

Перевірка без встановлення: `helm lint helm/mini-eshop --set secrets.postgresPassword=x` і `helm template <реліз> helm/mini-eshop -f <файл значень> --set secrets.postgresPassword=x`.

### Параметри `values`

Пріоритет (від слабшого до сильнішого): `values.yaml` → файли `-f` (пізніший перемагає) → `--set`.

| Параметр | За замовчуванням | dev | prod | Призначення |
|---|---|---|---|---|
| `image.repository` | `ghcr.io/booooooo76/mini-eshop` | | | спільний префікс образів сервісів (`<префікс>-catalog-api` тощо) |
| `image.tag` | `""` (бере `appVersion`) | `1.1.0` | `1.0.0` | тег образів застосунку |
| `image.pullPolicy` | `IfNotPresent` | | | політика завантаження образів |
| `config.environment` | `Development` | `Development` | `Production` | `ASPNETCORE_ENVIRONMENT` (у `Development` доступний Swagger) |
| `config.logLevel` | `Information` | `Information` | `Warning` | рівень логів застосунку |
| `secrets.postgresUser` | `postgres` | | | користувач Postgres |
| `secrets.postgresPassword` | `""` (обов'язково) | `--set` | `--set` | пароль Postgres; з нього складаються рядки підключення |
| `app.replicas` | `1` | `1` | `2` | реплік кожного з 5 сервісів |
| `app.resources` | requests `25m/64Mi`, limits `250m/256Mi` | те саме | requests `100m/128Mi`, limits `500m/384Mi` | CPU і пам'ять сервісів |
| `app.service.type`, `app.service.port` | `ClusterIP`, `8080` | | | Service сервісів |
| `app.preStopSleepSeconds` | `10` | | | пауза перед зупинкою пода (щоб Ingress встиг його прибрати) |
| `postgres.replicas`, `rabbitmq.replicas`, `redis.replicas` | `1` | | | не збільшувати: база й черга не масштабуються реплікою |
| `postgres.image`, `rabbitmq.image`, `redis.image`, `waitForDeps.image` | `postgres:16-alpine`, `rabbitmq:3-management-alpine`, `redis:7-alpine`, `busybox:1.37.0` | | | образи інфраструктури |
| `postgres.resources` | requests `50m/128Mi`, limits `500m/512Mi` | | requests `100m/256Mi` | ресурси Postgres (зміна перезапускає базу: стратегія `Recreate`) |
| `rabbitmq.resources`, `redis.resources`, `waitForDeps.resources` | див. `values.yaml` | | | ресурси решти компонентів |
| `postgres.persistence.*`, `rabbitmq.persistence.*` | `enabled: true`, `size: 1Gi`, `storageClass: ""` | | | PVC; при `enabled: false` диск стає `emptyDir` |
| `ingress.enabled` | `false` | `false` | `true` | створити Ingress |
| `ingress.className`, `ingress.host` | `nginx`, `""` | | | клас і необов'язковий хост Ingress |
| `hooks.preUpgradeCheck.enabled` | `true` | | | хук перед оновленням |
| `tests.enabled` | `true` | | | под для `helm test` |
| `nameOverride`, `fullnameOverride` | `""` | | | зміна назви в іменах об'єктів |

### Життєвий цикл релізу

```bash
helm upgrade dev helm/mini-eshop -f helm/mini-eshop/values-dev.yaml --set secrets.postgresPassword=<пароль> --set image.tag=1.0.0 -n eshop-dev --wait   # оновлення
helm upgrade dev helm/mini-eshop -f helm/mini-eshop/values-prod.yaml --set secrets.postgresPassword=<пароль> -n eshop-dev --wait                    # з файлом іншого середовища
helm history dev -n eshop-dev                  # ревізії
helm get values dev -n eshop-dev --revision 2  # значення конкретної ревізії
helm rollback dev 2 -n eshop-dev --wait        # відкат (з'являється нова ревізія "Rollback to 2")
helm test dev -n eshop-dev                     # перевірка працездатності
helm uninstall dev -n eshop-dev                # видалення (разом з PVC)
```

Під час `upgrade` потрібно знову передати `-f` і пароль (інакше значення візьмуться з `values.yaml`). У шаблони Deployment вбудований відбиток ConfigMap і Secret (`checksum/...`): після зміни налаштувань поди перезапускаються самі.

### Хук і тест

- **Хук `pre-upgrade`** ([pre-upgrade-job.yaml](helm/mini-eshop/templates/hooks/pre-upgrade-job.yaml)) перед кожним оновленням запускає Job, який до 30 с чекає на порт Postgres. Якщо бази немає, `helm upgrade` завершується помилкою `pre-upgrade hooks failed`, а сервіси не чіпаються (у `helm history` лишається ревізія зі статусом `failed`). Успішний Job видаляється сам.
- **`helm test <реліз> -n <namespace>`** ([test-connection.yaml](helm/mini-eshop/templates/tests/test-connection.yaml)) запускає под, який через Gateway звертається до `/info` усіх п'яти сервісів і перевіряє, що працює очікувана версія. Под видаляється після успіху, тому прапорець `--logs` логів не покаже (див. звіт про Лабу №4).

### Особливості чарта

- Стан релізу Helm зберігає в Secret `sh.helm.release.v1.<реліз>.v<ревізія>` у namespace релізу. Туди потрапляють і значення, передані через `--set`, зокрема пароль: доступ до таких Secret треба обмежувати.
- Пароль Postgres запам'ятовується базою при першому створенні (`PGDATA`). Зміна `secrets.postgresPassword` під час `upgrade` змінить рядки підключення, але не пароль у вже створеній базі.
- Зміна ресурсів Postgres перезапускає базу (стратегія `Recreate`, диск PVC зберігається). Сервіси на цей час втрачають з'єднання; під час таких перезапусків `catalog-api` і `notifications-api` бували перезапущені самим Kubernetes.
- `helm uninstall` не видаляє namespace, створений через `--create-namespace`; його видаляють окремо (`kubectl delete namespace`).
- Kubernetes-імена об'єктів містять ім'я релізу, тому Gateway отримує адреси сервісів не з `appsettings.json`, а зі змінних оточення з ConfigMap `<реліз>-mini-eshop-gateway-config`.
- Обмеження з розділу «Розгортання в Kubernetes» (гонка `EnsureCreated()`, «Try it out» у Swagger) діють і тут.

## Відомі обмеження

- Схема БД створюється через `EnsureCreated()`, а не EF Core-міграції (достатньо для лабораторної, але не для продакшену).
- `Microsoft.AspNetCore.OpenApi` 10.0.6 тягне транзитивну залежність `Microsoft.OpenApi` 2.0.0 із відомою вразливістю (CVE-2026-49451, NU1903). Її знайшов Trivy у CI; виправлено явним закріпленням `Microsoft.OpenApi` 2.7.5 у чотирьох API-проєктах (гілка 3.x ламає сумісність з генератором ASP.NET Core, а 2.7.5 сумісна). Якщо `Microsoft.AspNetCore.OpenApi` колись сам почне тягнути патчену версію, закріплення можна прибрати.
- CORS на всіх чотирьох API дозволений з будь-якого origin (`AllowAnyOrigin`) — потрібно, щоб агрегований Swagger на Gateway міг підвантажити `/openapi/v1.json` з інших портів. Прийнятно для локальної лабораторної роботи, не для продакшену.
- Postgres опублікований на хостовому порту **5433**, а не стандартному 5432 — на машині розробника вже може працювати нативний (не-Docker) Postgres-сервер на 5432, і Windows непередбачувано маршрутизує з'єднання між ними.
- `ASPNETCORE_ENVIRONMENT=Development` явно виставлений у docker-compose для всіх сервісів, щоб Swagger UI був доступний і в контейнерах (за замовчуванням .NET-контейнери піднімаються як Production, де Swagger вимкнено).
