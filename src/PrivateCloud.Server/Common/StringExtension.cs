using SharpDevLib;

namespace PrivateCloud.Server.Common;

public static class StringExtension
{
    private static readonly char[] separator = [',', ';'];

    public static List<string> SplitToList(this string source, bool lowercase = true) => source?.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(x => lowercase ? x.ToLower() : x).ToList() ?? [];

    public static List<string> StringArrayMatch(this string source, string target, bool ignoreCase = true)
    {
        if (source.IsEmpty() || target.IsEmpty()) return [];
        if (ignoreCase) return (from a in source.SplitToList() join b in target.SplitToList() on a.ToLower() equals b.ToLower() select a).ToList();
        return (from a in source.SplitToList() join b in target.SplitToList() on a equals b select a).ToList();
    }
}