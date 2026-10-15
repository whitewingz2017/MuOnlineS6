#nullable enable
using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Graphics;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects;

// MuMain NextGradeObjectRender: each equipped +15 armor part owns its linked models.
// Parenting keeps model and sprite lifetime tied to the character.
public sealed class Grade15ArmorEffect : ModelObject
{
    private readonly ModelObject _equipment;
    private readonly string _path;
    private Task? _preparation;
    public string Diagnostic => _preparation?.Exception?.ToString() ?? _preparation?.Status.ToString() ?? "inactive";
    public Texture2D? RgbTexture { get; private set; }

    private Grade15ArmorEffect(ModelObject equipment, string name, int bone)
    {
        _equipment = equipment;
        _path = "Item/class15_" + name + ".bmd";
        ParentBoneLink = bone;
        // MuMain RenderLinkObject applies these offsets before the parent bone.
        (Vector3 rotation, Vector3 offset) = name switch
        {
            "armleft" => (new Vector3(0,-90,0), new Vector3(30,0,20)),
            "armright" => (new Vector3(0,-90,0), new Vector3(30,0,-20)),
            "bodyleft" or "bodyright" => (new Vector3(0,-90,0), new Vector3(5,-20,0)),
            "bootleft" => (new Vector3(0,90,180), new Vector3(20,15,-10)),
            "bootright" => (new Vector3(0,90,180), new Vector3(20,15,10)),
            "head" => (new Vector3(180,-90,0), new Vector3(28,20,0)),
            "pantleft" => (new Vector3(0,90,180), new Vector3(25,5,-5)),
            _ => (new Vector3(0,90,180), new Vector3(25,5,5))
        };
        Angle = rotation * (MathF.PI / 180f);
        Position = offset;
        LinkParentAnimation = false;
        ItemLevel = 13;
        MaterialItemGroup = -2;
        RenderShadow = false;
        LightEnabled = false;
        IsTransparent = true;
        AffectedByTransparency = true;
        BlendState = BlendState.Additive;
        BlendMeshState = BlendState.Additive;
        DepthState = DepthStencilState.DepthRead;
        Hidden = true;
    }

    public static void Attach(PlayerObject player)
    {
        player.Children.Add(new Grade15ArmorEffect(player.Helm, "head", 20));
        player.Children.Add(new Grade15ArmorEffect(player.Armor, "bodyleft", 35));
        player.Children.Add(new Grade15ArmorEffect(player.Armor, "bodyright", 26));
        player.Children.Add(new Grade15ArmorEffect(player.Pants, "pantleft", 3));
        player.Children.Add(new Grade15ArmorEffect(player.Pants, "pantright", 10));
        player.Children.Add(new Grade15ArmorEffect(player.Gloves, "armleft", 36));
        player.Children.Add(new Grade15ArmorEffect(player.Gloves, "armright", 27));
        player.Children.Add(new Grade15ArmorEffect(player.Boots, "bootleft", 4));
        player.Children.Add(new Grade15ArmorEffect(player.Boots, "bootright", 11));
    }

    private async Task PrepareGrade()
    {
        var model = await BMDLoader.Instance.Prepare(_path);
        var rgb = await TextureLoader.Instance.PrepareAndGetTexture("Item/rgb_mix.jpg");
        if (IsDisposeRequested) return;
        RgbTexture = rgb;
        Model = model;
        Children.Add(new GradeFlash("Effect/Magic_Ground1.jpg", .12f, new Vector3(.2f,.4f,1), true));
        Children.Add(new GradeFlash("Effect/shiny05.jpg", .4f, new Vector3(.4f,.7f,1), false));
        Children.Add(new GradeFlash("Effect/pin_lights.jpg", .6f, new Vector3(.1f,.3f,1), false) { Angle = new Vector3(0,0,MathHelper.PiOver2) });
        await LoadContent();
        foreach (var child in Children.GetSnapshotArray())
        {
            if (IsDisposeRequested) return;
            await child.Load();
        }
    }

    private sealed class GradeFlash : SpriteObject
    {
        private readonly string _texture;
        private readonly Vector3 _color;
        private readonly bool _pulse;
        private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[6];
        public override string TexturePath => _texture;
        public GradeFlash(string texture, float scale, Vector3 color, bool pulse)
        {
            _texture=texture; _color=color; _pulse=pulse;
            Scale=scale; Light=color; LightEnabled=true;
            BlendState=BlendState.Additive;
            DepthState=DepthStencilState.DepthRead;
            IsTransparent=true; AffectedByTransparency=true;
        }
        protected override void RecalculateWorldPosition()
        {
            if (Parent is ModelObject model)
            {
                var bones=model.GetBoneTransforms();
                var attachment=bones != null && bones.Length > 0 ? bones[0] : Matrix.Identity;
                WorldPosition=Matrix.CreateTranslation(attachment.Translation)*model.WorldPosition;
            }
            else base.RecalculateWorldPosition();
        }
        public override void Draw(GameTime gameTime) { }
        // Grade models render in the transparent pass; emit their flashes afterward.
        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || SpriteTexture == null) return;
            var camera = Matrix.Invert(Camera.Instance.View);
            float rotation = Angle.Z;
            float inheritedScale = Parent?.WorldPosition.Right.Length() ?? 1;
            var right = (camera.Right*MathF.Cos(rotation)+camera.Up*MathF.Sin(rotation))*SpriteTexture.Width*Scale*inheritedScale*.5f;
            var up = (camera.Up*MathF.Cos(rotation)-camera.Right*MathF.Sin(rotation))*SpriteTexture.Height*Scale*inheritedScale*.5f;
            var center = WorldPosition.Translation;
            int count=0;
            SourceEffectGeometry.Quad(_vertices,ref count,center-up,center+up,right,new Color(Light*TotalAlpha),0,1);
            ElfSupportBuffEffect.DrawWorldGeometry(SpriteTexture,_vertices,count,samplerOverride:SamplerState.LinearClamp);
        }

        public override void Update(GameTime gameTime)
        {
            Light=_color*1.8f*(_pulse ? MathF.Abs(MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds*10)) : 1);
            RecalculateWorldPosition();
            base.Update(gameTime);
        }
    }

    public override void Update(GameTime gameTime)
    {
        RenderShadow = false;
        bool active = _equipment.ItemLevel == 15 && _equipment.Model != null && !_equipment.Hidden;
        if (active && _preparation == null)
            _preparation = PrepareGrade();
        Hidden = !active || Model == null || _preparation == null || !_preparation.IsCompletedSuccessfully;
        base.Update(gameTime);
    }
}
