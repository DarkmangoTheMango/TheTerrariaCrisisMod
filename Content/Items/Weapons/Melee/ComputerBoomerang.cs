using Crisis.Content.Dusts;
using System.IO;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.GameContent;
using static Crisis.Core.LocalizationReferences.Mods.Crisis.Projectiles;

namespace Crisis.Content.Items.Weapons.Melee;

public class ComputerBoomerang : ModItem
{
    public override string Texture => Assets.Images.Items.Weapons.Melee.ComputerBoomerang.KEY;

    public override void SetDefaults()
    {
        Item.Size = new(16);
        Item.scale = 1f;

        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.damage = 30;
        Item.knockBack = 4f;

        Item.shoot = ModContent.ProjectileType<ComputerBoomerangPro>();
        Item.shootSpeed = 12;

        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.UseSound = SoundID.Item1;

        Item.value = Item.buyPrice(0, 12, 0, 0);
        Item.rare = ItemRarityID.LightRed;
    }
}

public class ComputerBoomerangPro : ModProjectile
{
    #region Fields

    private Player Owner => Main.player[Projectile.owner];

    private NPC? Target
    {
        get;
        set;
    }

    private ref float Timer => ref Projectile.ai[0];

    private enum AIState
    {
        Fly,
        Home,
        Explode
    }

    private AIState State
    {
        get;
        set;
    }

    #endregion Fields

    public override string Texture => Assets.Images.Items.Weapons.Melee.ComputerBoomerangPro.KEY;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.friendly = true;

        Projectile.Size = new(46);
        Projectile.scale = 1f;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;

        Projectile.aiStyle = -1;
        AIType = -1;
    }

    #region Networking

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)State);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        State = (AIState)reader.ReadByte();
    }

    #endregion Networking

    #region Behavior

    public override void AI()
    {
        if (Projectile.wet)
            Projectile.Kill();

        switch (State)
        {
            case AIState.Fly:
                DoBehavior_Fly();
                break;
            case AIState.Home:
                DoBehavior_Home();
                break;
            case AIState.Explode:
                DoBehavior_Explode();
                break;
            default:
                break;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        SwitchState(AIState.Explode);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        SwitchState(AIState.Explode);
    }

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        width = height = 16;
        return true;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

        return true;
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.NPCDeath37, Projectile.Center);

        for (int k = 0; k < 5; k++)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(5, 15), ModContent.ProjectileType<ComputerShard>(), (int)(Owner.HeldItem.damage * 0.2f), Owner.HeldItem.knockBack * 0.1f, Projectile.owner);
    }

    private void DoBehavior_Fly()
    {
        if (Timer >= 1)
        {
            if (Target is null)
            {
                Projectile.tileCollide = true;
                Projectile.velocity.Y += 0.5f;
            }
            else
                SwitchState(AIState.Home);

            return;
        }
        else
            foreach (var npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy(Projectile) && Projectile.Distance(npc.Center) < 300f)
                    Target = npc;

        Projectile.rotation = Projectile.velocity.X * 0.2f;

        Projectile.velocity *= 0.95f;

        Timer += 0.02f;
    }

    private void DoBehavior_Home()
    {
        if (Target is not null && !Target.active)
            Projectile.Kill();

        Projectile.tileCollide = false;

        Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(Target?.Center ?? Vector2.Zero) * 18f, 0.05f);
        Projectile.rotation += Projectile.velocity.Length() * 0.1f;
    }

    private void DoBehavior_Explode()
    {
        Projectile.frame = 1;

        if (Timer <= 0)
        {
            SoundEngine.PlaySound(new SoundStyle(Assets.Sounds.Custom.ComputerCrash.KEY), Projectile.Center);

            Projectile.velocity = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.25f, 0.5f);
        }

        if (Timer >= 1)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ComputerExplosion>(), Owner.HeldItem.damage, Owner.HeldItem.knockBack * 2, Projectile.owner);

            Projectile.Kill();
        }

        Projectile.rotation += Projectile.velocity.Length() * 0.1f;

        Projectile.velocity *= 0.95f;

        Timer += 0.015f;
    }

    private void SwitchState(AIState state)
    {
        State = state;
        Timer = 0f;
        Projectile.netUpdate = true;
    }

    #endregion Behavior

    #region Drawing

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;
        var glow = ModContent.Request<Texture2D>($"{Texture}_Glow").Value;
        var glow2 = ModContent.Request<Texture2D>($"{Texture}_Glow2").Value;

        var position = Projectile.Center - Main.screenPosition;

        var frameHeight = texture.Height / Main.projFrames[Type];
        Rectangle sourceRectangle = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);

        Main.spriteBatch.Draw(texture, position, sourceRectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(glow, position, sourceRectangle, Projectile.GetAlpha(Color.White), Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);

        if (State == AIState.Explode)
        {
            Main.spriteBatch.Draw(glow2, position + Main.rand.NextVector2Circular(2, 2), sourceRectangle, Projectile.GetAlpha(Color.OrangeRed with { A = 0 }) * Timer, Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale * 1.1f, SpriteEffects.None, 0f);
        }

        return false;
    }

    #endregion Drawing
}

public class ComputerExplosion : ModProjectile
{
    public override string Texture => Assets.Images.Items.Weapons.Melee.ComputerBoomerang.KEY;

    public override void SetDefaults()
    {
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.friendly = true;

        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;

        Projectile.Size = new(300);
        Projectile.scale = 1f;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;

        Projectile.alpha = 255;
        Projectile.hide = true;

        Projectile.aiStyle = -1;
        AIType = -1;
    }

    public override void OnSpawn(IEntitySource source)
    {
        SoundEngine.PlaySound(new SoundStyle(Assets.Sounds.Custom.Explosion.KEY)
        {
            pitchVariance = 0.1f
        }, Projectile.Center);

        for (int k = 0; k < 20; k++)
        {
            Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(1, 1) * 50, ModContent.DustType<Smoke>(), Main.rand.NextVector2Circular(1, 1), 0, default, 1);
            Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(2, 1) * 7, 0, default, 3).noGravity = true;
        }

        CameraSystem.ScreenShake();
    }

    public override void AI()
    {
        if (++Projectile.frameCounter >= 5)
        {
            Projectile.frameCounter = 0;

            if (++Projectile.frame >= Main.projFrames[Type])
                Projectile.Kill();
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;

        var frameHeight = texture.Height / Main.projFrames[Type];
        Rectangle sourceRectangle = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);

        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, new(sourceRectangle.Width / 2f, sourceRectangle.Height - 10), Projectile.scale, SpriteEffects.None, 0f);

        return false;
    }
}

public class ComputerShard : ModProjectile
{
    public override string Texture => Assets.Images.Projectiles.Melee.ComputerShard.KEY;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 3;
    }

    public override void SetDefaults()
    {
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.friendly = true;

        Projectile.Size = new(8);
        Projectile.scale = 1f;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;

        Projectile.aiStyle = -1;
        AIType = -1;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
    }

    public override void AI()
    {
        Projectile.rotation += Projectile.velocity.X * 0.1f;
        Projectile.velocity.Y += 0.6f;
        Projectile.velocity *= 0.98f;

        if (Projectile.timeLeft <= 20)
        {
            Projectile.damage = 0;
            Projectile.scale *= 0.9f;
            Projectile.alpha += 10;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.timeLeft >= 20)
            SoundEngine.PlaySound(SoundID.NPCHit4 with
            {
                Pitch = 0.3f,
                volume = 0.3f
            }, Projectile.Center);

        if (Projectile.timeLeft > 40)
            Projectile.timeLeft = 40;

        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X * 0.98f;

        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y * 0.8f;

        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;

        var frameHeight = texture.Height / Main.projFrames[Type];
        Rectangle sourceRectangle = new(0, frameHeight * Projectile.frame, texture.Width, frameHeight);

        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);

        return false;
    }
}