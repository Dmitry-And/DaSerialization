using UnityEngine;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "container")]
public class ContainerImporter : ScriptedImporter 
{
    private ContainerAsset _containerAsset;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        _containerAsset = new ContainerAsset(assetPath);

        ctx.AddObjectToAsset("Container", _containerAsset);
        ctx.SetMainObject(_containerAsset);
    }
}
