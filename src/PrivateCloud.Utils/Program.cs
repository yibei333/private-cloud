using PrivateCloud.Utils;

Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
Console.WriteLine(AppContext.BaseDirectory);
var result = ProjectLinkUtil.GenerateLinks(true);
Console.WriteLine(result);