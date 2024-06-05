using System;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;


namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeComponent: USceneComponent
{
    public int SectionBaseX;
    public int SectionBaseY;
    public int ComponentSizeQuads;
    public int SubsectionSizeQuads;
    public int NumSubsections;
    public FVector4 HeightmapScaleBias;
    public FVector4 WeightmapScaleBias;
    public float WeightmapSubsectionOffset;
    public FWeightmapLayerAllocationInfo[] WeightmapLayerAllocations;
    
    public UTexture2D[] WeightmapTextures;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SectionBaseX = GetOrDefault(nameof(SectionBaseX), 0);
        SectionBaseY = GetOrDefault(nameof(SectionBaseY), 0);
        ComponentSizeQuads = GetOrDefault(nameof(ComponentSizeQuads), 0);
        SubsectionSizeQuads = GetOrDefault(nameof(SubsectionSizeQuads), 0);
        NumSubsections = GetOrDefault(nameof(NumSubsections), 1);
        HeightmapScaleBias = GetOrDefault(nameof(HeightmapScaleBias), new FVector4(0, 0, 0, 0));
        WeightmapScaleBias = GetOrDefault(nameof(WeightmapScaleBias), new FVector4(0, 0, 0, 0));
        WeightmapSubsectionOffset = GetOrDefault(nameof(WeightmapSubsectionOffset), 0f);
        WeightmapLayerAllocations = GetOrDefault(nameof(WeightmapLayerAllocations), Array.Empty<FWeightmapLayerAllocationInfo>());
        WeightmapTextures = GetOrDefault("WeightmapTextures", Array.Empty<UTexture2D>());
    }

    public void GetComponentExtent(ref int minX, ref int minY, ref int maxX, ref int maxY)
    {
        minX = Math.Min(SectionBaseX, minX);
        minY = Math.Min(SectionBaseY, minY);
        maxX = Math.Max(SectionBaseX + ComponentSizeQuads, maxX);
        maxY = Math.Max(SectionBaseY + ComponentSizeQuads, maxY);
    }
    public UTexture2D? GetHeightmap(bool bWorkOnEditingLayer) => GetOrDefault<UTexture2D>("HeightmapTexture", null);
    public UTexture2D[] GetWeightmapTextures(bool bWorkOnEditingLayer) => WeightmapTextures;

    public FWeightmapLayerAllocationInfo[] GetWeightmapLayerAllocations(bool bWorkOnEditingLayer) => WeightmapLayerAllocations;
}