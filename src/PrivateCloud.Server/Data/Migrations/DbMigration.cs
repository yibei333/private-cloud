using PrivateCloud.Server.Common;
using PrivateCloud.Server.Data.Entity;
using SharpDevLib;
using SharpDevLib.Extensions.Data;

namespace PrivateCloud.Server.Data.Migrations;

public class DbMigration(DataContext dbContext) : DataMigration<DataContext>(dbContext)
{
    private readonly DataContext dbContext = dbContext;

    public override void Seed()
    {
        if (!dbContext.User.Any())
        {
            var salt = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            File.WriteAllText(Statics.AdminPasswordPath, password);
            var genteratedPassword = salt.PasswordHash(password);
            var admin = new UserEntity(StaticNames.AdminName, salt, genteratedPassword, StaticNames.AdminName);
            dbContext.User.Add(admin);
        }

        dbContext.SaveChanges();
    }
}
