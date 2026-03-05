TÀI LIỆU HỆ THỐNG SSO - .NET 10 (API + MVC + SQL SERVER)
Hệ thống: AuthService + Application1 + Application2
Ngày cập nhật: 05/03/2026
1. Mục tiêu
•	Xây dựng xác thực tập trung (Single Sign-On) cho nhiều ứng dụng nội bộ.
•	App1 và App2 không có login riêng, dùng đăng nhập từ AuthService.
•	Dùng JWT + Cookie bảo mật + SQL Server cho quản lý tài khoản/phân quyền.
2. Phạm vi hệ thống
Thành phần	Mô tả
AuthService	ASP.NET Core 10 Web API, quản lý User/Role/Auth, phát hành token và cookie SSO.
Application1	ASP.NET Core 10 MVC, nghiệp vụ Dashboard/Reports/Settings, dùng SSO.
Application2	ASP.NET Core 10 MVC, nghiệp vụ Dashboard/Monitoring/Logs/Settings, dùng SSO.
Database	SQL Server (EF Core) lưu user, role, permission, mapping.
3. Kiến trúc tổng thể
•	AuthService đóng vai trò trung tâm xác thực.
•	App1/App2 dùng JWT middleware đọc cookie access_token.
•	Khi token thiếu/hết hạn, app redirect về /sso/login của AuthService.
•	AuthService login thành công sẽ set cookie và redirect về app gốc bằng returnUrl.
3.1 Luồng đăng nhập SSO
1.	User truy cập App1 hoặc App2.
2.	Middleware kiểm tra cookie access_token.
3.	Nếu không hợp lệ → redirect sang AuthService login.
4.	User nhập tài khoản/mật khẩu.
5.	AuthService xác thực, tạo access token + refresh token.
6.	AuthService set cookie HttpOnly/Secure và redirect về app ban đầu.
7.	User truy cập App còn lại không cần login lại (SSO).
4. Công nghệ sử dụng
Nhóm	Công nghệ
Backend	C# .NET 10
API	ASP.NET Core Web API
MVC	ASP.NET Core MVC + Razor
Authentication	JWT Bearer + Cookie
ORM	Entity Framework Core
Database	SQL Server
Password	BCrypt
5. Thiết kế cơ sở dữ liệu (AuthService)
•	Users: Id, Username, PasswordHash, Email, FullName, IsActive, CreatedAt, RefreshToken, RefreshTokenExpiresAt.
•	Roles: Id, RoleName, Description.
•	UserRoles: Id, UserId, RoleId.
•	Permissions: Id, PermissionName, Description.
•	RolePermissions: Id, RoleId, PermissionId.
6. API danh sách endpoint
6.1 Auth API
•	POST /api/auth/login
•	POST /api/auth/register
•	POST /api/auth/refresh-token
•	POST /api/auth/refresh-token-cookie
•	POST /api/auth/logout
6.2 User API (Admin)
•	GET /api/users
•	POST /api/users
•	PUT /api/users/{id}
•	DELETE /api/users/{id}
6.3 Role API (Admin)
•	GET /api/roles
•	POST /api/roles
•	POST /api/roles/assign
6.4 SSO UI
•	GET /sso/login?returnUrl=...
•	POST /sso/login
•	GET /sso/logout?returnUrl=...
7. JWT và Cookie
7.1 JWT Claims
•	UserId
•	Username
•	Role
7.2 Token lifetime
•	Access Token: 60 phút
•	Refresh Token: 7 ngày
7.3 Cookie bảo mật
•	Cookie name: access_token, refresh_token
•	HttpOnly: true
•	Secure: true
•	SameSite: Lax
•	Path: /
•	Production: đặt Domain = .company.com
8. CORS
AuthService dùng policy strict whitelist origin, chỉ cho phép:
•	https://localhost:7002
•	https://localhost:7281
Cho phép credentials để browser gửi cookie SSO đúng chuẩn.
9. Cấu trúc source code
•	AuthService.slnx
•	AuthService/ (API + Data + Services + Repositories + Controllers)
•	Application1/ (MVC + JWT middleware + views nghiệp vụ)
•	Application2/ (MVC + JWT middleware + views nghiệp vụ)
•	postman/ (Collection + Environment JSON)
•	docs/ (tài liệu hệ thống)
10. Hướng dẫn cài đặt và chạy
10.1 Yêu cầu
•	.NET SDK 10
•	SQL Server
•	Dev HTTPS certificate
10.2 Build
dotnet build AuthService.slnx
10.3 Run
•	cd AuthService && dotnet run
•	cd Application1 && dotnet run
•	cd Application2 && dotnet run
11. Kiểm thử hệ thống
11.1 Test thủ công
1.	Mở App1 dashboard → redirect login AuthService.
2.	Login thành công → vào App1.
3.	Mở App2 dashboard → vào trực tiếp, không login lại.
4.	Logout SSO → cả App1/App2 yêu cầu login lại.
11.2 Test tự động
Script regression:
powershell -ExecutionPolicy Bypass -File .\sso-regression.ps1
12. Postman
•	Collection: postman/AuthService-SSO.postman_collection.json
•	Environment: postman/AuthService-SSO.local.postman_environment.json
13. Bảo mật và vận hành production
•	Không lưu JWT secret cứng trong source code production.
•	Dùng Secret Manager/Key Vault.
•	Giới hạn CORS theo domain thật.
•	Bật audit log cho login/logout/role changes.
•	Sử dụng TLS certificate hợp lệ.
14. Định hướng mở rộng
Cho triển khai enterprise quy mô lớn, nên cân nhắc chuẩn OpenID Connect (IdentityServer hoặc Keycloak) để có tính năng federation, consent, SSO đa hệ thống nâng cao.
________________________________________
Tài liệu được xuất tự động từ trạng thái hệ thống hiện tại.
