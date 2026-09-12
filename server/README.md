# deblog .NET Web API Server

Modern, high-performance blog backend built with **ASP.NET Core (.NET 10)**, **Vertical Slice Architecture**, **Entity Framework Core**, **PostgreSQL (Supabase)**, and **Supabase Auth**.

---

## Tech Stack

1. **.NET 10 Web API** (Minimal APIs with modular `MapGroup` endpoints)
2. **Entity Framework Core 10** with **Npgsql** (isolated in `deblog` PostgreSQL schema)
3. **Supabase PostgreSQL** & **Supabase Auth** (JWT Bearer validation)
4. **Swashbuckle Swagger UI** (configured with interactive Bearer token testing)
5. **DotNetEnv** (loads `.env` configuration)
6. **In-Memory Anti-Spam Cache** (cooldown tracking for post views, likes, and shares)

---

## Completed Architecture & Checklist

- [x] **DotEnv Utility Setup**: Configured in [Program.cs](file:///home/dej/Projects/projectx/deblog/server/Program.cs) via `DotNetEnv.Env.TraversePath().Load()`.
- [x] **SwaggerUI Utility Setup**: Configured with JWT Bearer authorization in [Common/Extensions/SwaggerExtensions.cs](file:///home/dej/Projects/projectx/deblog/server/Common/Extensions/SwaggerExtensions.cs).
- [x] **Main Author Automated Seeding**: Automatically populates the main author profile from `.env` on server startup with `Role = "Admin"` (no passwords hardcoded).
- [x] **Role-Based Access Control (AdminOnly)**:
  - Exclusive access for Main Author: Posts CRUD, Comment Moderation, and User Management.
- [x] **Features Setup & File Structuring (Vertical Slice Architecture)**:
  - [x] **Posts Slice** ([Features/Posts](file:///home/dej/Projects/projectx/deblog/server/Features/Posts/)):
    - Canonical post URL links (`${APP_BASE_URL}/posts/${slug}`).
    - Post Analytics (1-to-1 table tracking Views, Likes, Shares, Approved Comments).
    - Lightweight deduplication service with visitor IP cooldown caching.
    - Public analytics increment endpoints (`/analytics/view`, `/analytics/like`, `/analytics/share`).
    - Admin-only post authoring and management (create, update, delete).
    - Public listing for published posts; Admin can view drafts and unpublished posts.
  - [x] **Comments Slice** ([Features/Comments](file:///home/dej/Projects/projectx/deblog/server/Features/Comments/)):
    - Guest commenting with required email validation; auto-provisions `Guest` user profile (fallback name "Anonymous").
    - Secure `ManagementToken` returned on creation for guest edits and soft-removals (`X-Comment-Token`).
    - Moderation statuses: `Pending` (default for guest comments), `Approved`, `Removed`.
    - Public listing returns **only** `Approved` comments.
    - Admin moderation endpoints (`GET /api/admin/comments`, `PATCH /api/admin/comments/{id}/status`, `DELETE /api/comments/{id}`).
  - [x] **Users Slice** ([Features/Users](file:///home/dej/Projects/projectx/deblog/server/Features/Users/)):
    - Authenticated user profile retrieval & sync (`GET /api/users/me`, `PUT /api/users/me`).
    - Public profile view (`GET /api/users/{id}`).
    - Full Admin CRUD for users (`GET /api/admin/users`, `POST /api/admin/users`, `PUT /api/admin/users/{id}`, `DELETE /api/admin/users/{id}`).
- [x] **Database & Migrations**:
  - Isolated under dedicated `deblog` PostgreSQL schema in Supabase.
  - Custom schema applied to `__EFMigrationsHistory`, `Users`, `Posts`, `PostAnalytics`, and `Comments`.
  - Version-controlled migrations in [Migrations/](file:///home/dej/Projects/projectx/deblog/server/Migrations/).

---

## API Endpoints Reference

### Posts (`/api/posts`)
| Method | Endpoint | Access | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/posts` | Public / Admin | List posts (pagination, search; admin sees drafts) |
| `GET` | `/api/posts/{idOrSlug}` | Public / Admin | Get post by ID or Slug with canonical URL and analytics |
| `POST` | `/api/posts` | **Admin Only** | Create blog post |
| `PUT` | `/api/posts/{id}` | **Admin Only** | Update blog post |
| `DELETE` | `/api/posts/{id}` | **Admin Only** | Delete blog post |
| `POST` | `/api/posts/{idOrSlug}/analytics/view` | Public | Increment views (visitor cooldown deduplicated) |
| `POST` | `/api/posts/{idOrSlug}/analytics/like` | Public | Increment likes (visitor cooldown deduplicated) |
| `POST` | `/api/posts/{idOrSlug}/analytics/share` | Public | Increment shares |

### Comments (`/api/posts/{postId}/comments` & `/api/comments`)
| Method | Endpoint | Access | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/posts/{postId}/comments` | Public | Get approved comments for post |
| `POST` | `/api/posts/{postId}/comments` | Public / Guest | Post guest comment (returns `managementToken`) |
| `PUT` | `/api/comments/{id}` | Guest Token / Admin | Update comment content (`X-Comment-Token`) |
| `DELETE` | `/api/comments/{id}` | Guest Token / Admin | Guest: soft remove (`Removed`); Admin: permanent delete |
| `GET` | `/api/admin/comments` | **Admin Only** | Moderation queue (filter by `status=pending/approved/removed`) |
| `PATCH` | `/api/admin/comments/{id}/status` | **Admin Only** | Change comment status (`Approved`, `Pending`, `Removed`) |

### Users (`/api/users` & `/api/admin/users`)
| Method | Endpoint | Access | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/users/me` | Authenticated | Get or provision current user profile |
| `PUT` | `/api/users/me` | Authenticated | Update current user profile |
| `GET` | `/api/users/{id}` | Public | Get public profile |
| `GET` | `/api/admin/users` | **Admin Only** | List all users (paginated, search, filter by role) |
| `POST` | `/api/admin/users` | **Admin Only** | Create user account |
| `PUT` | `/api/admin/users/{id}` | **Admin Only** | Update user account details or role |
| `DELETE` | `/api/admin/users/{id}` | **Admin Only** | Delete user account |

---

## Configuration (`.env`)

```bash
# PostgreSQL / Supabase Database Connection String (Session Pooler with IPv4 support)
DB_CONNECTION="Host=aws-0-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.your-project;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"

# PostgreSQL Schema (isolates deblog tables and migrations from other apps in Supabase)
DB_SCHEMA="deblog"

# Supabase Project JWT Secret
JWT_SECRET="your-supabase-jwt-secret"

# Allowed CORS Origins (for Angular client @web)
CORS_ORIGINS="http://localhost:4200,http://localhost:3000"

# Main Author Details (automatically seeded on startup with Role: Admin)
AUTHOR_USERNAME="dej"
AUTHOR_EMAIL="derickespinosa.espinosa@gmail.com"
AUTHOR_DISPLAYNAME="Derick Espinosa"
AUTHOR_AVATARURL="http://deblog.derickespinosa.site/assets/images/me.png"

# Frontend Base URL (for generating canonical post URLs)
APP_BASE_URL="http://localhost:4200"
```

---

## Running the Server & Tests

### Run the Server
```bash
# Apply migrations to Supabase
dotnet ef database update

# Run with hot-reload
dotnet watch
```

### Run the Test Suite
```bash
# Execute all feature integration tests
dotnet test
```

Navigate to `http://localhost:5247/swagger` for interactive Swagger documentation. To test Admin endpoints, click **Authorize** in Swagger and enter `Bearer <your-supabase-jwt-token>`.

---

## Directory Layout

```
server/
├── Common/
│   ├── Data/
│   │   └── AppDbContext.cs            # EF Core DbContext with deblog schema
│   ├── Entities/
│   │   └── BaseEntity.cs              # Base Guid Id, CreatedAt, UpdatedAt
│   ├── Extensions/
│   │   ├── AuthenticationExtensions.cs# JWT Bearer, AdminOnly policy & Claims
│   │   ├── DatabaseExtensions.cs      # Npgsql / InMemory registration & Seeding
│   │   └── SwaggerExtensions.cs       # Swagger generator with Bearer auth
│   ├── Middleware/
│   │   └── GlobalExceptionHandler.cs  # RFC 7807 ProblemDetails middleware
│   └── Services/
│       └── AnalyticsTracker.cs        # Visitor IP cooldown deduplication
├── Features/
│   ├── Users/                         # Vertical slice: User entity, DTOs, Endpoints
│   ├── Posts/                         # Vertical slice: Post & Analytics, DTOs, Endpoints
│   └── Comments/                      # Vertical slice: Comment entity, DTOs, Endpoints
├── tests/
│   ├── Common/
│   │   ├── TestWebApplicationFactory.cs# InMemory test host & client factories
│   │   └── TestAuthHandler.cs         # Header-driven test auth claims handler
│   ├── Features/
│   │   ├── Posts/
│   │   │   └── PostsTests.cs          # Posts CRUD, Analytics, & RBAC tests
│   │   ├── Comments/
│   │   │   └── CommentsTests.cs       # Guest comments, Tokens, & Moderation tests
│   │   └── Users/
│   │       └── UsersTests.cs          # Profile sync, RBAC User CRUD tests
│   └── server.Tests.csproj
├── Migrations/                        # Version-controlled EF Core migrations
├── Program.cs                         # Server startup, pipeline, and endpoint mapping
├── server.sln                         # Solution containing server and tests
├── appsettings.json
└── .env
```
