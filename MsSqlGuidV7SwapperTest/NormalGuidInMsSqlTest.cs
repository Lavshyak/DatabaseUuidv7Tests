using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using MsSqlFactory;
using MsSqlTest;
using Xunit.Abstractions;

namespace MsSqlGuidV7SwapperTest;

public class NormalGuidInMsSqlTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public NormalGuidInMsSqlTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static readonly string ConnectionString =
        $"Data Source=\"tcp:localhost, {SqlServerDatabaseFixture.Port}\";Initial Catalog=dotnet;User ID=sa;Password={SqlServerDatabaseFixture.Password};Trust Server Certificate=True";

    [Fact]
    public async Task Test4()
    {
        _testOutputHelper.WriteLine($"{nameof(ConnectionString)}:\n{ConnectionString}");
        {
            await using var ssdf = new SqlServerDatabaseFixture();
            await ssdf.InitializeAsync();
            await using var dbContext = ssdf.CreateDbContext();
            await ssdf.ResetDatabaseAsync();
        }

        {
            await using var ssdf = new SqlServerDatabaseFixture();
            await ssdf.InitializeAsync();
        }

        var normalGuidHex = MsSqlTest.Program.GenerateUuidV7();
        var normalGuid = new Guid(normalGuidHex);
        var msSqlFriendlyNormalGuid = normalGuid.SwapV7ToMsSqlServer();

        {
            var msSqlFriendlyNormalGuid1Hex = MsSqlTest.Program.ReorderUuid(normalGuidHex);
            var msSqlFriendlyNormalGuid1 = new Guid(msSqlFriendlyNormalGuid1Hex);
            Assert.Equal(msSqlFriendlyNormalGuid, msSqlFriendlyNormalGuid1);
            Assert.Equal(msSqlFriendlyNormalGuid.ToString(), msSqlFriendlyNormalGuid1.ToString());
        }

        _testOutputHelper.WriteLine($"{nameof(msSqlFriendlyNormalGuid)}:\n{msSqlFriendlyNormalGuid.ToString()}");

        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            if (transaction is not SqlTransaction sqlTransaction)
            {
                throw new InvalidOperationException();
            }

            {
                Guid[] uuids = [msSqlFriendlyNormalGuid];
                long alreadyInserted = 0;

                var requestBuilder = new StringBuilder("insert into uuids (uuid, [order]) VALUES ");
                var firstRecord = true;
                for (int i = 0; i < uuids.Length; i++)
                {
                    if (firstRecord)
                    {
                        requestBuilder.Append($"(@u{i},@o{i})");
                        firstRecord = false;
                    }
                    else
                    {
                        requestBuilder.Append($",(@u{i},@o{i})");
                    }
                }

                requestBuilder.Append(';');
                var sql = requestBuilder.ToString();
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = sqlTransaction;
                cmd.CommandText = sql;
                for (int i = 0; i < uuids.Length; i++)
                {
                    var uuid = uuids[i];
                    var order = alreadyInserted + i;
                    cmd.Parameters.AddWithValue($"@u{i}", uuid);
                    cmd.Parameters.AddWithValue($"@o{i}", order);
                }

                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "select uuid, [order], LOWER(CAST(uuid AS VARCHAR(36))) AS LowercaseGuid from uuids order by uuid ASC;";
            await using var reader = await cmd.ExecuteReaderAsync();
            
            Assert.True(await reader.ReadAsync());
            Assert.Equal(0, reader.GetInt64(1));
            
            var returnedGuid = reader.GetGuid(0);
            _testOutputHelper.WriteLine($"{nameof(returnedGuid)}:\n{returnedGuid.ToString()}");
            Assert.Equal(msSqlFriendlyNormalGuid, returnedGuid);
            Assert.Equal(msSqlFriendlyNormalGuid.ToString(), returnedGuid.ToString());
            
            var returnedGuidText = reader.GetString(2);
            _testOutputHelper.WriteLine($"{nameof(returnedGuidText)}:\n{returnedGuidText.ToString()}");
            Assert.Equal(msSqlFriendlyNormalGuid.ToString(), returnedGuidText);
            
            Assert.False(await reader.ReadAsync());
        }
    }
}