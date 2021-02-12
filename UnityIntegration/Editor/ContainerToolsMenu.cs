#if UNITY_2018_1_OR_NEWER

using UnityEditor;

namespace DaSerialization.Editor
{
    public static class ContainerToolsMenu
    {
        private static string _showTooltipsKey = "DaSerialization.Editor.ShowTooltips";

        public static bool ShowInfoTooltips
        {
            get => EditorPrefs.GetBool(_showTooltipsKey);
            set => EditorPrefs.SetBool(_showTooltipsKey, value);
        }

        [MenuItem("Tools/Container Viewer/Info Tooltips/Enable")]
        public static void EnableTooltips() => ShowInfoTooltips = true;
        [MenuItem("Tools/Container Viewer/Info Tooltips/Enable", validate = true)]
        public static bool EnableTooltipsValidate() => ShowInfoTooltips != true;

        [MenuItem("Tools/Container Viewer/Info Tooltips/Disable")]
        public static void DisableTooltips() => ShowInfoTooltips = false;
        [MenuItem("Tools/Container Viewer/Info Tooltips/Disable", validate = true)]
        public static bool DisableTooltipsValidate() => ShowInfoTooltips != false;

    }
}

#endif