using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Rendering;

namespace SnapshotShaders
{
    public class SnapshotInstaller : Editor
    {
        private static List<Pipeline> compatiblePipelines;
        private static List<Pipeline> installedPipelines;

        private static readonly string builtInPackageGUID = "e7dff09af592a41479f49798b8032c2e";
        private static readonly string urpPackageGUID = "c6bc192c70515064fb56a8b66e39aa0e";
        private static readonly string hdrpPackageGUID = "810c5a1d9c280c3438db52b25788e127";

        private static readonly string builtInInstallGUID = "476254a6c35e24547902755984901904";
        private static readonly string urpInstallGUID = "d55c55deba26fc64e89428a524d51cdb";
        private static readonly string hdrpInstallGUID = "dbb496cc567761f458c40854cccd25e8";

        public class SnapshotImport : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                foreach (string str in importedAssets)
                {
                    // If we detect that this very file was reimported, trigger the installation window.
                    if(str.Contains("SnapshotInstaller.cs"))
                    {
                        SnapshotInstallerWindow.ShowWindow();
                    }
                }
            }
        }

        static SnapshotInstaller()
        {
            AssetDatabase.importPackageCompleted += Initialize;
        }

        private static void Initialize(string packagename)
        {
            Initialize();
        }

        public static void Initialize()
        {
            compatiblePipelines = FindCompatiblePipelines();
            installedPipelines = FindInstalledPipelines();
        }

        public enum Pipeline
        {
            BuiltinAlone,
            BuiltinPostProcess,
            URP,
            HDRP
        }

        // Get a list of every package installed via the Package Manager.
        private static List<UnityEditor.PackageManager.PackageInfo> GetInstalledPackages()
        {
            ListRequest listRequest = Client.List(true, true);

            while (listRequest.Status == StatusCode.InProgress) { }

            if (listRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("(Snapshot Shaders Pro): Could not retrieve package list.");
            }

            PackageCollection packageCollection = listRequest.Result;
            return new List<UnityEditor.PackageManager.PackageInfo>(packageCollection);
        }

        // Check which RP packages are currently installed.
        private static List<Pipeline> FindCompatiblePipelines()
        {
            List<Pipeline> compatiblePipelines = new List<Pipeline>();

            var packageCollection = GetInstalledPackages();

            for(int i = 0; i < packageCollection.Count; ++i)
            {
                var packageInfo = packageCollection[i];

                if(packageInfo.name == "com.unity.render-pipelines.universal")
                {
                    compatiblePipelines.Add(Pipeline.URP);
                }

                if(packageInfo.name == "com.unity.render-pipelines.high-definition")
                {
                    compatiblePipelines.Add(Pipeline.HDRP);
                }

                if(packageInfo.name == "com.unity.postprocessing")
                {
                    compatiblePipelines.Add(Pipeline.BuiltinPostProcess);
                }
            }

            if(!compatiblePipelines.Contains(Pipeline.BuiltinPostProcess))
            {
                compatiblePipelines.Add(Pipeline.BuiltinAlone);
            }

            return compatiblePipelines;
        }

        private static List<Pipeline> FindInstalledPipelines()
        {
            List<Pipeline> installedPipelines = new List<Pipeline>();

            if(IsInstalled(urpInstallGUID))
            {
                installedPipelines.Add(Pipeline.URP);
            }

            if (IsInstalled(hdrpInstallGUID))
            {
                installedPipelines.Add(Pipeline.HDRP);
            }

            if (IsInstalled(builtInInstallGUID))
            {
                installedPipelines.Add(Pipeline.BuiltinPostProcess);
            }

            return installedPipelines;
        }

        // We are using built-in but PostProcessing not installed. Try to install it.
        public static bool InstallBuiltinPostProcess()
        {
            AddRequest addRequest = Client.Add("com.unity.postprocessing");

            while(addRequest.Status == StatusCode.InProgress) { }

            if(addRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("(Snapshot Shaders Pro): Unable to install PostProcessing package for Built-in Pipeline.");
                return false;
            }

            return true;
        }

        public static void InstallSnapshotBuiltin()
        {
            InstallSnapshot(builtInPackageGUID, "Built-in");
        }

        public static void InstallSnapshotURP()
        {
            InstallSnapshot(urpPackageGUID, "URP");
        }

        public static void InstallSnapshotHDRP()
        {
            InstallSnapshot(hdrpPackageGUID, "HDRP");
        }

        private static void InstallSnapshot(string GUID, string pipelineName)
        {
            string path = AssetDatabase.GUIDToAssetPath(GUID);
            
            if(path.Length > 0)
            {
                AssetDatabase.ImportPackage(path, true);
            }
            else
            {
                Debug.LogError($"(Snapshot Shaders Pro): Could not find package file for {pipelineName}. Consider manually installing the package.");
            }
        }

        private static bool IsInstalled(string GUID)
        {
            string path = string.Empty;
            path = AssetDatabase.GUIDToAssetPath(GUID);

            if(path.Length > 0)
            {
                var file = AssetDatabase.LoadAssetAtPath(path, typeof(object));

                return (file != null);
            }

            return false;
        }

        public static List<Pipeline> GetCompatiblePipelines()
        {
            return compatiblePipelines;
        }

        public static List<Pipeline> GetInstalledPipelines()
        {
            return installedPipelines;
        }
    }
}

