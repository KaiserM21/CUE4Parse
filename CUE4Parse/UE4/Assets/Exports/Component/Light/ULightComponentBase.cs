namespace CUE4Parse.UE4.Assets.Exports.Component.Light;

public class ULightComponentBase : USceneComponent { }
public class ULightComponent : ULightComponentBase { }
public class USkyLightComponent : ULightComponentBase { }
public class UDirectionalLightComponent : ULightComponent { }
public class ULocalLightComponent : ULightComponent { }
public class URectLightComponent : ULocalLightComponent { }
public class UPointLightComponent : ULocalLightComponent { }
public class USpotLightComponent : UPointLightComponent { }
