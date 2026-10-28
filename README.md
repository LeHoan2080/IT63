# Book Sales Core

Ứng dụng MVC quản lý danh mục sách, khách hàng, báo giá/đơn bán và báo cáo doanh số.

## Yêu cầu

- .NET 10 SDK
- SQL Server 2022 hoặc Azure SQL tương thích, đang chạy và có quyền tạo/cập nhật database

Mặc định, ứng dụng dùng SQL Server tại `localhost`, database `booksalescore` và Windows Integrated Authentication. Nếu SQL Server dùng địa chỉ, cổng hoặc tài khoản khác, cấu hình `ConnectionStrings:DefaultConnection` bằng User Secrets hoặc biến môi trường `ConnectionStrings__DefaultConnection`. Không commit mật khẩu hay connection string có thông tin xác thực vào source control.

## Chạy

Sau khi SQL Server được cấu hình và chạy:

```sh
dotnet restore
dotnet build
dotnet run
```

Khi khởi động, ứng dụng tự áp dụng các EF Core migration lên database. Không xóa thư mục `Migrations`: đây là schema versioning cần thiết để database mới được tạo và database hiện hữu được cập nhật mà không phải chạy lệnh migration thủ công.

Để tạo quản trị viên đầu tiên, cấu hình `BootstrapAdmin__UserName`, `BootstrapAdmin__Email` và `BootstrapAdmin__Password` bằng User Secrets hoặc biến môi trường. Mật khẩu bootstrap không được lưu trong repository; tài khoản mới phải đổi mật khẩu ở lần đăng nhập đầu. Gỡ cấu hình bootstrap sau khi tạo tài khoản.

## Phạm vi MVP

Luồng nghiệp vụ gồm sách → khách hàng → báo giá → xác nhận thành đơn bán hàng → báo cáo. Kho, tồn kho, thanh toán, thuế, giao hàng và API/JWT chưa thuộc MVP. Giao diện quản trị dùng ngôn ngữ tiếng Việt, VND và múi giờ `Asia/Ho_Chi_Minh`.

## Dữ liệu kiểm thử

`scripts/seed-test-data.sql` tạo dữ liệu mẫu có tiền tố `TST-` để thử tìm kiếm, lọc và phân trang. Chạy script thủ công trên database kiểm thử sau khi đã khởi tạo ứng dụng; script yêu cầu có ít nhất một Administrator và có thể chạy lặp lại.
