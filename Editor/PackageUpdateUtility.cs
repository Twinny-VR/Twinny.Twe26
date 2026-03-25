#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Twinny.Core.Editor
{
    public static class PackageUpdateUtility
    {
        private const string ManifestPath = "Packages/manifest.json";
        private static AddRequest s_addRequest;
        private static string s_pendingPackageLabel;

        public static bool CanShowUpdateButton(string packageRootPath)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageRootPath);
            if (packageInfo == null)
            {
                return false;
            }

            return packageInfo.source == PackageSource.Registry || packageInfo.source == PackageSource.Git;
        }

        public static string GetPackageVersionLabel(string packageRootPath, string prefix = "Version")
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageRootPath);
            if (packageInfo != null && !string.IsNullOrWhiteSpace(packageInfo.version))
            {
                return $"{prefix} {packageInfo.version}";
            }

            return prefix;
        }

        public static void RequestUpdate(string packageRootPath)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageRootPath);
            if (packageInfo == null)
            {
                EditorUtility.DisplayDialog("Update Package", "Package info could not be resolved.", "OK");
                return;
            }

            if (!CanShowUpdateButton(packageRootPath))
            {
                EditorUtility.DisplayDialog("Update Package", "This package source does not support Update in UPM.", "OK");
                return;
            }

            if (s_addRequest != null && !s_addRequest.IsCompleted)
            {
                EditorUtility.DisplayDialog("Update Package", "Another package update is already in progress.", "OK");
                return;
            }

            string identifier = packageInfo.source == PackageSource.Git
                ? GetManifestDependencyValue(packageInfo.name)
                : packageInfo.name;

            if (string.IsNullOrWhiteSpace(identifier))
            {
                EditorUtility.DisplayDialog("Update Package", "Could not resolve the package identifier for update.", "OK");
                return;
            }

            s_pendingPackageLabel = string.IsNullOrWhiteSpace(packageInfo.displayName) ? packageInfo.name : packageInfo.displayName;
            s_addRequest = Client.Add(identifier);

            EditorApplication.update -= MonitorUpdateRequest;
            EditorApplication.update += MonitorUpdateRequest;
        }

        private static void MonitorUpdateRequest()
        {
            if (s_addRequest == null || !s_addRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= MonitorUpdateRequest;

            if (s_addRequest.Status == StatusCode.Success)
            {
                UnityEngine.Debug.Log($"[PackageUpdateUtility] Updated package '{s_pendingPackageLabel}'.");
            }
            else if (s_addRequest.Status >= StatusCode.Failure)
            {
                string message = s_addRequest.Error != null ? s_addRequest.Error.message : "Unknown package update error.";
                EditorUtility.DisplayDialog("Update Package", message, "OK");
            }

            s_addRequest = null;
            s_pendingPackageLabel = null;
        }

        private static string GetManifestDependencyValue(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName) || !File.Exists(ManifestPath))
            {
                return null;
            }

            string manifestContent = File.ReadAllText(ManifestPath);
            string pattern = $"\"{Regex.Escape(packageName)}\"\\s*:\\s*\"([^\"]+)\"";
            Match match = Regex.Match(manifestContent, pattern);
            if (!match.Success || match.Groups.Count < 2)
            {
                return null;
            }

            return match.Groups[1].Value;
        }
    }
}
#endif
