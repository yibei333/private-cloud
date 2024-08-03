using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PrivateCloud.Server.Data.Entity;

namespace PrivateCloud.Server.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CS_AS");
    }

    public DbSet<UserEntity> User { get; set; }
    public DbSet<MediaLibEntity> MediaLib { get; set; }
    public DbSet<FavoriteEntity> Favorite { get; set; }
    public DbSet<ForeverRecordEntity> ForeverRecord { get; set; }
    public DbSet<HistoryEntity> History { get; set; }
    public DbSet<ThumbEntity> Thumb { get; set; }
    public DbSet<ThumbTaskEntity> ThumbTask { get; set; }
    public DbSet<CryptoTaskEntity> CryptoTask { get; set; }
    public DbSet<EncryptedFileEntity> EncryptedFile { get; set; }
}

public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlite();
        return new DataContext(optionsBuilder.Options);
    }
}