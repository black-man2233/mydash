using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyDash.Domain.Entities;
using MyDash.Infrastructure.Data;
using MyDash.Infrastructure.Repositories;

namespace MyDash.Infrastructure.Tests;

// Uses SQLite in-memory for fast unit tests; production uses SQL Server.
public class EfServerRepositoryTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;

    public EfServerRepositoryTests()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        _conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .Options;
        _db = new AppDbContext(opts);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task Adds_and_retrieves_server()
    {
        var sut = new EfServerRepository(_db);
        var server = Server.NewEnrolled("hp", "HP ProLiant", "hp.tailnet.ts.net", "Ubuntu 24.04");

        await sut.AddAsync(server);
        var loaded = await sut.GetByIdAsync(server.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("hp");
        loaded.FullName.Should().Be("HP ProLiant");
    }

    [Fact]
    public async Task Updates_server_status()
    {
        var sut = new EfServerRepository(_db);
        var server = Server.NewEnrolled("test", "Test", "test.ts.net", "Linux");
        await sut.AddAsync(server);

        server.Status = ServerStatus.Up;
        server.CpuPercent = 42.5;
        await sut.UpdateAsync(server);

        var loaded = await sut.GetByIdAsync(server.Id);
        loaded!.Status.Should().Be(ServerStatus.Up);
        loaded.CpuPercent.Should().BeApproximately(42.5, 0.01);
    }

    [Fact]
    public async Task Deletes_server()
    {
        var sut = new EfServerRepository(_db);
        var server = Server.NewEnrolled("del", "Delete Me", "del.ts.net", "Linux");
        await sut.AddAsync(server);

        await sut.DeleteAsync(server.Id);
        var loaded = await sut.GetByIdAsync(server.Id);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task Lists_all_servers()
    {
        var sut = new EfServerRepository(_db);
        await sut.AddAsync(Server.NewEnrolled("a", "A", "a.ts.net", "Linux"));
        await sut.AddAsync(Server.NewEnrolled("b", "B", "b.ts.net", "Linux"));

        var all = await sut.ListAllAsync();
        all.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
