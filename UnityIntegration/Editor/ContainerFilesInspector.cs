using UnityEngine;
using UnityEditor;
using DaSerialization;
using DaSerialization.Editor;

[CustomEditor(typeof(ContainerAsset))]
public class ContainerFilesInspector : Editor
{
    public ContainerAsset asset;
    private BinaryContainer container;
    private ContainerEditorView _containerView;

    public override void OnInspectorGUI()
    {
        var pos = new Rect(new Vector2(), new Vector2(Screen.width, Screen.height));
        asset = (ContainerAsset)target;
        asset.TryGetContainer(out container);
        var info = new ContainerEditorInfo(container);
        _containerView = new ContainerEditorView(info, true);

        var updatedContainer = _containerView.Draw(pos);
    }
}
