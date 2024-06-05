using System.Linq;
using System.Xml;
using UnityEditor;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Mirai
{
    public class PostBuildProcessor : 
#if UNITY_ANDROID
        IPostGenerateGradleAndroidProject, 
#endif
        IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        #region Android

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            UpdateManifest($"{path}/src/main/AndroidManifest.xml");
        }

        private static void UpdateManifest(string manifestPath)
        {
#if UNITY_ANDROID
            var unityManifest = new XmlDocument();
            unityManifest.Load(manifestPath);

            var namespaceManager = new XmlNamespaceManager(unityManifest.NameTable);
            namespaceManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

            var manifestNode = unityManifest.SelectSingleNode("/manifest", namespaceManager);
            var queriesNode = manifestNode.SelectSingleNode("queries", namespaceManager) ??
                              AddElementAndReturn(manifestNode, "queries");

            var packageName = "co.mirailabs.app";
            _ = queriesNode.SelectSingleNode($"package[@android:name='{packageName}']", namespaceManager) ??
                AddElementAndReturn(queriesNode, "package", "android:name", packageName);
            //---//
            if (!string.IsNullOrEmpty(MiraiSdkSettings.Instance.AppSchema))
            {
                var scheme = MiraiSdkSettings.Instance.AppSchema.Split(":")[0];
                var activityNode = manifestNode.SelectSingleNode(
                    "application/activity", namespaceManager);
                var intentFilterDataNode =
                    activityNode?.SelectSingleNode($"intent-filter/data[@android:scheme='{scheme}']", namespaceManager);
                if (intentFilterDataNode == null)
                {
                    var intentFilterNode = AddElementAndReturn(activityNode, "intent-filter");
                    AddElementAndReturn(intentFilterNode, "action", "android:name", "android.intent.action.VIEW");
                    AddElementAndReturn(intentFilterNode, "category", "android:name",
                        "android.intent.category.DEFAULT");
                    AddElementAndReturn(intentFilterNode, "category", "android:name",
                        "android.intent.category.BROWSABLE");
                    AddElementAndReturn(intentFilterNode, "data", "android:scheme", scheme);
                }
            }

            unityManifest.Save(manifestPath);
#endif
        }

        private static XmlNode AddElementAndReturn(XmlNode parentNode, string elementName, string attributeName = null,
            string attributeValue = null)
        {
            var element = parentNode.OwnerDocument.CreateElement(elementName);

            if (!string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(attributeValue))
            {
                var attribute =
                    parentNode.OwnerDocument.CreateAttribute(attributeName,
                        "http://schemas.android.com/apk/res/android");
                attribute.Value = attributeValue;
                element.Attributes.Append(attribute);
            }

            parentNode.AppendChild(element);
            return element;
        }

        #endregion

        #region IOS

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
            {
                ModifyPlist(report.summary.outputPath);
            }
        }

        private static void ModifyPlist(string pathToBuiltProject)
        {
#if UNITY_IOS
            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var rootDict = plist.root;
            var lSApplicationQueriesSchemes =
 rootDict["LSApplicationQueriesSchemes"]?.AsArray() ?? rootDict.CreateArray("LSApplicationQueriesSchemes");
            var queriesSchemes = lSApplicationQueriesSchemes.values.Select(v => v.AsString()).ToList();
            var miraiAppSchema = MiraiSdkSettings.Instance.MiraiAppSchema.Split(":")[0];
            if (!queriesSchemes.Contains(miraiAppSchema))
                lSApplicationQueriesSchemes.AddString(miraiAppSchema);
            //---//
            if (!string.IsNullOrEmpty(MiraiSdkSettings.Instance.AppSchema))
            {
                var cFBundleURLTypes =
                    rootDict["CFBundleURLTypes"]?.AsArray() ?? rootDict.CreateArray("CFBundleURLTypes");
                var bundleURLSchemes = cFBundleURLTypes.values
                    .Where(v => v.AsDict()["CFBundleURLSchemes"] != null)
                    .SelectMany(v => v.AsDict()["CFBundleURLSchemes"].AsArray().values)
                    .Select(v => v.AsString())
                    .ToList();
                var appSchema = MiraiSdkSettings.Instance.AppSchema.Split(":")[0];
                if (!bundleURLSchemes.Contains(appSchema))
                {
                    if (cFBundleURLTypes.values.Count == 0)
                    {
                        var dict = cFBundleURLTypes.AddDict();
                        dict.SetString("CFBundleURLName", "");
                    }
                    var firstDict = cFBundleURLTypes.values[0].AsDict();
                    var arrBundleURLSchemes = firstDict["CFBundleURLSchemes"]?.AsArray() ??
                                              firstDict.CreateArray("CFBundleURLSchemes");
                    arrBundleURLSchemes.AddString(appSchema);
                }
            }
            plist.WriteToFile(plistPath);
#endif
        }

        #endregion

    }
}