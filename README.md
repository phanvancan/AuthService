# AuthService SSO (.NET 10)

Giải pháp SSO cho hệ thống nội bộ gồm `AuthService` (API + Razor Pages UI), `Application1` và `Application2` (MVC). AuthService phát hành JWT, thiết lập cookie bảo mật để các ứng dụng chia sẻ đăng nhập.

## Thành phần & phạm vi
- `AuthService`: ASP.NET Core 10 Web API + UI, quản lý người dùng, vai trò, quyền; phát hành token/cookie SSO.
- `Application1`: ASP.NET Core 10 MVC cho Dashboard/Reports/Settings, dùng SSO.
- `Application2`: ASP.NET Core 10 MVC cho Dashboard/Monitoring/Logs/Settings, dùng SSO.
- `Database`: SQL Server (EF Core) lưu `Users`, `Roles`, `Permissions` và mapping.

## Kiến trúc & luồng SSO
1. Người dùng truy cập App1/2, middleware kiểm tra cookie `access_token`.
2. Nếu thiếu/hết hạn → chuyển hướng về `AuthService` (`/sso/login?returnUrl=...`).
3. Đăng nhập thành công → AuthService tạo access token + refresh token, set cookie HttpOnly/Secure, redirect về `returnUrl`.
4. Người dùng sang ứng dụng còn lại không cần đăng nhập lại (SSO).

## Công nghệ
- Backend: C# .NET 10
- API: ASP.NET Core Web API
- UI: Razor Pages (AuthService SSO UI), ASP.NET Core MVC (Application1/2)
- Auth: JWT Bearer + Cookie (HttpOnly, Secure, SameSite=Lax)
- ORM: Entity Framework Core
- Database: SQL Server
- Password: BCrypt

## Thiết kế CSDL (AuthService)
- `Users`: Id, Username, PasswordHash, Email, FullName, IsActive, CreatedAt, RefreshToken, RefreshTokenExpiresAt
- `Roles`: Id, RoleName, Description
- `UserRoles`: Id, UserId, RoleId
- `Permissions`: Id, PermissionName, Description
- `RolePermissions`: Id, RoleId, PermissionId

## API chính
- Auth: `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/refresh-token`, `POST /api/auth/refresh-token-cookie`, `POST /api/auth/logout`
- User (Admin): `GET /api/users`, `POST /api/users`, `PUT /api/users/{id}`, `DELETE /api/users/{id}`
- Role (Admin): `GET /api/roles`, `POST /api/roles`, `POST /api/roles/assign`
- SSO UI: `GET /sso/login?returnUrl=...`, `POST /sso/login`, `GET /sso/logout?returnUrl=...`

## JWT & Cookie
- Claims: UserId, Username, Role
- Access token: 60 phút; Refresh token: 7 ngày
- Cookie: `access_token`, `refresh_token`; HttpOnly=true, Secure=true, SameSite=Lax, Path=/, (Prod) Domain=`.company.com`

## CORS
- Whitelist: `https://localhost:7002`, `https://localhost:7281`
- Cho phép credentials để gửi cookie SSO.

## Cấu trúc source code
- `AuthService.slnx`
- `AuthService/` (API, Data, Services, Repositories, Controllers, Razor Pages SSO UI)
- `Application1/` (MVC + JWT middleware)
- `Application2/` (MVC + JWT middleware)
- `postman/` (collection + environment)
- `docs/` (tài liệu hệ thống)

## Cài đặt & chạy
Yêu cầu: .NET SDK 10, SQL Server, dev HTTPS cert.

```bash
dotnet build AuthService.slnx
cd AuthService && dotnet run
cd Application1 && dotnet run
cd Application2 && dotnet run
```

## Kiểm thử
- Thủ công: App1 → redirect login, đăng nhập → vào App1; mở App2 → vào trực tiếp; logout SSO → App1/App2 yêu cầu login lại.
- Tự động: `powershell -ExecutionPolicy Bypass -File .\sso-regression.ps1`

## Postman
- Collection: `postman/AuthService-SSO.postman_collection.json`
- Environment: `postman/AuthService-SSO.local.postman_environment.json`

## Bảo mật & vận hành
- Không hard-code JWT secret; dùng Secret Manager/Key Vault.
- Giới hạn CORS theo domain thật.
- Bật audit log cho login/logout/role changes.
- Dùng TLS certificate hợp lệ.

## Định hướng mở rộng
Cân nhắc chuẩn OpenID Connect (IdentityServer, Keycloak) cho nhu cầu federation, consent và SSO đa hệ thống quy mô lớn.
