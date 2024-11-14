using System;

namespace ImageOrganizer
{
    public static class VersionHelper
    {
        public static int CompareVersions(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion))
                return 0;

            // Remove 'v' prefix if present
            currentVersion = currentVersion.TrimStart('v');
            latestVersion = latestVersion.TrimStart('v');

            Version vCurrent;
            Version vLatest;

            if (!Version.TryParse(currentVersion, out vCurrent) || !Version.TryParse(latestVersion, out vLatest))
            {
                // If parsing fails, compare as strings
                return string.Compare(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
            }

            return vCurrent.CompareTo(vLatest);
        }
    }
}