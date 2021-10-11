using UnityEngine;
using UnityEditor;
using DaSerialization;
using DaSerialization.Editor;

[CustomEditor(typeof(TextAsset))]
public class ContainerInspector : Editor
{
    public BinaryContainer ContainerToInspect;

    public override void OnInspectorGUI()
    {
        var containerPath = AssetDatabase.GetAssetPath(target);
        var containerInfo = new ContainerEditorInfo(ContainerToInspect);
        var containerView = new ContainerEditorView(containerInfo, ContainerToInspect != null);

        if (containerPath.EndsWith(".bytes"))
        {
            var pos = new Rect(new Vector2(), new Vector2(300f, 450f));
            containerView.Draw(pos);
        }
    }
}
