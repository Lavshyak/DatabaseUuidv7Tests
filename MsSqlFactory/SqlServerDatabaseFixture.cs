using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace MsSqlFactory;

public sealed class SqlServerDatabaseFixture : IAsyncDisposable
{
    private MsSqlContainer? _dbContainer;
    private string? _connectionString;

    //public const int Port = 5985; // Run1
    //public const int Port = 5986; // Run2
    //public const int Port = 5987; // Run3
    public const int Port = 5988; // Run3UnitTest1
    public const string Password = "YourStrong!Passw0rd";
    //public const string ContainerName = "MsSqlGuidTest-1"; // Run1
    //ublic const string ContainerName = "MsSqlGuidTest-2"; // Run2
    //public const string ContainerName = "MsSqlGuidTest-3"; // Run3
    public const string ContainerName = "MsSqlGuidTest-3-UnitTest-1"; // Run3UnitTest1

    public SqlServerDatabaseFixture()
    {
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine($"ContainerName: {ContainerName}");
        using var docker = new DockerClientConfiguration().CreateClient();
        var containers = await docker.Containers.ListContainersAsync(
            new ContainersListParameters { All = true });
        var existing = containers.FirstOrDefault(c =>
            c.Names.Any(n => n.Trim('/') == ContainerName));

        if (existing != null)
        {
            if (!string.Equals(existing.State, "running", StringComparison.OrdinalIgnoreCase))
            {
                await docker.Containers.StartContainerAsync(
                    existing.ID,
                    new ContainerStartParameters());
                await Task.Delay(5000);
            }
        }
        else
        {
            _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPortBinding(Port, 1433)
                .WithPassword(Password)
                .WithAutoRemove(false)
                .WithCleanUp(false)
                .WithCreateParameterModifier(parameters =>
                {
                    parameters.HostConfig ??= new HostConfig();
                    parameters.HostConfig.CPUCount = 6;
                })
                .WithName(ContainerName)
                .Build();

            await _dbContainer.StartAsync();

            var generatedConnectionString = _dbContainer.GetConnectionString();
            Console.WriteLine($"Generated connection string: {generatedConnectionString}");
        }

        _connectionString =
            $"Server=localhost,{Port};Database=dotnet;User Id=sa;Password={Password};TrustServerCertificate=True;";
        Console.WriteLine($"Hardcoded connection string: {_connectionString}");

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
        }
    }

    public MyDbContext CreateDbContext()
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
        }

        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new MyDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.UuidTable.ExecuteDeleteAsync();
    }
}