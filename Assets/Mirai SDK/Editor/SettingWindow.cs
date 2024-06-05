using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Mirai
{
    [InitializeOnLoad]
    public class SettingWindow : EditorWindow
    {
        #region InitAndOpen
        private static MiraiSdkSettings Settings => MiraiSdkSettings.Instance;

        static SettingWindow()
        {
            EditorApplication.update += OpenUpdate;
        }

        private static void OpenUpdate()
        {
            if (string.IsNullOrEmpty(Settings.ClientID) || string.IsNullOrEmpty(Settings.AppSchema))
                Open();
            EditorApplication.update -= OpenUpdate;
        }

        [MenuItem("Window/Mirai SDK Settings")]
        public static void Open()
        {
            var window = GetWindowWithRect<SettingWindow>(new Rect(0, 0, 400, 500), true, "Mirai SDK Settings", true);
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        #endregion

        private static Texture2D LogoTexture=>Resources.Load<Texture2D>("LogoMirailabs");
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(LogoTexture, GUILayout.MaxHeight(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Shared Settings:", EditorStyles.boldLabel);
            Settings.autoInit = EditorGUILayout.Toggle("Auto Init", Settings.autoInit);
            Settings.AppSchema = EditorGUILayout.TextField("App Schema", Settings.AppSchema);
            Settings.callMiraiAppType = (MiraiSdkSettings.CallMiraiAppType)EditorGUILayout.EnumPopup("If MiraiApp not found:", Settings.callMiraiAppType);
            Settings.autoLoginCachedAfterSdkInit = EditorGUILayout.Toggle("Auto Login Cached After SDK Init", Settings.autoLoginCachedAfterSdkInit);
            Settings.autoReferal = EditorGUILayout.Toggle("Auto Referral", Settings.autoReferal);
            GUILayout.Space(20);
            GUILayout.Label("Product-Dev Settings:", EditorStyles.boldLabel);
            Settings.environment = (MiraiSdkSettings.Environment)EditorGUILayout.EnumPopup("Environment", Settings.environment);
            Settings.ClientID = EditorGUILayout.TextField("Client ID", Settings.ClientID);
            Settings.referralLink = EditorGUILayout.TextField("Referral Link", Settings.referralLink);
            GUILayout.Label("List End Point:",EditorStyles.boldLabel);
            GUILayout.Label($"{Settings.authEndPoint}\n{Settings.mpcEndPoint}\n{Settings.shardsTechEndPoint}\n{Settings.referralEndPoint}",EditorStyles.helpBox);
            if (GUILayout.Button("Apply Settings"))
            {
                if (string.IsNullOrEmpty(Settings.ClientID) || string.IsNullOrEmpty(Settings.AppSchema))
                {
                    Debug.LogError("Client ID and App Schema is required!");
                }
                else
                {
                    EditorUtility.SetDirty(Settings);
                    AssetDatabase.SaveAssetIfDirty(Settings);    
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    Debug.Log("Settings applied!");
                }
            }
        }
    }
}