using UnityEngine;
using UnityEditor;
using DaSerialization;
using DaSerialization.Editor;

[CustomEditor(typeof(DefaultAsset))]
public class ContainerFilesInspector : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("this works");
    }
}
