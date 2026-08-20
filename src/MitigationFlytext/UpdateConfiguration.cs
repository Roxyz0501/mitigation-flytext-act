using System.Linq;

namespace MitigationFlytext
{
    internal static class UpdateConfiguration
    {
        public const string RepositoryOwner = "Roxyz0501";
        public const string RepositoryName = "mitigation-flytext-act";
        public const string PluginAssemblyName = "MitigationFlytext";
        public const string PluginFileName = "MitigationFlytext.dll";
        public const string UpdaterFileName = "MitigationFlytext.Updater.exe";

        public static bool IsConfigured => IsSafeSegment(RepositoryOwner) && IsSafeSegment(RepositoryName);

        public static string ReleasesApiUrl => "https://api.github.com/repos/" + RepositoryOwner + "/" + RepositoryName + "/releases?per_page=10";

        private static bool IsSafeSegment(string value) => !string.IsNullOrWhiteSpace(value) &&
            value.All(x => char.IsLetterOrDigit(x) || x == '-' || x == '_' || x == '.');
    }
}


