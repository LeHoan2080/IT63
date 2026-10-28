/*
    One-shot SQL Server setup for BookSalesCore.

    Run this entire file in SQL Server Management Studio or sqlcmd while
    connected to a SQL Server instance. It creates the booksalescore database
    and all application tables when missing, then inserts demo login accounts
    and repeatable TST- sample data.

    Demo Administrator account (must change password after first sign-in):
      Username: admin
      Password: Password!23

    Intended for local/test databases only. Do not use this account in
    production. This script does not delete or modify existing business rows.

    Creates:
      - 160 books (with active and inactive examples)
      - 125 customers (with active and inactive examples)
      - 240 sales documents spanning Draft, Sent, Confirmed, and Cancelled
      - 480 sales-order lines
      - Audit events for catalog and sales data
*/

SET NOCOUNT ON;

IF DB_ID(N'booksalescore') IS NULL
BEGIN
    CREATE DATABASE [booksalescore];
END;
GO

USE [booksalescore];
GO

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        CREATE TABLE [dbo].[AspNetRoles] (
            [Id] nvarchar(450) NOT NULL,
            [Name] nvarchar(256) NULL,
            [NormalizedName] nvarchar(256) NULL,
            [ConcurrencyStamp] nvarchar(max) NULL,
            CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
        );

        CREATE TABLE [dbo].[AspNetUsers] (
            [Id] nvarchar(450) NOT NULL,
            [MustChangePassword] bit NOT NULL,
            [UserName] nvarchar(256) NULL,
            [NormalizedUserName] nvarchar(256) NULL,
            [Email] nvarchar(256) NULL,
            [NormalizedEmail] nvarchar(256) NULL,
            [EmailConfirmed] bit NOT NULL,
            [PasswordHash] nvarchar(max) NULL,
            [SecurityStamp] nvarchar(max) NULL,
            [ConcurrencyStamp] nvarchar(max) NULL,
            [PhoneNumber] nvarchar(max) NULL,
            [PhoneNumberConfirmed] bit NOT NULL,
            [TwoFactorEnabled] bit NOT NULL,
            [LockoutEnd] datetimeoffset NULL,
            [LockoutEnabled] bit NOT NULL,
            [AccessFailedCount] int NOT NULL,
            CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
        );

        CREATE TABLE [dbo].[AspNetRoleClaims] (
            [Id] int NOT NULL IDENTITY,
            [RoleId] nvarchar(450) NOT NULL,
            [ClaimType] nvarchar(max) NULL,
            [ClaimValue] nvarchar(max) NULL,
            CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
                FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
        );

        CREATE TABLE [dbo].[AspNetUserClaims] (
            [Id] int NOT NULL IDENTITY,
            [UserId] nvarchar(450) NOT NULL,
            [ClaimType] nvarchar(max) NULL,
            [ClaimValue] nvarchar(max) NULL,
            CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
                FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
        );

        CREATE TABLE [dbo].[AspNetUserLogins] (
            [LoginProvider] nvarchar(128) NOT NULL,
            [ProviderKey] nvarchar(128) NOT NULL,
            [ProviderDisplayName] nvarchar(max) NULL,
            [UserId] nvarchar(450) NOT NULL,
            CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
            CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
                FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
        );

        CREATE TABLE [dbo].[AspNetUserRoles] (
            [UserId] nvarchar(450) NOT NULL,
            [RoleId] nvarchar(450) NOT NULL,
            CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
            CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
                FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
            CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
                FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
        );

        CREATE TABLE [dbo].[AspNetUserTokens] (
            [UserId] nvarchar(450) NOT NULL,
            [LoginProvider] nvarchar(128) NOT NULL,
            [Name] nvarchar(128) NOT NULL,
            [Value] nvarchar(max) NULL,
            CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
            CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
                FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
        );

        CREATE TABLE [dbo].[Books] (
            [BookId] bigint NOT NULL IDENTITY,
            [SKU] nvarchar(50) NOT NULL,
            [ISBN] nvarchar(20) NULL,
            [BookTitle] nvarchar(300) NOT NULL,
            [CategoryName] nvarchar(150) NULL,
            [SalePrice] decimal(18, 0) NOT NULL,
            [IsActive] bit NOT NULL CONSTRAINT [DF_Books_IsActive] DEFAULT (1),
            [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Books_CreatedAt] DEFAULT (SYSUTCDATETIME()),
            [UpdatedAt] datetime2 NULL,
            CONSTRAINT [PK_Books] PRIMARY KEY ([BookId]),
            CONSTRAINT [CK_Books_SalePrice_NonNegative] CHECK ([SalePrice] >= 0)
        );

        CREATE TABLE [dbo].[Customers] (
            [CustomerId] bigint NOT NULL IDENTITY,
            [CustomerCode] nvarchar(30) NOT NULL,
            [CustomerName] nvarchar(200) NOT NULL,
            [Phone] nvarchar(30) NULL,
            [Email] nvarchar(255) NULL,
            [Address] nvarchar(500) NULL,
            [IsActive] bit NOT NULL CONSTRAINT [DF_Customers_IsActive] DEFAULT (1),
            [CreatedAt] datetime2 NOT NULL,
            [UpdatedAt] datetime2 NULL,
            CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId]),
            CONSTRAINT [CK_Customers_WalkIn_Active]
                CHECK ([CustomerCode] <> N'CUS-WALKIN' OR [IsActive] = 1)
        );

        CREATE TABLE [dbo].[AuditLogs] (
            [AuditLogId] bigint NOT NULL IDENTITY,
            [UserId] nvarchar(450) NULL,
            [Action] nvarchar(30) NOT NULL,
            [EntityName] nvarchar(50) NOT NULL,
            [EntityId] nvarchar(450) NOT NULL,
            [OldValue] nvarchar(1000) NULL,
            [NewValue] nvarchar(1000) NULL,
            [Reason] nvarchar(500) NULL,
            [CreatedAt] datetime2 NOT NULL,
            CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
        );

        CREATE TABLE [dbo].[SalesOrders] (
            [SalesOrderId] bigint NOT NULL IDENTITY,
            [OrderNumber] nvarchar(30) NOT NULL,
            [OrderDate] datetime2 NOT NULL,
            [CustomerId] bigint NOT NULL,
            [CustomerNameSnapshot] nvarchar(200) NOT NULL,
            [CreatedByUserId] nvarchar(450) NOT NULL,
            [CreatedAt] datetime2 NOT NULL,
            [UpdatedAt] datetime2 NULL,
            [Status] nvarchar(20) NOT NULL,
            [Note] nvarchar(1000) NULL,
            [TotalAmount] decimal(18, 0) NOT NULL,
            [SentByUserId] nvarchar(450) NULL,
            [SentAt] datetime2 NULL,
            [ConfirmedByUserId] nvarchar(450) NULL,
            [ConfirmedAt] datetime2 NULL,
            [CancelledByUserId] nvarchar(450) NULL,
            [CancelledAt] datetime2 NULL,
            [CancellationReason] nvarchar(500) NULL,
            [RowVersion] rowversion NOT NULL,
            CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([SalesOrderId]),
            CONSTRAINT [CK_SalesOrders_Status]
                CHECK ([Status] IN (N'Draft', N'Sent', N'Confirmed', N'Cancelled')),
            CONSTRAINT [CK_SalesOrders_StatusTimestamps]
                CHECK (([Status] <> N'Sent' OR [SentAt] IS NOT NULL)
                    AND ([Status] <> N'Confirmed' OR [ConfirmedAt] IS NOT NULL)
                    AND ([Status] <> N'Cancelled' OR ([CancelledAt] IS NOT NULL AND [CancellationReason] IS NOT NULL))),
            CONSTRAINT [CK_SalesOrders_TotalAmount] CHECK ([TotalAmount] >= 0),
            CONSTRAINT [FK_SalesOrders_Customers_CustomerId]
                FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([CustomerId]) ON DELETE NO ACTION
        );

        CREATE TABLE [dbo].[SalesOrderLines] (
            [SalesOrderLineId] bigint NOT NULL IDENTITY,
            [SalesOrderId] bigint NOT NULL,
            [BookId] bigint NOT NULL,
            [SKUSnapshot] nvarchar(30) NOT NULL,
            [BookTitleSnapshot] nvarchar(200) NOT NULL,
            [Quantity] int NOT NULL,
            [UnitPrice] decimal(18, 0) NOT NULL,
            [LineTotal] decimal(18, 0) NOT NULL,
            CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([SalesOrderLineId]),
            CONSTRAINT [CK_SalesOrderLines_LineTotal] CHECK ([LineTotal] = [Quantity] * [UnitPrice]),
            CONSTRAINT [CK_SalesOrderLines_Quantity] CHECK ([Quantity] > 0),
            CONSTRAINT [CK_SalesOrderLines_UnitPrice] CHECK ([UnitPrice] >= 0),
            CONSTRAINT [FK_SalesOrderLines_Books_BookId]
                FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([BookId]) ON DELETE NO ACTION,
            CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId]
                FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrders] ([SalesOrderId]) ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName])
            WHERE [NormalizedName] IS NOT NULL;
        CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId]);
        CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId]);
        CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId]);
        CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId]);
        CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail]);
        CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName])
            WHERE [NormalizedUserName] IS NOT NULL;
        CREATE UNIQUE INDEX [IX_Books_SKU] ON [dbo].[Books] ([SKU]);
        CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [dbo].[Customers] ([CustomerCode]);
        CREATE INDEX [IX_AuditLogs_EntityName_EntityId_CreatedAt]
            ON [dbo].[AuditLogs] ([EntityName], [EntityId], [CreatedAt]);
        CREATE INDEX [IX_SalesOrderLines_BookId] ON [dbo].[SalesOrderLines] ([BookId]);
        CREATE INDEX [IX_SalesOrderLines_SalesOrderId] ON [dbo].[SalesOrderLines] ([SalesOrderId]);
        CREATE INDEX [IX_SalesOrders_CreatedByUserId_Status_ConfirmedAt]
            ON [dbo].[SalesOrders] ([CreatedByUserId], [Status], [ConfirmedAt]);
        CREATE INDEX [IX_SalesOrders_CreatedByUserId_Status_OrderDate]
            ON [dbo].[SalesOrders] ([CreatedByUserId], [Status], [OrderDate]);
        CREATE INDEX [IX_SalesOrders_CustomerId_OrderDate]
            ON [dbo].[SalesOrders] ([CustomerId], [OrderDate]);
        CREATE UNIQUE INDEX [IX_SalesOrders_OrderNumber] ON [dbo].[SalesOrders] ([OrderNumber]);
        CREATE INDEX [IX_SalesOrders_Status_ConfirmedAt] ON [dbo].[SalesOrders] ([Status], [ConfirmedAt]);
        CREATE INDEX [IX_SalesOrders_Status_OrderDate] ON [dbo].[SalesOrders] ([Status], [OrderDate]);

        CREATE SEQUENCE [dbo].[BookCodeSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;
        CREATE SEQUENCE [dbo].[CustomerCodeSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;
        CREATE SEQUENCE [dbo].[SalesOrderNumberSequence] START WITH 1 INCREMENT BY 1 NO CYCLE;

        INSERT INTO [dbo].[Customers] ([CustomerCode], [CustomerName], [IsActive], [CreatedAt])
        VALUES (N'CUS-WALKIN', N'Khách lẻ', 1, SYSUTCDATETIME());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Books', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Customers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SalesOrders', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SalesOrderLines', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BookCodeSequence', N'SO') IS NULL
   OR OBJECT_ID(N'dbo.CustomerCodeSequence', N'SO') IS NULL
   OR OBJECT_ID(N'dbo.SalesOrderNumberSequence', N'SO') IS NULL
   OR COL_LENGTH(N'dbo.SalesOrders', N'CustomerNameSnapshot') IS NULL
   OR COL_LENGTH(N'dbo.SalesOrders', N'Note') IS NULL
BEGIN
    THROW 51000, 'Database exists but its schema is incomplete or outdated. Use a new empty booksalescore database.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @TestActorUserId nvarchar(450);

    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'ADMINISTRATOR')
    BEGIN
        INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (N'TST-ROLE-ADMIN', N'Administrator', N'ADMINISTRATOR', CONVERT(nvarchar(36), NEWID()));
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'SALESSTAFF')
    BEGIN
        INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (N'TST-ROLE-SALES', N'SalesStaff', N'SALESSTAFF', CONVERT(nvarchar(36), NEWID()));
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedUserName = N'ADMIN')
    BEGIN
        INSERT INTO dbo.AspNetUsers
        (
            Id, MustChangePassword, UserName, NormalizedUserName, Email, NormalizedEmail,
            EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
            PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount
        )
        VALUES
        (
            N'SQL-DEMO-ADMIN', 1, N'admin', N'ADMIN',
            N'admin@example.test', N'ADMIN@EXAMPLE.TEST', 1,
            N'AQAAAAIAAYagAAAAEJ96PYwhtOZQoZfD0ujxBGtffc70ZBp2jdE01TfNNyQ5ybbvpypQFsEbIOnW/KUWoQ==',
            CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
            0, 0, 1, 0
        );
    END;

    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
    SELECT u.Id, r.Id
    FROM dbo.AspNetUsers AS u
    CROSS JOIN dbo.AspNetRoles AS r
    WHERE u.NormalizedUserName = N'ADMIN'
      AND r.NormalizedName = N'ADMINISTRATOR'
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.AspNetUserRoles AS currentRole
          WHERE currentRole.UserId = u.Id AND currentRole.RoleId = r.Id
      );

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'CUS-WALKIN')
    BEGIN
        INSERT INTO dbo.Customers (CustomerCode, CustomerName, IsActive, CreatedAt)
        VALUES (N'CUS-WALKIN', N'Khách lẻ', 1, SYSUTCDATETIME());
    END;

    UPDATE dbo.Customers
    SET IsActive = 1
    WHERE CustomerCode = N'CUS-WALKIN'
      AND IsActive = 0;

    SELECT @TestActorUserId = Id
    FROM dbo.AspNetUsers
    WHERE NormalizedUserName = N'ADMIN';

    IF @TestActorUserId IS NULL
    BEGIN
        THROW 51001, 'The demo Administrator account could not be created.', 1;
    END;

    ;WITH Numbers AS
    (
        SELECT TOP (160)
            CONVERT(int, ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS N
        FROM sys.all_objects AS a
        CROSS JOIN sys.all_objects AS b
    )
    INSERT INTO dbo.Books
        (SKU, ISBN, BookTitle, CategoryName, SalePrice, IsActive, CreatedAt)
    SELECT
        CONCAT('TST-BK-', RIGHT(CONCAT('0000', N), 4)),
        CASE
            WHEN N % 7 = 0 THEN NULL
            ELSE CONCAT('978604', RIGHT(CONCAT('0000000', N), 7))
        END,
        CONCAT(N'Sách kiểm thử ', RIGHT(CONCAT('0000', N), 4)),
        CASE N % 5
            WHEN 0 THEN N'Văn học'
            WHEN 1 THEN N'Thiếu nhi'
            WHEN 2 THEN N'Kỹ năng'
            WHEN 3 THEN N'Lịch sử'
            ELSE N'Khoa học'
        END,
        CONVERT(decimal(18, 0), 25000 + (N * 750)),
        CASE WHEN N % 8 = 0 THEN 0 ELSE 1 END,
        DATEADD(day, -N, SYSUTCDATETIME())
    FROM Numbers
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Books AS existing
        WHERE existing.SKU = CONCAT('TST-BK-', RIGHT(CONCAT('0000', N), 4))
    );

    ;WITH Numbers AS
    (
        SELECT TOP (125)
            CONVERT(int, ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS N
        FROM sys.all_objects AS a
        CROSS JOIN sys.all_objects AS b
    )
    INSERT INTO dbo.Customers
        (CustomerCode, CustomerName, Phone, Email, Address, IsActive, CreatedAt)
    SELECT
        CONCAT('TST-CUS-', RIGHT(CONCAT('0000', N), 4)),
        CONCAT(N'Khách kiểm thử ', RIGHT(CONCAT('0000', N), 4)),
        CONCAT('090', RIGHT(CONCAT('00000000', N), 8)),
        CONCAT('test-customer-', RIGHT(CONCAT('0000', N), 4), '@example.test'),
        CONCAT(N'Địa chỉ kiểm thử số ', N),
        CASE WHEN N % 9 = 0 THEN 0 ELSE 1 END,
        DATEADD(day, -N, SYSUTCDATETIME())
    FROM Numbers
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Customers AS existing
        WHERE existing.CustomerCode = CONCAT('TST-CUS-', RIGHT(CONCAT('0000', N), 4))
    );

    CREATE TABLE #TestOrderSeed
    (
        N int NOT NULL PRIMARY KEY,
        OrderNumber nvarchar(30) NOT NULL,
        CustomerId bigint NOT NULL,
        CustomerName nvarchar(200) NOT NULL,
        BookId1 bigint NOT NULL,
        Sku1 nvarchar(30) NOT NULL,
        Title1 nvarchar(200) NOT NULL,
        Price1 decimal(18, 0) NOT NULL,
        Quantity1 int NOT NULL,
        BookId2 bigint NOT NULL,
        Sku2 nvarchar(30) NOT NULL,
        Title2 nvarchar(200) NOT NULL,
        Price2 decimal(18, 0) NOT NULL,
        Quantity2 int NOT NULL,
        Status nvarchar(20) NOT NULL,
        OrderDate datetime2 NOT NULL
    );

    ;WITH Numbers AS
    (
        SELECT TOP (240)
            CONVERT(int, ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS N
        FROM sys.all_objects AS a
        CROSS JOIN sys.all_objects AS b
    )
    INSERT INTO #TestOrderSeed
    (
        N, OrderNumber, CustomerId, CustomerName,
        BookId1, Sku1, Title1, Price1, Quantity1,
        BookId2, Sku2, Title2, Price2, Quantity2,
        Status, OrderDate
    )
    SELECT
        n.N,
        CONCAT('TST-SO-', RIGHT(CONCAT('000000', n.N), 6)),
        c.CustomerId,
        c.CustomerName,
        b1.BookId,
        b1.SKU,
        b1.BookTitle,
        b1.SalePrice,
        1 + (n.N % 5),
        b2.BookId,
        b2.SKU,
        b2.BookTitle,
        b2.SalePrice,
        1 + ((n.N + 2) % 3),
        CASE n.N % 4
            WHEN 0 THEN N'Draft'
            WHEN 1 THEN N'Sent'
            WHEN 2 THEN N'Confirmed'
            ELSE N'Cancelled'
        END,
        DATEADD(hour, -(n.N % 24), DATEADD(day, -(n.N % 120), SYSUTCDATETIME()))
    FROM Numbers AS n
    INNER JOIN dbo.Customers AS c
        ON c.CustomerCode = CONCAT(
            'TST-CUS-',
            RIGHT(CONCAT('0000', ((n.N - 1) % 125) + 1), 4))
    INNER JOIN dbo.Books AS b1
        ON b1.SKU = CONCAT(
            'TST-BK-',
            RIGHT(CONCAT('0000', ((n.N - 1) % 160) + 1), 4))
    INNER JOIN dbo.Books AS b2
        ON b2.SKU = CONCAT(
            'TST-BK-',
            RIGHT(CONCAT('0000', ((n.N + 21) % 160) + 1), 4));

    INSERT INTO dbo.SalesOrders
    (
        OrderNumber, OrderDate, CustomerId, CustomerNameSnapshot,
        CreatedByUserId, CreatedAt, UpdatedAt, Status, Note, TotalAmount,
        SentByUserId, SentAt, ConfirmedByUserId, ConfirmedAt,
        CancelledByUserId, CancelledAt, CancellationReason
    )
    SELECT
        seed.OrderNumber,
        seed.OrderDate,
        seed.CustomerId,
        seed.CustomerName,
        @TestActorUserId,
        seed.OrderDate,
        NULL,
        seed.Status,
        CONCAT(N'Ghi chú kiểm thử cho chứng từ ', seed.OrderNumber),
        (seed.Price1 * seed.Quantity1) + (seed.Price2 * seed.Quantity2),
        CASE WHEN seed.Status <> N'Draft'
            THEN @TestActorUserId
        END,
        CASE WHEN seed.Status <> N'Draft' THEN DATEADD(minute, 2, seed.OrderDate) END,
        CASE
            WHEN seed.Status = N'Confirmed'
              OR (seed.Status = N'Cancelled' AND seed.N % 8 = 3)
                THEN @TestActorUserId
        END,
        CASE
            WHEN seed.Status = N'Confirmed'
              OR (seed.Status = N'Cancelled' AND seed.N % 8 = 3)
                THEN DATEADD(minute, 5, seed.OrderDate)
        END,
        CASE WHEN seed.Status = N'Cancelled'
            THEN @TestActorUserId
        END,
        CASE WHEN seed.Status = N'Cancelled' THEN DATEADD(minute, 10, seed.OrderDate) END,
        CASE
            WHEN seed.Status = N'Cancelled'
                THEN CONCAT(N'Lý do hủy kiểm thử ', seed.OrderNumber)
        END
    FROM #TestOrderSeed AS seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.SalesOrders AS existing
        WHERE existing.OrderNumber = seed.OrderNumber
    );

    INSERT INTO dbo.SalesOrderLines
        (SalesOrderId, BookId, SKUSnapshot, BookTitleSnapshot, Quantity, UnitPrice, LineTotal)
    SELECT
        orderHeader.SalesOrderId,
        line.BookId,
        line.SKU,
        line.BookTitle,
        line.Quantity,
        line.UnitPrice,
        line.Quantity * line.UnitPrice
    FROM #TestOrderSeed AS seed
    INNER JOIN dbo.SalesOrders AS orderHeader
        ON orderHeader.OrderNumber = seed.OrderNumber
    CROSS APPLY
    (
        VALUES
            (seed.BookId1, seed.Sku1, seed.Title1, seed.Quantity1, seed.Price1),
            (seed.BookId2, seed.Sku2, seed.Title2, seed.Quantity2, seed.Price2)
    ) AS line(BookId, SKU, BookTitle, Quantity, UnitPrice)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.SalesOrderLines AS existing
        WHERE existing.SalesOrderId = orderHeader.SalesOrderId
          AND existing.SKUSnapshot = line.SKU
    );

    CREATE TABLE #TestOrderIds
    (
        N int NOT NULL PRIMARY KEY,
        SalesOrderId bigint NOT NULL,
        OrderNumber nvarchar(30) NOT NULL,
        Status nvarchar(20) NOT NULL,
        OrderDate datetime2 NOT NULL,
        ConfirmedAt datetime2 NULL,
        TotalAmount decimal(18, 0) NOT NULL
    );

    INSERT INTO #TestOrderIds
        (N, SalesOrderId, OrderNumber, Status, OrderDate, ConfirmedAt, TotalAmount)
    SELECT
        seed.N,
        orderHeader.SalesOrderId,
        seed.OrderNumber,
        seed.Status,
        seed.OrderDate,
        CASE
            WHEN seed.Status = N'Confirmed'
              OR (seed.Status = N'Cancelled' AND seed.N % 8 = 3)
                THEN DATEADD(minute, 5, seed.OrderDate)
        END,
        (seed.Price1 * seed.Quantity1) + (seed.Price2 * seed.Quantity2)
    FROM #TestOrderSeed AS seed
    INNER JOIN dbo.SalesOrders AS orderHeader
        ON orderHeader.OrderNumber = seed.OrderNumber;

    INSERT INTO dbo.AuditLogs
        (UserId, Action, EntityName, EntityId, OldValue, NewValue, Reason, CreatedAt)
    SELECT
        @TestActorUserId,
        N'Created',
        N'Book',
        CONVERT(nvarchar(450), book.BookId),
        NULL,
        CONCAT(N'SKU=', book.SKU, N'; Title=', book.BookTitle),
        N'TESTDATA: generated catalog record',
        book.CreatedAt
    FROM dbo.Books AS book
    WHERE book.SKU LIKE 'TST-BK-%'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.AuditLogs AS existing
          WHERE existing.EntityName = N'Book'
            AND existing.EntityId = CONVERT(nvarchar(450), book.BookId)
            AND existing.Reason = N'TESTDATA: generated catalog record'
      );

    INSERT INTO dbo.AuditLogs
        (UserId, Action, EntityName, EntityId, OldValue, NewValue, Reason, CreatedAt)
    SELECT
        @TestActorUserId,
        N'Created',
        N'Customer',
        CONVERT(nvarchar(450), customer.CustomerId),
        NULL,
        CONCAT(N'Code=', customer.CustomerCode, N'; Name=', customer.CustomerName),
        N'TESTDATA: generated catalog record',
        customer.CreatedAt
    FROM dbo.Customers AS customer
    WHERE customer.CustomerCode LIKE 'TST-CUS-%'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.AuditLogs AS existing
          WHERE existing.EntityName = N'Customer'
            AND existing.EntityId = CONVERT(nvarchar(450), customer.CustomerId)
            AND existing.Reason = N'TESTDATA: generated catalog record'
      );

    INSERT INTO dbo.AuditLogs
        (UserId, Action, EntityName, EntityId, OldValue, NewValue, Reason, CreatedAt)
    SELECT
        @TestActorUserId,
        N'Created',
        N'SalesOrder',
        CONVERT(nvarchar(450), orderSeed.SalesOrderId),
        NULL,
        CONCAT(N'Status=Draft; Total=', orderSeed.TotalAmount),
        N'TESTDATA: generated sales document',
        orderSeed.OrderDate
    FROM #TestOrderIds AS orderSeed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.AuditLogs AS existing
        WHERE existing.EntityName = N'SalesOrder'
          AND existing.EntityId = CONVERT(nvarchar(450), orderSeed.SalesOrderId)
          AND existing.Action = N'Created'
          AND existing.Reason = N'TESTDATA: generated sales document'
    );

    INSERT INTO dbo.AuditLogs
        (UserId, Action, EntityName, EntityId, OldValue, NewValue, Reason, CreatedAt)
    SELECT
        @TestActorUserId,
        eventData.Action,
        N'SalesOrder',
        CONVERT(nvarchar(450), orderSeed.SalesOrderId),
        eventData.OldValue,
        eventData.NewValue,
        eventData.Reason,
        eventData.CreatedAt
    FROM #TestOrderIds AS orderSeed
    CROSS APPLY
    (
        VALUES
        (
            N'Sent',
            N'Draft',
            N'Sent',
            N'TESTDATA: generated status event',
            DATEADD(minute, 2, orderSeed.OrderDate),
            CASE WHEN orderSeed.Status IN (N'Sent', N'Confirmed', N'Cancelled') THEN 1 ELSE 0 END
        ),
        (
            N'Confirmed',
            N'Sent',
            N'Confirmed',
            N'TESTDATA: generated status event',
            orderSeed.ConfirmedAt,
            CASE WHEN orderSeed.ConfirmedAt IS NOT NULL THEN 1 ELSE 0 END
        ),
        (
            N'Cancelled',
            orderSeed.Status,
            N'Cancelled',
            N'TESTDATA: generated cancellation event',
            DATEADD(minute, 10, orderSeed.OrderDate),
            CASE WHEN orderSeed.Status = N'Cancelled' THEN 1 ELSE 0 END
        )
    ) AS eventData(Action, OldValue, NewValue, Reason, CreatedAt, ShouldInsert)
    WHERE eventData.ShouldInsert = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.AuditLogs AS existing
          WHERE existing.EntityName = N'SalesOrder'
            AND existing.EntityId = CONVERT(nvarchar(450), orderSeed.SalesOrderId)
            AND existing.Action = eventData.Action
            AND existing.Reason = eventData.Reason
      );

    COMMIT TRANSACTION;

    SELECT N'Books' AS Entity, COUNT_BIG(*) AS SeededRows
    FROM dbo.Books
    WHERE SKU LIKE 'TST-BK-%'
    UNION ALL
    SELECT N'Customers', COUNT_BIG(*)
    FROM dbo.Customers
    WHERE CustomerCode LIKE 'TST-CUS-%'
    UNION ALL
    SELECT N'SalesOrders', COUNT_BIG(*)
    FROM dbo.SalesOrders
    WHERE OrderNumber LIKE 'TST-SO-%'
    UNION ALL
    SELECT N'SalesOrderLines', COUNT_BIG(*)
    FROM dbo.SalesOrderLines AS line
    INNER JOIN dbo.SalesOrders AS orderHeader
        ON orderHeader.SalesOrderId = line.SalesOrderId
    WHERE orderHeader.OrderNumber LIKE 'TST-SO-%'
    UNION ALL
    SELECT N'AuditLogs', COUNT_BIG(*)
    FROM dbo.AuditLogs
    WHERE Reason LIKE N'TESTDATA:%';

    SELECT Status, COUNT_BIG(*) AS TestOrderCount
    FROM dbo.SalesOrders
    WHERE OrderNumber LIKE 'TST-SO-%'
    GROUP BY Status
    ORDER BY Status;

    SELECT
        (
            SELECT COUNT_BIG(*)
            FROM dbo.SalesOrders AS orderHeader
            WHERE orderHeader.OrderNumber LIKE 'TST-SO-%'
              AND orderHeader.TotalAmount <>
                  COALESCE(
                      (
                          SELECT SUM(line.LineTotal)
                          FROM dbo.SalesOrderLines AS line
                          WHERE line.SalesOrderId = orderHeader.SalesOrderId
                      ),
                      0)
        ) AS OrdersWithIncorrectTotal,
        (
            SELECT COUNT_BIG(*)
            FROM dbo.SalesOrders AS orderHeader
            WHERE orderHeader.OrderNumber LIKE 'TST-SO-%'
              AND
              (
                  SELECT COUNT_BIG(*)
                  FROM dbo.SalesOrderLines AS line
                  WHERE line.SalesOrderId = orderHeader.SalesOrderId
              ) <> 2
        ) AS OrdersWithoutTwoLines;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;
    THROW;
END CATCH;
