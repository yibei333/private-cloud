using SharpDevLib;
using System.Text;

namespace PrivateCloud.Server.Common;

public static class CryptoExtension
{
    public const string ZeroAesIV = "0000000000000000";
    public static readonly byte[] ZeroAesIVBtyes = Encoding.UTF8.GetBytes(ZeroAesIV);

    public static string GenerateAesIV() => Guid.NewGuid().ToString().Replace("-", "")[..16];

    public static string PasswordHash(this Guid id, string password) => id.ToString().PasswordHash(password);

    public static string PasswordHash(this string id, string password) => $"{password}_{id}".Utf8Decode().Sha256();
}