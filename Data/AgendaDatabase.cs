using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoWpfApp.Data;

internal sealed class AgendaDatabase
{
    private readonly string _databasePath;

    public AgendaDatabase(string? databasePath = null)
    {
        _databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? AgendaDbContext.DefaultDatabasePath
            : databasePath;

        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    public AgendaDbContext CreateContext()
    {
        return new AgendaDbContext(_databasePath);
    }

    public async Task MigrateAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }
}
