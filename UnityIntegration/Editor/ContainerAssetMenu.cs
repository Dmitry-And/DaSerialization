#if UNITY_2018_1_OR_NEWER

using System;
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
            var window = ContainerInspectorWindow.GetOrCreateWindow();
            window.Target = text;
        }
        [MenuItem("Assets/Container/Open in New Inspector...", priority = 21)]
        public static void OpenInNewContainerViewer()
        {
            var text = Selection.activeObject as TextAsset;
            var window = ContainerInspectorWindow.GetOrCreateWindow(true);
            window.Target = text;
        }
        [MenuItem("Assets/Container/Open in Inspector...", true)]
        [MenuItem("Assets/Container/Open in New Inspector...", true)]
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
            int errorsCount = 0;
            TextAsset lastUpdated = null;
            foreach (var text in Selection.GetFiltered<TextAsset>(SelectionMode.DeepAssets))
            {
                try
                {
                    var containerRef = ContainerRef.FromTextAsset(text, false);
                    if (!containerRef.IsValid)
                        continue;
                    if (containerRef.UpdateSerializers())
                        changedCount++;
                    totalCount++;
                    lastUpdated = text;
                }
                catch (Exception)
                {
                    errorsCount++;
                }
            }
            if (errorsCount > 0)
                Debug.LogWarning($"(Errors) Serializers updated for {totalCount} containers. {changedCount} changed. {errorsCount} errors (cannot update)\n",
                    totalCount == 1 ? lastUpdated : null);
            else
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