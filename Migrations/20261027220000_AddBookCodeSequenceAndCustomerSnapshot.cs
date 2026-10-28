using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSalesCore.Migrations;

public partial class AddBookCodeSequenceAndCustomerSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence<long>(
            name: "BookCodeSequence");

        migrationBuilder.Sql("""
            DECLARE @maxBookCode bigint =
            (
                SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([SKU], 4, 50)))
                FROM [Books]
                WHERE [SKU] LIKE 'BK-%'
            );
            DECLARE @bookSequenceValue bigint =
            (
                SELECT TRY_CONVERT(bigint, [current_value])
                FROM sys.sequences
                WHERE [name] = 'BookCodeSequence'
                    AND SCHEMA_NAME([schema_id]) = N'dbo'
            );
            DECLARE @nextBookCode bigint =
                CASE WHEN ISNULL(@bookSequenceValue, 0) > ISNULL(@maxBookCode, 0)
                    THEN ISNULL(@bookSequenceValue, 0) + 1
                    ELSE ISNULL(@maxBookCode, 0) + 1
                END;
            DECLARE @sequenceSql nvarchar(200) =
                N'ALTER SEQUENCE [dbo].[BookCodeSequence] RESTART WITH '
                + CONVERT(nvarchar(30), @nextBookCode);
            EXEC sys.sp_executesql @sequenceSql;

            DECLARE @maxCustomerCode bigint =
            (
                SELECT MAX(TRY_CONVERT(bigint, SUBSTRING([CustomerCode], 5, 50)))
                FROM [Customers]
                WHERE [CustomerCode] LIKE 'CUS-%'
            );
            DECLARE @customerSequenceValue bigint =
            (
                SELECT TRY_CONVERT(bigint, [current_value])
                FROM sys.sequences
                WHERE [name] = 'CustomerCodeSequence'
                    AND SCHEMA_NAME([schema_id]) = N'dbo'
            );
            DECLARE @nextCustomerCode bigint =
                CASE WHEN ISNULL(@customerSequenceValue, 0) > ISNULL(@maxCustomerCode, 0)
                    THEN ISNULL(@customerSequenceValue, 0) + 1
                    ELSE ISNULL(@maxCustomerCode, 0) + 1
                END;
            SET @sequenceSql =
                N'ALTER SEQUENCE [dbo].[CustomerCodeSequence] RESTART WITH '
                + CONVERT(nvarchar(30), @nextCustomerCode);
            EXEC sys.sp_executesql @sequenceSql;
            """);

        migrationBuilder.AddColumn<string>(
            name: "CustomerNameSnapshot",
            table: "SalesOrders",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Note",
            table: "SalesOrders",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE [SalesOrders]
            SET [CustomerNameSnapshot] = [Customers].[CustomerName]
            FROM [SalesOrders]
            INNER JOIN [Customers]
                ON [SalesOrders].[CustomerId] = [Customers].[CustomerId];
            """);

        migrationBuilder.AlterColumn<string>(
            name: "CustomerNameSnapshot",
            table: "SalesOrders",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1
                FROM [Customers]
                WHERE [CustomerCode] = 'CUS-WALKIN'
            )
            BEGIN
                INSERT INTO [Customers]
                    ([CustomerCode], [CustomerName], [IsActive], [CreatedAt])
                VALUES
                    ('CUS-WALKIN', N'Khách lẻ', 1, SYSUTCDATETIME());
            END;
            UPDATE [Customers]
            SET [IsActive] = 1
            WHERE [CustomerCode] = 'CUS-WALKIN';
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Customers_WalkIn_Active",
            table: "Customers",
            sql: "[CustomerCode] <> 'CUS-WALKIN' OR [IsActive] = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Customers_WalkIn_Active",
            table: "Customers");

        migrationBuilder.DropColumn(
            name: "CustomerNameSnapshot",
            table: "SalesOrders");

        migrationBuilder.DropColumn(
            name: "Note",
            table: "SalesOrders");

        migrationBuilder.DropSequence(
            name: "BookCodeSequence");
    }
}
