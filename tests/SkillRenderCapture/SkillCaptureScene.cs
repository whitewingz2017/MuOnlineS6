using Client.Main;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Effects.Skills;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

public sealed class SkillCaptureScene : BaseScene
{
    public static ushort SkillId { get; set; }
    public static bool CaptureLocalTargetImpact { get; set; }
    public static string OutputDirectory { get; set; } = "";
    private SkillCaptureWorld _world;
    private WorldObject _effect;
    private Client.Main.Core.Client.BuffManager _rageBuffs;
    private Client.Main.Objects.Effects.RageFighterBuffWindEffect _rageWind;
    private InventoryItem[] _inventoryCaptureItems;
    private int _frame;
    private float _animationFrame;
    private int _warmup;
    private bool _spawned;
    private bool _loaded;
    private bool _failed;
    private bool _actorsPrepared;
    private bool _actorsPreparing;
    private int _lastCapturedFrame = -1;
    private static int[] CaptureFrames => SkillId == 0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_AURA")=="1" ? AuraCaptureFrames : SkillId is >= 260 and <= 270 ? RageCaptureFrames : SkillId is 223 or 224 or 225 ? BookCaptureFrames : SkillId is 219 or 221 or 222 ? DebuffCaptureFrames : SkillId == 214 ? DrainCaptureFrames : SkillId == 215 ? ChainCaptureFrames : SkillId == 238 ? ChaoticCaptureFrames : SkillId == 63 ? SummonCaptureFrames : SkillId == 3 ? LightningCaptureFrames : SkillId is 13 or 56 or 77 ? CometCaptureFrames : DefaultCaptureFrames;
    private static readonly int[] AuraCaptureFrames = [1,24,64,156,256,400];
    private static readonly int[] RageCaptureFrames = [1, 5, 12, 24, 48, 156];
    private static readonly int[] BookCaptureFrames = [1, 12, 24, 48, 96, 236];
    private static readonly int[] DebuffCaptureFrames = [1, 5, 12, 24, 48, 82];
    private static readonly int[] DrainCaptureFrames = [1, 5, 12, 24, 48, 116];
    private static readonly int[] ChainCaptureFrames = [1, 5, 12, 24, 48, 76];
    private static readonly int[] ChaoticCaptureFrames = [1, 5, 12, 24, 48, 152];
    private static readonly int[] SummonCaptureFrames = [1, 5, 12, 24, 48, 332];
    private static readonly int[] CometCaptureFrames = [1, 5, 12, 24, 48, 96];
    private static readonly int[] DefaultCaptureFrames = [1, 5, 12, 24, 48, 64];
    private static readonly int[] LightningCaptureFrames = [1, 2, 5, 12, 24, 64];

    public override async Task Load()
    {
        await ChangeWorldAsync<SkillCaptureWorld>();
        _world = (SkillCaptureWorld)World;
        await base.Load();
        MuGame.Instance.ApplyGraphicsConfiguration(new Client.Main.Configuration.GraphicsSettings
        {
            Width = 1024, Height = 768, UiVirtualWidth = 1024, UiVirtualHeight = 768,
            IsFullScreen = false
        });
        Constants.RENDER_SCALE = 1f;
    }

    public override void Update(GameTime gameTime)
    {
        // Loading may complete over several render ticks. Do not consume a
        // short effect's lifetime before the capture clock has started.
        if (_spawned && !_loaded) return;
        // PNG readback can stall a host frame and skip a two-tick effect.
        // This offline fixture runs at 100 simulated FPS, including the public
        // timing service consumed by production effects.
        if (_loaded)
        {
            FPSCounter.Instance.CalcFPS(new GameTime(gameTime.TotalGameTime - TimeSpan.FromMilliseconds(10), TimeSpan.Zero));
            FPSCounter.Instance.CalcFPS(new GameTime(gameTime.TotalGameTime, TimeSpan.FromMilliseconds(10)));
        }
        if (_loaded)
            gameTime = new GameTime(gameTime.TotalGameTime,
                TimeSpan.FromSeconds(FPSCounter.Instance.FPS_ANIMATION_FACTOR / 25f));
        base.Update(gameTime);
        if (_world?.Caster.Status == GameControlStatus.Ready)
        {
            // Reference default zoom step is 1300; the mounted reference raises
            // the eye another 30 units. Record this fixture camera explicitly.
            Vector3 center = _world.Caster.WorldPosition.Translation;
            Camera.Instance.AspectRatio = 4f / 3f;
            Camera.Instance.FOV = MathHelper.ToDegrees(2f * MathF.Atan(MathF.Tan(MathHelper.ToRadians(20f)) / (4f / 3f)));
            Camera.Instance.Position = center + new Vector3(1300f / MathF.Sqrt(2f), -1300f / MathF.Sqrt(2f), 1180f);
            Camera.Instance.Target = center + new Vector3(0f, 0f, 30f);
        }
        if (Status != GameControlStatus.Ready || _failed) return;
        if (_world.Caster.Status != GameControlStatus.Ready || _world.Victim.Status != GameControlStatus.Ready) return;
        if (!_actorsPrepared)
        {
            if (_actorsPreparing) return;
            _actorsPreparing = true;
            MuGame.TaskScheduler.QueueTask(async () =>
            {
                // No server inventory exists in this offline scene. Restore naked
                // class models after the normal actor initialization finishes.
                await _world.Caster.UpdateBodyPartClassesAsync(SkillCaptureWorld.CasterClass);
                await _world.Victim.UpdateBodyPartClassesAsync(PlayerClass.DarkWizard);
                // The production loader intentionally embeds its player skeleton.
                // This fixture uses the supplied reference skeleton on both sides,
                // so old body meshes are not bound to a different game's bones.
                var referencePlayer = await new Client.Data.BMD.BMDReader().Load(
                    Path.Combine(Constants.DataPath, "Player", "Player.bmd"));
                _world.Caster.Model = referencePlayer;
                _world.Victim.Model = referencePlayer;
                for (int i = 1; i < _world.ChainTargets.Length; i++)
                {
                    await _world.ChainTargets[i].UpdateBodyPartClassesAsync(PlayerClass.DarkWizard);
                    _world.ChainTargets[i].Model = referencePlayer;
                    _world.ChainTargets[i].Location = SkillCaptureWorld.TargetTile + (i == 1 ? new Vector2(2f, 1f) : new Vector2(3f, -1f));
                }
                foreach (var referenceWeapon in SkillCaptureWorld.ReferenceWeapons)
                {
                    if (!Client.Main.Core.Utilities.ItemDatabase.TryGetItemDefinitionByName(referenceWeapon.Name, out var definition))
                        throw new InvalidOperationException($"Cannot resolve reference weapon: {referenceWeapon.Name}");
                    // Inventory ammunition is not a held 3D weapon.
                    if (definition.Group == 4 && definition.Id is 7 or 15)
                        continue;
                    var weapon = referenceWeapon.Slot == 0 ? _world.Caster.Weapon1 : _world.Caster.Weapon2;
                    weapon.Model = await Client.Main.Content.BMDLoader.Instance.Prepare(definition.TexturePath);
                    weapon.TexturePath = definition.TexturePath;
                    weapon.ItemGroup = (byte)definition.Group;
                    weapon.MaterialItemGroup = definition.Group;
                    weapon.MaterialItemIndex = definition.Id;
                    weapon.ItemLevel = referenceWeapon.Level;
                    weapon.Hidden = false;
                    weapon.LinkParentAnimation = false;
                    weapon.ParentBoneLink = referenceWeapon.Slot == 0 ? 33 : 42;
                    weapon.Position = Vector3.Zero;
                    weapon.Angle = Vector3.Zero;
                }
                string itemWeaponMode = Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_WEAPONS") ?? "";
                if (SkillId == 0 && itemWeaponMode.Length > 0)
                {
                    bool bow = itemWeaponMode == "bow";
                    foreach (var weapon in new[] { _world.Caster.Weapon1, _world.Caster.Weapon2 })
                    {
                        weapon.Model = await Client.Main.Content.BMDLoader.Instance.Prepare(bow ? "Item/Bow01.bmd" : "Item/Sword01.bmd");
                        weapon.ItemGroup = (byte)(bow ? 4 : 0);
                        weapon.MaterialItemGroup = weapon.ItemGroup;
                        weapon.MaterialItemIndex = 0;
                        weapon.ItemLevel = int.Parse(Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_LEVEL") ?? "15");
                        weapon.Hidden = false;
                        weapon.LinkParentAnimation = false;
                        weapon.ParentBoneLink = weapon == _world.Caster.Weapon1 ? 33 : 42;
                        weapon.Position = Vector3.Zero;
                        weapon.Angle = Vector3.Zero;
                    }
                }
                if (SkillId == 0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_MATCH") == "1")
                {
                    int armorGroup=7;
                    foreach (var part in new ModelObject[] {_world.Caster.Helm,_world.Caster.Armor,_world.Caster.Pants,_world.Caster.Gloves,_world.Caster.Boots})
                    {
                        string name=armorGroup switch {7=>"Helm",8=>"Armor",9=>"Pant",10=>"Glove",_=>"Boot"};
                        part.Model=await Client.Main.Content.BMDLoader.Instance.Prepare($"Player/HDK_{name}Male01.bmd");
                        part.MaterialItemGroup=armorGroup++;part.MaterialItemIndex=29;part.Hidden=false;
                    }
                    var wing=_world.Caster.EquippedWings;
                    short wingIndex=short.Parse(Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE") ?? "36");
                    wing.ItemIndex=wingIndex;
                    var wingDefinition=Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition(12,wingIndex)!;
                    wing.Model=await Client.Main.Content.BMDLoader.Instance.Prepare(wingDefinition.TexturePath);
                    wing.ItemLevel=15;wing.Hidden=false;
                    await wing.LoadContent();
                }
                if (SkillId==0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_GM")=="1")
                    MuGame.Network.GetCharacterState().ActivateBuff(28,_world.Caster.NetworkId);
                _actorsPrepared = true;
            }, Client.Main.Controllers.TaskScheduler.Priority.High, "SkillCapture.Actors");
            return;
        }
        if (!_spawned)
        {
            if (++_warmup < 50) return;
            _world.Caster.FaceTowards(_world.Victim.Location, immediate: true);
            if(SkillId==0)
            {
                int level=int.Parse(Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_LEVEL")??"13");
                bool excellent=Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_OPTION")=="excellent";
                bool ancient=Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_OPTION")=="ancient";
                ModelObject[] parts=[_world.Caster.Helm,_world.Caster.Armor,_world.Caster.Pants,_world.Caster.Gloves,_world.Caster.Boots];
                int group=7;
                foreach(var part in parts){part.ItemLevel=level;part.IsExcellentItem=excellent;part.IsAncientItem=ancient;part.MaterialItemGroup=group++;part.MaterialItemIndex=Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_MATCH")=="1"?29:0;}
            }
            var action = _world.Caster.GetSkillAction(SkillId, isInSafeZone: false);
            if ((int)action < _world.Caster.Model.Actions.Length)
                _world.Caster.CurrentAction = action;
            File.WriteAllText(Path.Combine(OutputDirectory, "capture-state.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                SkillId,
                CasterVisible = _world.Caster.Visible,
                CasterHidden = _world.Caster.Hidden,
                CasterClass = _world.Caster.CharacterClass.ToString(),
                CasterPosition = _world.Caster.Position.ToString(),
                CasterWorldPosition = _world.Caster.WorldPosition.Translation.ToString(),
                CasterStatus = _world.Caster.Status.ToString(),
                HelmStatus = _world.Caster.Helm.Status.ToString(),
                HelmHidden = _world.Caster.Helm.Hidden,
                HelmHasModel = _world.Caster.Helm.Model != null,
                ArmorHasModel = _world.Caster.Armor.Model != null,
                ReferenceWeapons = SkillCaptureWorld.ReferenceWeapons.Select(w => new { w.Slot, w.Name, w.Level }),
                LeftWeaponHasModel = _world.Caster.Weapon1.Model != null,
                RightWeaponHasModel = _world.Caster.Weapon2.Model != null,
                SelfTarget = SkillCaptureWorld.SelfTarget,
                CameraPosition = Camera.Instance.Position.ToString(),
                CameraTarget = Camera.Instance.Target.ToString()
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            var context = new SkillEffectContext
            {
                Caster = _world.Caster,
                TargetId = SkillCaptureWorld.SelfTarget ? _world.Caster.NetworkId : _world.Victim.NetworkId,
                TargetPosition = SkillCaptureWorld.SelfTarget ? _world.Caster.WorldPosition.Translation : _world.Victim.WorldPosition.Translation,
                SkillId = SkillId,
                World = _world
            };
            bool hasRenderer = SkillVisualEffectRegistry.TrySpawn(SkillId, context, out _effect);
            if(SkillId==0){_effect=new ChainCaptureRoot();hasRenderer=true;}
            if (SkillId == 215 && SkillCaptureWorld.CaptureChainHops && hasRenderer)
            {
                _effect.Dispose();
                var root = new ChainCaptureRoot();
                WalkerObject previous = _world.Caster;
                for (int i = 0; i < _world.ChainTargets.Length; i++)
                {
                    root.Children.Add(new Client.Main.Objects.Effects.ChainLightningEffect(previous, _world.ChainTargets[i], i == 0));
                    previous = _world.ChainTargets[i];
                }
                _effect = root;
            }
            if (SkillId == 238 && CaptureLocalTargetImpact && hasRenderer)
            {
                _effect.Dispose();
                _effect = new Client.Main.Objects.Effects.ChaoticDiseierEffect(_world.Caster, null, _world.Victim.WorldPosition.Translation);
            }
            if (SkillId is 16 or 64)
            {
                // Soul Barrier enters through server buff state, not the cast registry.
                var buffs = new Client.Main.Core.Client.BuffManager(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                buffs.ProcessMagicEffectStatus(_world.Caster.NetworkId,
                    (byte)(SkillId == 16 ? Client.Main.Core.Client.BuffEffectId.ManaShield : Client.Main.Core.Client.BuffEffectId.CriticalDamage), true);
                if (SkillId == 16) _effect = new Client.Main.Objects.Effects.ClassBuffEffect(_world.Caster, buffs);
                else
                {
                    var persistent = new Client.Main.Objects.Effects.ClassBuffEffect(_world.Caster, buffs);
                    _effect.Children.Add(persistent);
                }
                hasRenderer = true;
            }
            if(SkillId is 266 or 267 or 268)
            {
                _rageBuffs=new Client.Main.Core.Client.BuffManager(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                _rageBuffs.ProcessMagicEffectStatus(_world.Caster.NetworkId,(byte)(129+SkillId-266),true);
                var persistent=new Client.Main.Objects.Effects.ClassBuffEffect(_world.Caster,_rageBuffs);
                _effect.Children.Add(persistent);
            }
            if (!hasRenderer || _effect == null)
            {
                File.WriteAllText(Path.Combine(OutputDirectory, "missing-renderer.txt"), $"Skill {SkillId} has no registry renderer. This is a failed case, not a visual match.");
                _failed = true;
                MuGame.Instance.Exit();
                return;
            }
            _world.Objects.Add(_effect);
            MuGame.TaskScheduler.QueueTask(async () =>
            {
                await _effect.Load();
                if(SkillId==263)
                {
                    for(int i=0;i<_world.ChainTargets.Length;i++)
                    {
                        var ghost=new Client.Main.Objects.Effects.DarkSideAfterimageEffect(_world.Caster,_world.ChainTargets[i],i){World=_world};
                        _world.Objects.Add(ghost);await ghost.Load();
                    }
                }
                if(_rageBuffs!=null)
                    _rageWind=_effect.Children.OfType<Client.Main.Objects.Effects.ClassBuffEffect>().Single().Children.OfType<Client.Main.Objects.Effects.RageFighterBuffWindEffect>().Single();
                _loaded = true;
            }, Client.Main.Controllers.TaskScheduler.Priority.High, "SkillCapture.Load");
            _spawned = true;
        }
        // The game uses variable-rate rendering. Count the same 25-Hz animation
        // units as the effects rather than labelling every Draw as a source frame.
        if (_loaded)
        {
            _animationFrame += FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            _frame = (int)_animationFrame;
            if(SkillId==0 && Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE")!=null)
            {
                // Show the full spread instead of the old edge-on fixture angle.
                _world.Caster.Direction=Client.Main.Models.Direction.West;
                if(_frame>=350 && Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE_REMOVE")=="1")
                    _world.Caster.EquippedWings.ItemIndex=-1;
            }
            if (SkillId == 0 && _frame >= 32 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_WEAPONS") == "remove")
            {
                _world.Caster.Weapon1.Model = null;
                _world.Caster.Weapon2.Model = null;
            }
            if(SkillId==0 && _frame>=256 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_GM")=="1")
                MuGame.Network.GetCharacterState().DeactivateBuff(28,_world.Caster.NetworkId);
            if(_rageBuffs!=null && _frame>=48)
                _rageBuffs.ProcessMagicEffectStatus(_world.Caster.NetworkId,(byte)(129+SkillId-266),false);
        }
        if (_frame > CaptureFrames[^1] + 1) MuGame.Instance.Exit();
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        if (SkillId == 0 && Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_INVENTORY") == "1")
        {
            bool general = Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_GENERAL") == "1";
            string? captureWing=Environment.GetEnvironmentVariable("TWINMU_WING_CAPTURE");
            if (_inventoryCaptureItems == null)
            {
                _inventoryCaptureItems = captureWing!=null
                    ? new[] {new InventoryItem(Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition(12,short.Parse(captureWing))!, Microsoft.Xna.Framework.Point.Zero, level:15)}
                    : general
                    ? new (int Group, short Id)[] {(0,0),(2,0),(4,0),(5,0),(6,0),(7,29),(8,29),(9,29),(10,29),(11,29),(12,0),(12,36),(13,8),(14,0),(15,0)}
                        .Select(key => new InventoryItem(Client.Main.Core.Utilities.ItemDatabase.GetItemDefinition((byte)key.Group,key.Id)
                            ?? throw new InvalidOperationException($"Missing capture item {key.Group}:{key.Id}"), Microsoft.Xna.Framework.Point.Zero, level:key.Group<=12?13:0)).ToArray()
                    : new[] {29,59,60,61,73}.Select(id => new InventoryItem(
                    new Client.Main.Controls.UI.Game.Inventory.ItemDefinition(id,"Source inventory armor",2,3,id==29?"Player/HDK_ArmorMale01.bmd":$"Player/ArmorMale{id+1:D2}.bmd") {Group=8},
                    Microsoft.Xna.Framework.Point.Zero, level:int.Parse(Environment.GetEnvironmentVariable("TWINMU_ITEM_CAPTURE_LEVEL") ?? "13"))).ToArray();
            }
            foreach (var inventoryItem in _inventoryCaptureItems)
            {
                int slotWidth = general ? inventoryItem.Definition.Width*32 : 64;
                int slotHeight = general ? inventoryItem.Definition.Height*32 : 96;
                var preview = general || captureWing!=null || inventoryItem.Definition.Id==29
                    ? Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.GetOverflowPreview(inventoryItem,slotWidth,slotHeight,gameTime,false)
                    : Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.GetMaterialAnimatedPreview(inventoryItem, 256, 256, gameTime);
                if (preview != null && _loaded && CaptureFrames.Contains(_frame) && _lastCapturedFrame != _frame)
                {
                    using var stream = File.Create(Path.Combine(OutputDirectory, $"inventory-{inventoryItem.Definition.Group}-{inventoryItem.Definition.Id}-frame-{_frame:D3}.png"));
                    preview.SaveAsPng(stream, preview.Width, preview.Height);
                    if (general && _frame == CaptureFrames[^1])
                    {
                        var slot = new Microsoft.Xna.Framework.Rectangle(100,100,slotWidth,slotHeight);
                        var draw = Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.GetItemDrawRectangle(preview,slot);
                        if (draw.Width != preview.Width || draw.Height != preview.Height || draw.Center != slot.Center)
                            throw new InvalidOperationException($"Preview placement mismatch: {inventoryItem.Definition.Group}:{inventoryItem.Definition.Id}");
                        if (Client.Main.Controls.UI.Game.Inventory.BmdPreviewRenderer.TryGetCachedOverflowPreview(inventoryItem,slotWidth,slotHeight) != preview)
                            throw new InvalidOperationException("Drag cache did not return the padded preview.");
                        Console.WriteLine($"PASS: grid preview + drag cache {inventoryItem.Definition.Group}:{inventoryItem.Definition.Id}, {slotWidth}x{slotHeight} -> {preview.Width}x{preview.Height}");
                    }
                }
                else if (general && preview == null && _frame == CaptureFrames[^1]) throw new InvalidOperationException("General item preview failed to render.");
            }
        }
        if (!_loaded || !CaptureFrames.Contains(_frame) || _lastCapturedFrame == _frame) return;
        var bindings = MuGame.Instance.GraphicsDevice.GetRenderTargets();
        var target = bindings.Length > 0 ? bindings[0].RenderTarget as Microsoft.Xna.Framework.Graphics.RenderTarget2D : null;
        File.WriteAllText(Path.Combine(OutputDirectory, $"skill-{SkillId}-frame-{_frame:D3}.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                Frame = _frame,
                CaptureLocalTargetImpact,
                SkillCaptureWorld.CaptureChainHops,
                EffectType = _effect.GetType().Name,
                EffectStatus = _effect.Status.ToString(),
                EffectPosition = _effect.Position.ToString(),
                EffectWorld = _effect.World != null,
                CasterWorld = _world.Caster.World != null,
                EffectVisible = _effect.Visible,
                EffectHidden = _effect.Hidden,
                Bounds = _effect.BoundingBoxWorld.ToString(),
                GameMasterBadge = _world.Caster.Children.ToArray().OfType<Client.Main.Objects.Effects.GameMasterBadgeEffect>().Select(b=>new {b.Hidden,b.Visible,b.Status,HasModel=b.Model!=null,Position=b.WorldPosition.Translation.ToString()}).ToArray(),
                WingMaterial = new {_world.Caster.EquippedWings.MaterialItemGroup,_world.Caster.EquippedWings.MaterialItemIndex},
                WingEffects = _world.Caster.EquippedWings.Children.ToArray().OfType<Client.Main.Objects.Effects.ReferenceWingEffect>().Select(e=>new {e.Hidden,e.Visible,e.ParticleCount,e.JointCount,e.SpriteCount}).ToArray(),
                FullSetAura = new {_world.Caster.FullSetAura.AuraTier,_world.Caster.FullSetAura.ExpandingTrailSpawns,_world.Caster.FullSetAura.RisingTrailSpawns},
                GradeArmor = _world.Objects.OfType<Client.Main.Objects.Player.PlayerObject>().SelectMany(p => p.Children.ToArray()).OfType<Client.Main.Objects.Effects.Grade15ArmorEffect>().Select(g => new {g.Hidden, g.Visible, g.Status, HasModel=g.Model!=null, Position=g.WorldPosition.Translation.ToString(), Flashes=g.Children.ToArray().Select(f=>new {f.Status,f.Visible,f.Hidden,Position=f.WorldPosition.Translation.ToString(),f.Scale}).ToArray(), g.Diagnostic}).ToArray(),
                GradeWeapons = _world.Objects.OfType<Client.Main.Objects.Player.PlayerObject>().SelectMany(p => p.Children.ToArray()).OfType<Client.Main.Objects.Effects.Grade15WeaponEffect>().Select(g => new {g.Hidden, g.ActiveHands, g.LiveParticles, g.Diagnostic}).ToArray(),
                ArrowDiagnostics = CaptureArrowDiagnostics(),
                ChaoticParts = CaptureChaoticParts(),
                RageBuffWind = _rageWind==null?null:new { _rageWind.Alpha, _rageWind.Status, Meshes=_rageWind.Model?.Meshes.Length },
                BookParts = _effect is Client.Main.Objects.Effects.SummonerBookCastEffect or Client.Main.Objects.Effects.RageFighterCastEffect
                    ? ((Client.Main.Objects.WorldObject[])typeof(Client.Main.Objects.Effects.StagedCombatEffect).GetField("_parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_effect)!)
                        .OfType<Client.Main.Objects.ModelObject>().Select(m => new { Model = m.Model?.Name, Meshes = m.Model?.Meshes.Length, Path = m.GetType().GetField("_path", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(m)?.ToString(), m.Status, m.Hidden, m.Alpha, m.CurrentAction, Frame = m.GetAnimationTime(), Position = m.Position.ToString() }).ToArray() : null,
                WorldObjects = _world.Objects.Where(o => o is Client.Main.Objects.EffectObject || o is Client.Main.Objects.Player.PlayerObject || o is Client.Main.Objects.ModelObject && o.GetType().Namespace?.StartsWith("Client.Main.Objects.Effects")==true).Select(o => new
                {
                    Type = o.GetType().Name, Status = o.Status.ToString(), o.Visible,
                    Position = o.WorldPosition.Translation.ToString()
                }).ToArray(),
                AnimationFactor = FPSCounter.Instance.FPS_ANIMATION_FACTOR
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        using var output = File.Create(Path.Combine(OutputDirectory, $"skill-{SkillId}-frame-{_frame:D3}.png"));
        if (target != null)
            target.SaveAsPng(output, target.Width, target.Height);
        else
        {
            // At native render scale the production renderer draws directly to
            // the backbuffer; MainRenderTarget can be a stale, unused surface.
            var device = MuGame.Instance.GraphicsDevice;
            int width = device.PresentationParameters.BackBufferWidth;
            int height = device.PresentationParameters.BackBufferHeight;
            var pixels = new Microsoft.Xna.Framework.Color[width * height];
            device.GetBackBufferData(pixels);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(device, width, height);
            texture.SetData(pixels);
            texture.SaveAsPng(output, width, height);
        }
        _lastCapturedFrame = _frame;
        if (SkillId == 62 && _frame == 5)
        {
            // Exercise the same authoritative forced-move entry used by packet 0x15.
            typeof(WalkerObject).GetMethod("ApplyServerForcedMove",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(_world.Victim, new object[] { _world.Victim.Location + new Vector2(3f, 0f) });
        }
    }

    private sealed class ChainCaptureRoot : EffectObject
    {
        private float _age;
        public ChainCaptureRoot()
        {
            IsTransparent = true; LightEnabled = false;
            BoundingBoxLocal = new BoundingBox(new Vector3(-10000f), new Vector3(10000f));
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            _age += FPSCounter.Instance.FPS_ANIMATION_FACTOR;
            if (_age >= 72f) { World?.RemoveObject(this); Dispose(); }
        }
    }

    private object CaptureChaoticParts()
    {
        if (_effect is not Client.Main.Objects.Effects.ChaoticDiseierEffect) return null;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var parts = new[] { "_birds", "_feathers" }.SelectMany(name =>
            ((_effect.GetType().GetField(name, flags)?.GetValue(_effect) as Array)?.Cast<object>() ?? Array.Empty<object>()))
            .OfType<ModelObject>().ToArray();
        return new
        {
            Total = parts.Length,
            Undisposed = parts.Count(p => p.Status != GameControlStatus.Disposed),
            InWorld = parts.Count(p => _world.Objects.Contains(p))
        };
    }

    private object CaptureArrowDiagnostics()
    {
        if (_effect is not Client.Main.Objects.Effects.ArrowProjectileEffect) return null;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var type = _effect.GetType();
        var renderer = type.GetField("_modelRenderer", flags)?.GetValue(_effect) as Client.Main.Objects.ModelObject;
        var projectiles = type.GetField("_projectiles", flags)?.GetValue(_effect) as Array;
        return new
        {
            Profile = type.GetField("_profile", flags)?.GetValue(_effect)?.ToString(),
            RendererStatus = renderer?.Status.ToString(), RendererVisible = renderer?.Visible,
            RendererWorld = renderer?.World != null, RendererPosition = renderer?.WorldPosition.Translation.ToString(),
            Model = renderer?.Model?.Name, ModelMeshes = renderer?.Model?.Meshes.Length,
            Projectiles = projectiles?.Cast<object>().Select(p => new
            {
                Active = p.GetType().GetField("Active")?.GetValue(p),
                Position = p.GetType().GetField("Position")?.GetValue(p)?.ToString(),
                Stopped = p.GetType().GetField("Stopped")?.GetValue(p)
            }).ToArray()
        };
    }
}


