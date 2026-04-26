using System.Collections.Immutable;
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

    /*
            э ну кароче вверху в мс скл от старшего к младшему:
                mg5 ->,       mg4 ->,    mg3 <-,    mg2 <-, mg1 <-
            g1  ->, g2 ->,    g3  ->,    g4  ->,       g5  ->
            - внизу у нормального uuid в нормальной среде от старшего к младшему
         */

    private static readonly IReadOnlyCollection<IReadOnlyCollection<byte>> MsSqlFromSmallToBigUuidsBytes =
        new byte[16][]
        {
            // ORDER BY uuid ASC должен вернуть из мс скл в такой же последовательности
            //             0-3         4-5   6-7   8-9   10-15    
            new byte[16] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
        };

    private static readonly IReadOnlyCollection<string> MsSqlFromSmallToBigUuidsShortHexes =
        MsSqlFromSmallToBigUuidsBytes.Select(b => Convert.ToHexString(b.ToArray())).ToArray();
    
    private static readonly IReadOnlyCollection<Guid> MsSqlFromSmallToBigGuids =
        MsSqlFromSmallToBigUuidsShortHexes.Select(s => new Guid(s)).ToArray();
    
    private static readonly IReadOnlyCollection<IReadOnlyCollection<byte>> NormalFromSmallToBigUuidsBytes =
        new byte[16][]
        {
            // нормальные UUID в нормальной среде должны сортироваться по возрастанию в такой же последовательности
            //             0-3         4-5   6-7   8-9   10-15    
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },

            new byte[16] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[16] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        };

    private static readonly IReadOnlyCollection<string> NormalFromSmallToBigUuidsShortHexes =
        NormalFromSmallToBigUuidsBytes.Select(b => Convert.ToHexString(b.ToArray())).ToArray();
    
    private static readonly IReadOnlyCollection<Guid> NormalFromSmallToBigGuids =
        NormalFromSmallToBigUuidsShortHexes.Select(s => new Guid(s)).ToArray();

    [Fact]
    public void NormalFromSmallToBigGuidsIsHardcodedRight()
    {
        
        var expectedNormalGuids = NormalFromSmallToBigGuids.ToArray();
        var sortedGuids = expectedNormalGuids.ToArray();
        Array.Sort(sortedGuids);
        Assert.Equal(expectedNormalGuids.Length, sortedGuids.Length);
        for (int i = 0; i < sortedGuids.Length; i++)
        {
            var expected = expectedNormalGuids[i];
            var actual = sortedGuids[i];
            Assert.Equal(expected, actual);
            Assert.Equal(expected.ToString(), actual.ToString());
        }
    }
    
    [Fact]
    public async Task MsSqlFromSmallToBigGuidsIsHardcodedRight()
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

        Guid[] expectedMsSqlGuids = MsSqlFromSmallToBigGuids.ToArray();

        _testOutputHelper.WriteLine(
            $"{nameof(expectedMsSqlGuids)}:\n{string.Join("\n", expectedMsSqlGuids.Select(g => g.ToString()))}\n\n");

        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            if (transaction is not SqlTransaction sqlTransaction)
            {
                throw new InvalidOperationException();
            }

            {
                Guid[] uuids = expectedMsSqlGuids;
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
            // language=c# схему лень выбирать
            cmd.CommandText =
                "select uuid, [order], LOWER(CAST(uuid AS VARCHAR(36))) AS LowercaseGuid from uuids order by uuid ASC";
            await using var reader = await cmd.ExecuteReaderAsync();

            _testOutputHelper.WriteLine($"returned guids:");
            for (int i = 0; i < expectedMsSqlGuids.Length; i++)
            {
                var initialGuid = expectedMsSqlGuids[i];

                Assert.True(await reader.ReadAsync());
                Assert.Equal(i, reader.GetInt64(1));

                var returnedGuid = reader.GetGuid(0);
                _testOutputHelper.WriteLine(returnedGuid.ToString());
                Assert.Equal(initialGuid, returnedGuid);
                Assert.Equal(initialGuid.ToString(), returnedGuid.ToString());

                var returnedGuidText = reader.GetString(2);
                Assert.Equal(initialGuid.ToString(), returnedGuidText);
            }

            Assert.False(await reader.ReadAsync());
        }
    }

    [Fact]
    public async Task GuidExtensionsSwapToMsSqlAndFromSqlWorksRight()
    {
        Guid[] expectedGuidsMsSql = MsSqlFromSmallToBigGuids.ToArray();

        Guid[] guids = NormalFromSmallToBigGuids.ToArray();

        var swappedToMsSqlGuids = guids.Select(g => g.SwapToMsSqlServer()).ToArray();
        var swappedFromMsSqlGuids = swappedToMsSqlGuids.Select(g => g.SwapFromMsSqlServer()).ToArray();

        Assert.True(16 == expectedGuidsMsSql.Length &&
                    16 == guids.Length &&
                    16 == swappedToMsSqlGuids.Length &&
                    16 == swappedFromMsSqlGuids.Length);

        for (int i = 0; i < 16; i++)
        {
            _testOutputHelper.WriteLine($"i: {i}");
            var expectedGuidMsSql = expectedGuidsMsSql[i];
            var guid = guids[i];
            var swappedToMsSqlGuid = swappedToMsSqlGuids[i];
            var swappedFromMsSqlGuid = swappedFromMsSqlGuids[i];

            Assert.Equal(expectedGuidMsSql, swappedToMsSqlGuid);
            Assert.Equal(expectedGuidMsSql.ToString(), swappedToMsSqlGuid.ToString());
            Assert.Equal(guid, swappedFromMsSqlGuid);
            Assert.Equal(guid.ToString(), swappedFromMsSqlGuid.ToString());
        }
    }
}