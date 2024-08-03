using SharpDevLib;

namespace PrivateCloud.Maui.Extensions;

public static class RequestExtension
{
    public static Dictionary<string, string> ToDictionary<T>(this T request) where T : BaseRequest
    {
        var result = new Dictionary<string, string>();
        request.GetType().GetProperties().ToList().ForEach(x =>
        {
            var value = x.GetValue(request)?.ToString();
            if (!string.IsNullOrWhiteSpace(value)) result.Add(x.Name, value);
        });
        return result;
    }
}
