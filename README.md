# Architecture Notes — ApplifyLab Backend

## Authentication: JWT over HttpOnly Cookies

The API uses stateless JWTs for auth, but delivers them exclusively via
`HttpOnly` cookies — never exposed to frontend JavaScript (no
`localStorage`, no `Authorization` header).

| Cookie | Contents | HttpOnly | Path | Lifetime |
|---|---|---|---|---|
| `AccessToken` | signed JWT (HS256) | ✅ | `/` | 15 min |
| `RefreshToken` | random 64-byte opaque token (hash stored server-side) | ✅ | `/api/auth` | 7 days |

**Flow:**
1. Login/refresh → `AuthController.SetAuthCookies` issues both cookies.
2. Every request → browser auto-attaches `AccessToken`; `CookieJwtBearerEvents.MessageReceived`
   pulls it into the JWT bearer pipeline (SignalR gets it via `?access_token=` query param instead,
   since WebSocket handshakes can't carry custom cookies/headers the same way).
3. `TokenValidated` also checks the Redis JWT blacklist (`jti` claim) so a logged-out token
   is rejected before its natural expiry.
4. On 401, frontend calls `POST /auth/refresh` once — refresh cookie is validated
   against its DB hash, both cookies are reissued.

**Why cookies instead of a client-held token:** `HttpOnly` cookies can't be read by JS,
which removes the #1 JWT XSS-theft vector. `SameSite=None; Secure` is required since the
API and frontend are on different origins, which weakens CSRF protection — compensated for
by an `X-Requested-With` header check + `withCredentials: true` on the frontend client.

**Why access + refresh instead of one long-lived token:** short-lived access tokens cap
the exposure window if leaked; the refresh token can be revoked/rotated server-side
(unlike a bare JWT) and pairs with the blacklist for real logout support.

---

## Redis Usage

Redis is the shared, low-latency layer for anything too hot for Postgres or that must be
shared across multiple API instances.

| Use case | Class | Structure | Why |
|---|---|---|---|
| Feed page cache | `RedisFeedCacheService` | `STRING`, 45s TTL, keys `feed:public:cursor:*` / `feed:private:{userId}:cursor:*` | Absorbs repeat reads of an expensive paginated query; invalidated on new post |
| Like/comment counters | `RedisLikeCounterCache` | `SET` for dedupe (`likes:{type}:{id}`) + `STRING` counter (`counter:like:{type}:{id}`) | O(1) like/unlike without hot-row `UPDATE` contention on Postgres |
| JWT revocation | `RedisJwtBlacklistService` | `STRING` `blacklist:jwt:{jti}`, TTL = token's remaining lifetime | Enables real logout for otherwise-stateless JWTs; self-expiring, no cleanup job needed |
| Rate limiting | `AspNetCoreRateLimit.Redis` | IP-based counters via `IDistributedCache` | Shared limit counters across multiple API instances (in-memory wouldn't be) |
| SignalR backplane | `AddStackExchangeRedis` (channel prefix `applifylab-signalr`) | Pub/Sub | Broadcasts hub messages (e.g. feed updates) to clients connected to *any* API instance |

**Consistency model:** Postgres remains the durable source of truth. Redis counters are
seeded from Postgres (`SeedCountAsync`, `When.NotExists`) and mirrored back via Hangfire jobs
(`LikeSyncJob` after each toggle, `LikeReconciliationJob` on a recurring schedule to correct
any drift). Feed cache never trusts cached like/comment counts — those are always overlaid
live from Redis at read time, so counts stay real-time even while post content is cached.

---

## Background Jobs (Hangfire, Postgres-backed storage)

| Job | Trigger | Purpose |
|---|---|---|
| `ThumbnailJob` | enqueued on post creation | Resizes uploaded image to max 400×400 JPEG, uploads it, sets `Post.ThumbnailUrl` — done off the request path so post creation isn't blocked by image processing |
| `LikeSyncJob` | enqueued on each like/unlike toggle | Mirrors the live Redis counter into the denormalized `LikeCount` column in Postgres |
| `LikeReconciliationJob` | recurring | Corrects any drift between Redis counters and Postgres counts |
