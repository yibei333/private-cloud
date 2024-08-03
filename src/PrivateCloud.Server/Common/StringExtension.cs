using SharpDevLib;

namespace PrivateCloud.Server.Common;

public static class StringExtension
{
    public static List<string> StringArrayMatch(this string source, string target, bool ignoreCase = true)
    {
        if (source.IsNullOrWhiteSpace() || target.IsNullOrWhiteSpace()) return [];
        if (ignoreCase) return (from a in source.SplitToList() join b in target.SplitToList() on a.ToLower() equals b.ToLower() select a).ToList();
        return (from a in source.SplitToList() join b in target.SplitToList() on a equals b select a).ToList();
    }
}