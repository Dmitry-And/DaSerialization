#if UNITY_2018_1_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace DaSerialization.Editor
{
    public static class ContainerAssetMenu
    {
        [MenuItem("Assets/Container/Update Serializers", true)]
        public static bool SelectionHaveContainers()
        {
            foreach (var text in Selection.GetFiltered<TextAsset>(SelectionMode.DeepAssets))
                if (ContainerRef.FromTextAsset(text).IsValid)
                    return true;
            return false;
        }
        [MenuItem("Assets/Container/Open in Viewer...", true)]
        public static bool SelectionIsContainer()
        {
            var text = Selection.activeObject as TextAsset;
            return ContainerRef.FromTextAsset(text).IsValid;
        }

        [MenuItem("Assets/Container/Update Serializers")]
        public static void UpdateSerializers()
        {
            int totalCount = 0;
            int changedCount = 0;
            TextAsset lastUpdated = null;
            foreach (var text in Selection.GetFiltered<TextAsset>(SelectionMode.DeepAssets))
            {
                if (ContainerRef.FromTextAsset(text).UpdateSerializers())
                    changedCount++;
                totalCount++;
                lastUpdated = text;
            }
            Debug.Log($"Serializers updated for {totalCount} containers. {changedCount} changed.\n",
                totalCount == 1 ? lastUpdated : null);
        }

        [MenuItem("Assets/Container/Open in Viewer...")]
        public static void OpenInContainerViewer()
        {
            var text = Selection.activeObject as TextAsset;
            ContainerInAssetWindow.Init();
            ContainerInAssetWindow window = (ContainerInAssetWindow)EditorWindow.GetWindow(typeof(ContainerInAssetWindow));
            window.Target = text;
        }
    }
}

#endif