using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Texture
{
    public enum ETextureAddress : byte
    {
        [Description("Wrap")]
        TA_Wrap,
        [Description("Clamp")]
        TA_Clamp,
        [Description("Mirror")]
        TA_Mirror
    }
}
