/*
    Synthetic data for BookSalesCore UI and workflow testing.

    Run only after applying all EF Core migrations, against the booksalescore
    test database. This script is additive, repeatable, and does not delete or
    modify existing records. All inserted records use the TST- prefix.

    Creates:
      - 160 books (with active and inactive examples)
      - 125 customers (with active and inactive examples)
      - 240 sales documents spanning Draft, Sent, Confirmed, and Cancelled
      - 480 sales-order lines
      - Audit events for catalog and sales data

    User accounts/passwords are managed by ASP.NET Core Identity and are not
    inserted by this script. An existing Administrator is required to own the
    generated test records.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'booksalescore'
BEGIN
    THROW 51000, 'Select the booksalescore test database before running this script.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @TestActorUserId nvarchar(450);

    SELECT TOP (1) @TestActorUserId = u.Id
    FROM dbo.AspNetUsers AS u
    INNER JOIN dbo.AspNetUserRoles AS ur ON ur.UserId = u.Id
    INNER JOIN dbo.AspNetRoles AS r ON r.Id = ur.RoleId
    WHERE r.NormalizedName = N'ADMINISTRATOR'
    ORDER BY u.UserName;

    IF @TestActorUserId IS NULL
    BEGIN
        THROW 51001, 'Create an Administrator account through the application before seeding sales test data.', 1;
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
        CASE WHEN seed.Status <> N'Draft' THEN @TestActorUserId END,
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
        CASE WHEN seed.Status = N'Cancelled' THEN @TestActorUserId END,
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
