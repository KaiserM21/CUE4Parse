using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class AActor : UObject {
    public string ActorLabel = "None";
    public override void Deserialize(FAssetArchive Ar, long validPos) {
        base.Deserialize(Ar, validPos);
        if (Ar.Position != validPos && Ar.Game <= EGame.GAME_UE5_2 && FUE5PrivateFrostyStreamObjectVersion.Get(Ar) >= FUE5PrivateFrostyStreamObjectVersion.Type.SerializeActorLabelInCookedBuilds)
        {
            if (Ar.ReadBoolean()) {
                ActorLabel = Ar.ReadFString();
            }
        }
    }
}