# Nhật ký cập nhật dự án PrintGrid (Agent)

## Yêu cầu thực hiện
**Chức năng / hạng mục:** Bảng người dùng + vai thật (Chuyển bảng người dùng và phân quyền từ hardcode vào cơ sở dữ liệu)
**Mã yêu cầu:** FR-ADMIN-001, BR-ACCESS-005, AC01, AC02

## Các thay đổi chính

### 1. Database & Entity (`Customer.cs`)
- Đã thêm thuộc tính `IsActive` (boolean, mặc định `true`) vào entity `Customer`.
- Đã thêm thuộc tính `Roles` (mảng chuỗi `string[]`, mặc định `["Customer"]`) vào entity `Customer`.
- **Entity Framework Core:** Đã tạo migration `AddUserRoles` để ánh xạ trường `Roles` thành `text[]` trong PostgreSQL, đồng thời thêm trường `IsActive` (boolean). Database đã được apply thành công.

### 2. Gỡ bỏ DemoRoles & Cập nhật JWT Auth
- Xóa bỏ hoàn toàn file `DemoRoles.cs`.
- Cập nhật các Handler xác thực để sử dụng Role thực từ database:
  - `LoginCustomerCommandHandler.cs`: Truyền `customer.Roles` thay vì `DemoRoles.RolesFor`.
  - `RefreshSessionCommandHandler.cs`: Tương tự, sử dụng quyền thực tế khi refresh token.
  - `RegisterCustomerCommandHandler.cs`: Xóa các using namespace rác/không sử dụng.
- File cấu hình `JwtTokenService.cs` vẫn hoạt động tốt, tự động parse các phần tử trong mảng Role ra thành các claims `ClaimTypes.Role` riêng biệt.

### 3. Cung cấp Admin APIs (Users Controller)
- Thêm `UsersController.cs` (yêu cầu Authorization Role `Admin`):
  - `POST /api/v1/users`: Tạo User mới kèm quyền tuỳ chọn. (Qua handler `CreateUserCommandHandler.cs`)
  - `PUT /api/v1/users/{id}/role`: Phân quyền. 1 tài khoản chỉ giữ 1 danh sách Role mới đè lên Role cũ, thoả mãn logic "One staff role per account".
  - `PUT /api/v1/users/{id}/deactivate`: Khóa tài khoản.
- Chặn Admin tự thay đổi quyền hoặc khóa chính mình (Kiểm tra `ActingUserId == UserId` trong `AssignRoleCommandHandler` và `DeactivateUserCommandHandler`).

### 4. Fix lỗi & Cập nhật Metadata
- Đã fix lỗi thiết sót namespace `IPasswordHasher` và `ITokenService` sau khi gỡ bỏ `Auth` directory using.
- Đã gán trực tiếp dữ liệu vào database qua SQL để test cho 2 users:
  - `fizzwild3@gmail.com` -> `{"LabManager"}`
  - `nhat9983@gmail.com` -> `{"Admin"}`

> Ghi chú: File này đã được thêm vào `.gitignore` để không bị push lên GitHub.
