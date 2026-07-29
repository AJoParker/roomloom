using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RoomLoom.Infrastructure.Persistence;

// Lets `dotnet ef migrations add` build the model without depending on
// Api configuration (the app only registers the DbContext when a real
// connection string is present). The placeholder string is never used
// to connect during migration generation.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RoomLoomDbContext>
{
    public RoomLoomDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RoomLoomDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=RoomLoom;User Id=sa;Password=design-time-only;TrustServerCertificate=True;")
            .Options;
        return new RoomLoomDbContext(options);
    }
}
