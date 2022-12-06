#if UNITY_EDITOR

using UnityEditor;

namespace DaSerialization.Tests
{
    public static partial class TestContainerCreator
    {
        private const string CONTAINER_PATH = "Assets/Tests/";
        private const string CONTAINER_NAME = "TestContainer";
        private const string FULL_CONTAINER_PATH = CONTAINER_PATH + CONTAINER_NAME;

        [MenuItem("Tools/Tests/Create Test Container", priority = int.MaxValue)]
        private static void CreateContainerInUnity()
        {
            CreateContainer(FULL_CONTAINER_PATH);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
}

#endif