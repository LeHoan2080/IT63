using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookSalesCore.Data;

public enum SqlServerSequence
{
    BookCode,
    CustomerCode,
    SalesOrderNumber
}

public static class SqlServerSequenceExtensions
{
    public static async Task<long> NextValueAsync(
        this DatabaseFacade database,
        SqlServerSequence sequence,
        CancellationToken cancellationToken = default)
    {
        var sequenceName = sequence switch
        {
            SqlServerSequence.BookCode => "BookCodeSequence",
            SqlServerSequence.CustomerCode => "CustomerCodeSequence",
            SqlServerSequence.SalesOrderNumber => "SalesOrderNumberSequence",
            _ => throw new ArgumentOutOfRangeException(nameof(sequence))
        };

        var connection = database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            if (database.CurrentTransaction is { } currentTransaction)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

            command.CommandText = $"SELECT NEXT VALUE FOR dbo.[{sequenceName}];";

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(value);
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
