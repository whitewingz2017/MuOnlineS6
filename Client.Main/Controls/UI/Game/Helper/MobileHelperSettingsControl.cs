#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Configuration;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Controls.UI.Game.Skills;
using Client.Main.Helpers;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace Client.Main.Controls.UI.Game.Helper;

internal sealed class MobileHelperSettingsControl : UIControl
{
    private readonly MuHelperController _controller;
    private MuHelperConfig _draft=new();
    private readonly List<(Rectangle Bounds,Action Click)> _actions=new();
    private readonly Dictionary<string,Texture2D?> _atlases=new();
    private int _tab,_scroll,_startScroll,_pressY;
    private bool _pressed,_dragged,_names;
    private Action<ushort>? _skillSelection;
    private readonly TextFieldControl _nameInput;
    private Rectangle _content;
    private const int RowHeight=52;
    public MobileHelperSettingsControl(MuHelperController controller)
    {
        _controller=controller;Visible=false;Interactive=true;AutoViewSize=false;
        BackgroundColor=Color.Transparent;BorderThickness=0;
        _nameInput=TextFieldControl.Create();_nameInput.Visible=false;_nameInput.Placeholder="Item name";
        _nameInput.FontSize=14;_nameInput.EnterKeyPressed+=(_,_)=>AddName();Controls.Add(_nameInput);
    }
    public override async Task Load()
    {
        await base.Load();
        foreach(string path in SkillIconAtlas.TexturePaths)_atlases[path]=await UiThemeManager.LoadThemeTextureAsync(path);
    }
    public void Toggle()
    {
        if(Visible){Cancel();return;}
        _draft=_controller.Config.Clone();_tab=0;_scroll=0;_names=false;_skillSelection=null;
        Visible=true;BringToFront();if(Scene!=null)Scene.FocusControl=this;
    }
    private void Cancel(){Visible=false;_nameInput.Blur();_nameInput.Visible=false;_skillSelection=null;}
    private void Save(){if(!_draft.PickAllItems)_draft.PickSelectedItems=true;_draft.Normalize();_controller.Load(_draft);_controller.Save();Cancel();}
    private void AddName()
    {
        string value=_nameInput.Value.Trim();
        if(value.Length>0 && !_draft.ExtraItems.Contains(value,StringComparer.OrdinalIgnoreCase))_draft.ExtraItems.Add(value);
        _nameInput.Value="";_nameInput.Blur();
    }
    public override void Update(GameTime time)
    {
        if(Status==Client.Main.Models.GameControlStatus.NonInitialized) { base.Update(time); }
        if(!Visible)return;
        if(!UiThemeManager.IsMobileInterfaceActive){Cancel();return;}
        var window=UiScaler.WindowBounds;
        int width=Math.Min(500,window.Width-24),height=Math.Min(650,window.Height-30);
        ControlSize=ViewSize=new Point(width,height);X=window.Center.X-width/2;Y=window.Center.Y-height/2;
        _content=new Rectangle(X+14,Y+94,width-28,height-157);
        _nameInput.X=20;_nameInput.Y=96;_nameInput.ViewSize=new Point(width-120,32);_nameInput.ControlSize=_nameInput.ViewSize;
        _nameInput.Visible=_names;
        base.Update(time);
        var mouse=MuGame.Instance.UiMouseState;var previous=MuGame.Instance.PrevUiMouseState;
        if(mouse.LeftButton==ButtonState.Pressed && previous.LeftButton==ButtonState.Released && DisplayRectangle.Contains(mouse.Position))
        {_pressed=true;_dragged=false;_pressY=mouse.Y;_startScroll=_scroll;}
        if(_pressed)
        {
            Scene?.SetMouseInputConsumed();
            if(_content.Contains(mouse.Position) && Math.Abs(mouse.Y-_pressY)>8)_dragged=true;
            if(_dragged)_scroll=Math.Max(0,_startScroll+_pressY-mouse.Y);
            if(mouse.LeftButton==ButtonState.Released)
            {
                _pressed=false;
                if(!_dragged)for(int i=_actions.Count-1;i>=0;i--)if(_actions[i].Bounds.Contains(mouse.Position)){_actions[i].Click();break;}
            }
        }
        if(DisplayRectangle.Contains(mouse.Position) && mouse.ScrollWheelValue!=previous.ScrollWheelValue)
        {_scroll=Math.Max(0,_scroll-Math.Sign(mouse.ScrollWheelValue-previous.ScrollWheelValue)*RowHeight);Scene?.SetMouseInputConsumed();}
        if(MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape))Cancel();
    }
    public override void Draw(GameTime time)
    {
        if(!Visible)return;
        var batch=GraphicsManager.Instance.Sprite;var pixel=GraphicsManager.Instance.Pixel;
        using var scope=new SpriteBatchScope(batch,SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,transform:UiScaler.SpriteTransform);
        _actions.Clear();batch.Draw(pixel,DisplayRectangle,new Color(0,0,0,235));
        MobileHudLayout.Text(batch,"TwinMU Helper - Settings",new Rectangle(X+8,Y+8,ViewSize.X-16,32),20,Color.Goldenrod);
        string[] tabs={"Hunting","Recovery","Loot"};int tabWidth=(ViewSize.X-28)/3;
        for(int i=0;i<3;i++){int tab=i;Button(new Rectangle(X+14+i*tabWidth,Y+45,tabWidth-2,36),tabs[i],()=>{_tab=tab;_scroll=0;_names=false;_skillSelection=null;},_tab==i);}
        Button(new Rectangle(X+ViewSize.X/2-150,Y+ViewSize.Y-48,145,34),"Save",Save);
        Button(new Rectangle(X+ViewSize.X/2+5,Y+ViewSize.Y-48,145,34),"Cancel",Cancel);
        int row=0;
        void Label(string text,Rectangle bounds)=>MobileHudLayout.Text(batch,text,bounds,15,Color.White,false);
        void Row(string label,string value,Action click,ushort skill=0)
        {
            var bounds=new Rectangle(_content.X,_content.Y+row++*RowHeight-_scroll,_content.Width,RowHeight-4);
            if(bounds.Top<_content.Top || bounds.Bottom>_content.Bottom)return;
            Label(label,new Rectangle(bounds.X,bounds.Y,bounds.Width/2,RowHeight-4));
            var valueBounds=new Rectangle(bounds.X+bounds.Width/2,bounds.Y,bounds.Width/2,RowHeight-4);
            Button(valueBounds,value,click);
            if(skill!=0 && SkillIconAtlas.TryResolve(skill,SkillDatabase.GetSkillDefinition(skill),out var icon) && _atlases.TryGetValue(icon.TexturePath,out var texture) && texture!=null)
                batch.Draw(texture,new Rectangle(valueBounds.X+3,valueBounds.Y+5,25,35),SkillIconAtlas.ScaleToTexture(icon.SourceRectangle,texture,icon.AtlasSize),Color.White);
        }
        void ToggleRow(string label,Func<bool> get,Action<bool> set)=>Row(label,get()?"ON":"OFF",()=>set(!get()));
        void Stepper(string label,int value,Action<int> set,int minimum,int maximum,int step=1)
        {
            var bounds=new Rectangle(_content.X,_content.Y+row++*RowHeight-_scroll,_content.Width,RowHeight-4);
            if(bounds.Top<_content.Top || bounds.Bottom>_content.Bottom)return;
            Label(label,new Rectangle(bounds.X,bounds.Y,bounds.Width/2,RowHeight-4));
            Button(new Rectangle(bounds.Right-190,bounds.Y,40,40),"-",()=>set(Math.Max(minimum,value-step)));
            MobileHudLayout.Text(batch,value.ToString("N0"),new Rectangle(bounds.Right-148,bounds.Y,104,40),16,Color.Goldenrod);
            Button(new Rectangle(bounds.Right-40,bounds.Y,40,40),"+",()=>set(Math.Min(maximum,value+step)));
        }
        void Skill(string label,ushort id,Action<ushort> set)=>Row(label,id==0?"None":SkillDatabase.GetSkillName(id),()=>{_skillSelection=set;_scroll=0;},id);
        if(_skillSelection!=null)
        {
            Row("Choose skill","Clear",()=>{_skillSelection?.Invoke(0);_skillSelection=null;_scroll=0;});
            foreach(var skill in MuGame.Network.GetCharacterState().GetSkills())
            {ushort id=skill.SkillId;Row(SkillDatabase.GetSkillName(id),"Select",()=>{_skillSelection?.Invoke(id);_skillSelection=null;_scroll=0;},id);}
        }
        else if(_names)
        {
            _nameInput.Draw(time);
            Button(new Rectangle(X+ViewSize.X-95,Y+96,75,32),"Add",AddName);
            row=1;
            foreach(string name in _draft.ExtraItems.ToArray()){string entry=name;Row(entry,"Remove",()=>_draft.ExtraItems.Remove(entry));}
            Row("Named items","Back",()=>{_names=false;_nameInput.Blur();_scroll=0;});
        }
        else if(_tab==0)
        {
            Stepper("Hunting range",_draft.HuntingRange,v=>_draft.HuntingRange=v,1,15);
            ToggleRow("Counterattack",()=>_draft.LongRangeCounterAttack,v=>_draft.LongRangeCounterAttack=v);
            ToggleRow("Return to start",()=>_draft.ReturnToOriginalPosition,v=>_draft.ReturnToOriginalPosition=v);
            Skill("Main Skill",_draft.BasicSkillId,id=>_draft.BasicSkillId=id);
            Skill("Skill 2",_draft.ActivationSkill1.SkillId,id=>_draft.ActivationSkill1.SkillId=id);
            Skill("Skill 3",_draft.ActivationSkill2.SkillId,id=>_draft.ActivationSkill2.SkillId=id);
            ToggleRow("Use buff duration",()=>_draft.BuffDuration,v=>_draft.BuffDuration=v);
            for(int i=0;i<3;i++){int slot=i;Skill($"Buff {i+1}",_draft.BuffSkillIds[i],id=>_draft.BuffSkillIds[slot]=id);}
            ToggleRow("Basic attack fallback",()=>_draft.FallbackBasicAttack,v=>_draft.FallbackBasicAttack=v);
            ToggleRow("Use combo",()=>_draft.UseCombo,v=>_draft.UseCombo=v);
            ToggleRow("Use Dark Raven",()=>_draft.UseDarkRaven,v=>_draft.UseDarkRaven=v);
        }
        else if(_tab==1)
        {
            ToggleRow("Use HP potions",()=>_draft.UseHealPotion,v=>_draft.UseHealPotion=v);
            Stepper("HP potion %",_draft.PotionThreshold,v=>_draft.PotionThreshold=v,0,100,5);
            Stepper("MP potion %",_draft.ManaPotionThreshold,v=>_draft.ManaPotionThreshold=v,0,100,5);
            ToggleRow("Auto heal skill",()=>_draft.AutoHeal,v=>_draft.AutoHeal=v);
            Stepper("Heal at HP %",_draft.HealThreshold,v=>_draft.HealThreshold=v,0,100,5);
            ToggleRow("Drain Life",()=>_draft.UseDrainLife,v=>_draft.UseDrainLife=v);
            ToggleRow("Support party",()=>_draft.SupportParty,v=>_draft.SupportParty=v);
            ToggleRow("Heal party",()=>_draft.AutoHealParty,v=>_draft.AutoHealParty=v);
            Stepper("Party heal HP %",_draft.HealPartyThreshold,v=>_draft.HealPartyThreshold=v,0,100,5);
        }
        else
        {
            Stepper("Pickup range",_draft.ObtainingRange,v=>_draft.ObtainingRange=v,1,15);
            ToggleRow("Pick everything",()=>_draft.PickAllItems,v=>_draft.PickAllItems=v);
            ToggleRow("Zen",()=>_draft.PickZen,v=>_draft.PickZen=v);
            ToggleRow("Jewels",()=>_draft.PickJewel,v=>_draft.PickJewel=v);
            ToggleRow("Excellent",()=>_draft.PickExcellent,v=>_draft.PickExcellent=v);
            ToggleRow("Ancient",()=>_draft.PickAncient,v=>_draft.PickAncient=v);
            ToggleRow("Named items",()=>_draft.PickExtraItems,v=>_draft.PickExtraItems=v);
            Row("Named items",$"Edit List ({_draft.ExtraItems.Count})",()=>{_names=true;_scroll=0;});
            ToggleRow("Auto repair",()=>_draft.RepairItem,v=>_draft.RepairItem=v);
        }
        _scroll=Math.Min(_scroll,Math.Max(0,row*RowHeight-_content.Height));
        int totalHeight=row*RowHeight;
        if(totalHeight>_content.Height)
        {
            int thumbHeight=Math.Max(22,_content.Height*_content.Height/totalHeight);
            int thumbY=_content.Y+(int)((_content.Height-thumbHeight)*(_scroll/(float)(totalHeight-_content.Height)));
            batch.Draw(pixel,new Rectangle(_content.Right-3,_content.Y,3,_content.Height),new Color(55,51,39));
            batch.Draw(pixel,new Rectangle(_content.Right-3,thumbY,3,thumbHeight),Color.Goldenrod);
        }
        void Button(Rectangle bounds,string text,Action click,bool selected=false)
        {
            batch.Draw(pixel,bounds,new Color(83,72,43));
            batch.Draw(pixel,new Rectangle(bounds.X+1,bounds.Y+1,bounds.Width-2,bounds.Height-2),text=="ON"?new Color(9,49,24):text=="OFF"?new Color(49,12,12):selected?new Color(60,52,37):new Color(8,12,13,240));
            MobileHudLayout.Text(batch,text,bounds,14,text=="ON"?new Color(130,235,150):text=="OFF"?new Color(240,120,120):Color.Goldenrod);_actions.Add((bounds,click));
        }
    }
}
