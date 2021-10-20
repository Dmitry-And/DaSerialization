using UnityEngine;
using UnityEditor;
using DaSerialization;
using DaSerialization.Editor;

[CustomEditor(typeof(DefaultAsset))]
public class ContainerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("this works");
    }
}
