using System.Reflection;

namespace Pinventory.ServiceDefaults;

public static class CodeGeneration
{
    public static bool IsGenerating => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider" ||
                                       Environment.GetCommandLineArgs().Contains("codegen");
}