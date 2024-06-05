using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using System;


namespace CUE4Parse.UE4.Assets.Exports.Actor
{
    public class ALandscapeProxy : AActor
    {
        public FPackageIndex[] LandscapeComponents = Array.Empty<FPackageIndex>();

        public int ComponentSizeQuads { get; private set; }

        public int SubsectionSizeQuads { get; private set; }

        public int NumSubsections { get; private set; }

        public int LandscapeSectionOffset { get; private set; }

        public FPackageIndex LandscapeMaterial { get; private set; }

        public FPackageIndex SplineComponent { get; private set; }

        public FGuid LandscapeGuid { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            ComponentSizeQuads = GetOrDefault<int>("ComponentSizeQuads");
            SubsectionSizeQuads = GetOrDefault<int>("SubsectionSizeQuads");
            NumSubsections = GetOrDefault<int>("NumSubsections");
            LandscapeComponents = GetOrDefault<FPackageIndex[]>("LandscapeComponents", Array.Empty<FPackageIndex>());
            LandscapeSectionOffset = GetOrDefault<int>("LandscapeSectionOffset");
            LandscapeMaterial = GetOrDefault<FPackageIndex>("LandscapeMaterial", new FPackageIndex());
            SplineComponent = GetOrDefault<FPackageIndex>("SplineComponent", new FPackageIndex());
            LandscapeGuid = GetOrDefault<FGuid>("LandscapeGuid");
        }
    }
    
    public class ALandscape: ALandscapeProxy { }
    public class ALandscapeStreamingProxy: ALandscapeProxy { }
}