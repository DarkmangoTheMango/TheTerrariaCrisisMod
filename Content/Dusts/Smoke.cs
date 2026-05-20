namespace Crisis.Content.Dusts;

public class Smoke : ModDust
{
    public override string Texture => Assets.Images.Dusts.Smoke.KEY;

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = true;
        dust.frame = new(0, Main.rand.Next(3) * 38, 38, 38);
        dust.customData = Main.rand.NextFloat(-1, 1);
    }

    public override bool Update(Dust dust)
    {
        dust.scale += 0.05f;
        dust.rotation += (float)dust.customData * 0.01f;

        dust.position += dust.velocity;
        dust.velocity *= 0.98f;

        dust.alpha += 7;

        if (dust.alpha >= 255)
            dust.active = false;

        return false;
    }

    public override bool PreDraw(Dust dust)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;

        var origin = dust.frame.Size() / 2f;

        Main.EntitySpriteDraw(texture, dust.position - Main.screenPosition, dust.frame, dust.GetAlpha(Lighting.GetColor(dust.position.ToTileCoordinates(), Color.Gray)), dust.rotation, origin, dust.scale, SpriteEffects.None, 0f);

        return false;
    }
}