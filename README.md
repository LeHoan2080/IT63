# Book Sales Core

Ứng dụng MVC quản lý danh mục sách, khách hàng, báo giá/đơn bán và báo cáo doanh số.

## Yêu cầu

- .NET 10 SDK
- SQL Server 2022 hoặc mới hơn, đang chạy

## Khởi tạo database

Mở [scripts/seed-test-data.sql](./scripts/seed-test-data.sql) trong SQL Server Management Studio và chạy toàn bộ file (hoặc dùng `sqlcmd`). Script sẽ tạo database `booksalescore`, toàn bộ bảng/sequence và dữ liệu demo. Nếu database đã có schema đầy đủ, script chỉ bổ sung các bản ghi demo còn thiếu; nếu schema tồn tại nhưng thiếu/cũ, script dừng và báo lỗi thay vì tự ý nâng cấp. Chạy lại script không tạo thêm các bản ghi `TST-`.

Tài khoản Administrator được tạo từ SQL:

| Vai trò | Tên đăng nhập | Mật khẩu tạm |
|---|---|---|
| Administrator | `admin` | `Password!23` |

Tài khoản yêu cầu đổi mật khẩu ở lần đăng nhập đầu. Chỉ dùng cho môi trường phát triển/kiểm thử; không chạy script hoặc dùng tài khoản này trên production.

Ứng dụng mặc định kết nối SQL Server tại `localhost`, database `booksalescore` bằng Windows Integrated Authentication. Nếu SQL Server dùng địa chỉ, cổng hoặc tài khoản khác (ví dụ SQL Server trong Docker), cấu hình `ConnectionStrings:DefaultConnection` bằng User Secrets hoặc biến môi trường `ConnectionStrings__DefaultConnection`. Không commit mật khẩu hay connection string có thông tin xác thực vào source control.

## Chạy

Sau khi SQL Server được cấu hình và chạy:

```sh
dotnet restore
dotnet build
dotnet run
```

Ứng dụng không tự tạo hoặc thay đổi schema khi khởi động; hãy chạy script SQL ở trên trước.

## Phạm vi MVP

Luồng nghiệp vụ gồm sách → khách hàng → báo giá → xác nhận thành đơn bán hàng → báo cáo. Kho, tồn kho, thanh toán, thuế, giao hàng và API/JWT chưa thuộc MVP. Giao diện quản trị dùng ngôn ngữ tiếng Việt, VND và múi giờ `Asia/Ho_Chi_Minh`.

## Dữ liệu kiểm thử

`scripts/seed-test-data.sql` là file SQL duy nhất để tạo schema và dữ liệu mẫu có tiền tố `TST-` cho tìm kiếm, lọc và phân trang.
