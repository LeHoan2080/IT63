-- Thêm vai trò SalesManager (Quản lý bán hàng) vào database đang có, không xóa dữ liệu.
IF NOT EXISTS (SELECT 1 FROM dbo.AppRoles WHERE Code = N'SalesManager')
BEGIN
    INSERT INTO dbo.AppRoles (Id, Code, Name, IsActive)
    VALUES (N'SQL-ROLE-SALES-MANAGER', N'SalesManager', N'SalesManager', 1);
END;
