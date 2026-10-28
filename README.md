# Book Sales Core

Ứng dụng MVC quản lý danh mục sách, khách hàng, báo giá/đơn bán và báo cáo doanh số.

## Yêu cầu

- .NET 10 SDK
- SQL Server 2022 hoặc mới hơn, đang chạy

## Khởi tạo database

Mở [database/seed-test-data.sql](./database/seed-test-data.sql) trong SQL Server Management Studio và chạy toàn bộ file (hoặc dùng `sqlcmd`). **Script có tính phá hủy:** trong database `booksalescore`, script xóa toàn bộ bảng và sequence hiện có (gồm schema cũ, dữ liệu nghiệp vụ và migration history), sau đó tạo schema hiện tại và nạp lại dữ liệu demo `TST-`. Chỉ chạy trên database local/test; hãy sao lưu trước nếu cần giữ dữ liệu khác.

Tài khoản Administrator được tạo từ SQL:

| Vai trò | Tên đăng nhập | Mật khẩu tạm |
|---|---|---|
| Administrator | `admin` | `Password!23` |

Tài khoản yêu cầu đổi mật khẩu ở lần đăng nhập đầu. Chỉ dùng cho môi trường phát triển/kiểm thử; không chạy script hoặc dùng tài khoản này trên production.

Ứng dụng mặc định kết nối SQL Server tại `localhost,1434`, database `booksalescore`. Với SQL Login, lưu connection string đầy đủ bằng User Secrets (không nhập mật khẩu vào file cấu hình):

```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1434;Database=booksalescore;User ID=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Thay `<SQL_PASSWORD>` bằng mật khẩu SQL Login trên máy của bạn. Có thể dùng biến môi trường `ConnectionStrings__DefaultConnection` thay User Secrets. Không commit mật khẩu hay connection string có thông tin xác thực vào source control.

## Chạy

Sau khi SQL Server được cấu hình và chạy:

```sh
dotnet restore
dotnet build
dotnet run
```

Ứng dụng không tự tạo hoặc thay đổi schema khi khởi động; hãy chạy script SQL ở trên trước.

Tài khoản được lưu trong hai bảng `AppUsers` và `AppRoles` theo các model `AppUser`/`AppRole`, không dùng ASP.NET Identity stores hoặc `UserManager`/`SignInManager`. Mật khẩu được băm bằng `PasswordHasher`; ứng dụng giữ đăng nhập bằng cookie HttpOnly và kiểm tra trạng thái/vai trò tài khoản trong cơ sở dữ liệu.

## Phạm vi MVP

Luồng nghiệp vụ gồm sách → khách hàng → báo giá → xác nhận thành đơn bán hàng → báo cáo. Kho, tồn kho, thanh toán, thuế, giao hàng và API/JWT chưa thuộc MVP. Giao diện quản trị dùng ngôn ngữ tiếng Việt, VND và múi giờ `Asia/Ho_Chi_Minh`.

## Dữ liệu kiểm thử

`database/seed-test-data.sql` là file SQL duy nhất để tạo schema và dữ liệu mẫu có tiền tố `TST-` cho tìm kiếm, lọc và phân trang.
