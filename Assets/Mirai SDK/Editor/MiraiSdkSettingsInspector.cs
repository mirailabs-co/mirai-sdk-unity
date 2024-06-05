using UnityEditor;

namespace Mirai
{
    [CustomEditor(typeof(MiraiSdkSettings))]
    public class MiraiSdkSettingsInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Go to Window/Mirai SDK Settings to edit settings");
            DrawDefaultInspector();
        }
    }
}