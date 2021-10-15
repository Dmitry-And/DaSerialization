#if UNITY_2018_1_OR_NEWER

using UnityEditor;

namespace DaSerialization.Tests
{
    static partial class TestContainerCreator
    {
        private const string CONTAINER_PATH = "Assets/Tests/";
        private const string CONTAINER_NAME = "TestContainer";
        private const string FULL_CONTAINER_PATH = CONTAINER_PATH + CONTAINER_NAME;

        [MenuItem("Tools/Tests/Create Test Container", priority = int.MaxValue)]
        private static void CreateContainer()
        {
            CreateContainer(FULL_CONTAINER_PATH);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
        }
    }
}

#endif