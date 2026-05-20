using Terraria.Graphics.CameraModifiers;

namespace Crisis.Common;

public class ScreenShakeModifier(float intensity, float fade = 0.9f, Vector2? source = null, float maxDistance = 1000f, string uniqueIdentity = "CrisisScreenShake") : ICameraModifier
{
    public string UniqueIdentity
    {
        get;
        internal set;
    } = uniqueIdentity;

    public bool Finished
    {
        get;
        internal set;
    }

    private float Intensity = intensity;
    private readonly float Fade = fade;
    private readonly Vector2? Source = source;
    private readonly float MaxDistance = maxDistance;

    public void Update(ref CameraInfo cameraInfo)
    {
        if (Main.gamePaused || Main.gameInactive)
            return;

        float effectiveIntensity = Intensity;

        if (Source.HasValue && Main.LocalPlayer?.active == true)
        {
            float dist = Vector2.Distance(Main.LocalPlayer.Center, Source.Value);
            float falloff = 1f - MathHelper.Clamp(dist / MaxDistance, 0f, 1f);
            effectiveIntensity *= falloff;
        }

        if (effectiveIntensity > 0f)
            cameraInfo.CameraPosition += Main.rand.NextVector2Circular(effectiveIntensity, effectiveIntensity);

        Intensity *= Fade;

        if (Intensity < 0.05f)
            Finished = true;
    }
}

public class CameraSystem : ModSystem
{
    public static void ScreenShake(float intensity = 8f, float fade = 0.9f, Vector2? source = null, float maxDistance = 1000f)
    {
        if (Main.dedServ)
            return;

        Main.instance.CameraModifiers.Add(new ScreenShakeModifier(intensity, fade, source, maxDistance));
    }
}