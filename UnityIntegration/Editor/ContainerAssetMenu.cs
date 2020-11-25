#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public static class ContainerAssetMenu
    {
        // priority = 18 - right after 'Create' menu
        [MenuItem("Assets/Container/Open in Inspector...", priority = 20)]
        public static void OpenInContainerViewer()
        {
            var text = Selection.activeObject as TextAsset;
            ContainerInspectorWindow.Init();
            ContainerInspectorWindow window = (ContainerInspectorWindow)EditorWindow.GetWindow(typeof(ContainerInspectorWindow));
            window.Target = text;
        }
        [MenuItem("Assets/Container/Open in Inspector...", true)]
        public static bool SelectionIsContainer()
        {
            var text = Selection.activeObject as TextAsset;
            return ContainerRef.FromTextAsset(text, false).IsValid;
        }

        [MenuItem("Assets/Container/Update Serializers", priority = 22)]
        public static void UpdateSerializers()
        {
            int totalCount = 0;
            int changedCount = 0;
            TextAsset lastUpdated = null;
            foreach (var text in Selection.GetFiltered<TextAsset>(SelectionMode.DeepAssets))
            {
                if (ContainerRef.FromTextAsset(text, false).UpdateSerializers())
                    changedCount++;
                totalCount++;
                lastUpdated = text;
            }
            Debug.Log($"Serializers updated for {totalCount} containers. {changedCount} changed.\n",
                totalCount == 1 ? lastUpdated : null);
        }
        [MenuItem("Assets/Container/Update Serializers", true)]
        public static bool SelectionHaveContainers()
        {
            foreach (var text in Selection.GetFiltered<TextAsset>(SelectionMode.DeepAssets))
                if (ContainerRef.FromTextAsset(text, false).IsValid)
                    return true;
            return false;
        }
    }
}

#endif