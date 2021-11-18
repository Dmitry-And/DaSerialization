using UnityEngine;
using UnityEditor;
using DaSerialization;
using DaSerialization.Editor;

[CustomEditor(typeof(ContainerAsset))]
public class ContainerFilesInspector : Editor
{
    private static GUIContent RefreshButton = new GUIContent("Refresh", "Reload container from the asset and update all stats");

    public ContainerAsset asset;
    private BinaryContainer container;
    private ContainerEditorView _containerView;

    private void Refresh()
    {
        _containerView = null;
        asset = null;
    }

    public override void OnInspectorGUI()
    {
        var pos = new Rect(new Vector2(), new Vector2(Screen.width, Screen.height));
        pos = pos.Expand(-2f);
        var line = pos.SliceTop();
        EditorGUI.BeginDisabledGroup(target == null);
        if (GUI.Button(line.SliceRight(60f), RefreshButton))
            Refresh();
        EditorGUI.EndDisabledGroup();
        EditorGUI.LabelField(line.SliceLeft(38f), "Asset");
        asset = (ContainerAsset)EditorGUI.ObjectField(line, target, typeof(ContainerAsset), false);
        asset.TryGetContainer(out container);
        var info = new ContainerEditorInfo(container);
        _containerView = new ContainerEditorView(info, true);

        var updatedContainer = _containerView.Draw(pos);
    }
}
