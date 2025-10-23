using System.Reflection;

namespace Pinventory.ApiDefaults;

public static class OpenApi
{
    public static bool IsGenerating => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}