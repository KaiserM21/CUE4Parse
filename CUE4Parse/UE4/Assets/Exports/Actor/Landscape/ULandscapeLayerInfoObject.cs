using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Actor.Landscape;

public class ULandscapeLayerInfoObject : USceneComponent
{
    public string LayerName;
    public FLinearColor LayerUsageDebugColor;
    public FPackageIndex PhysMaterial;

    public override void Deserialize(FAssetArchive Ar, long validPos) 
    {
        base.Deserialize(Ar, validPos);

        LayerName = GetOrDefault<string>(nameof(LayerName));
        LayerUsageDebugColor = GetOrDefault<FLinearColor>(nameof(LayerUsageDebugColor));
        PhysMaterial = GetOrDefault<FPackageIndex>(nameof(PhysMaterial));
    }
}