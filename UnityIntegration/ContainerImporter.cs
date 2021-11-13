using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "container")]
public class ContainerImporter : ScriptedImporter 
{
    private ContainerAsset _containerAsset;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        _containerAsset = (ContainerAsset)ScriptableObject.CreateInstance("ContainerAsset");
        _containerAsset.bytes = File.ReadAllBytes(assetPath);

        ctx.AddObjectToAsset("Container", _containerAsset);
        ctx.SetMainObject(_containerAsset);
    }
}
