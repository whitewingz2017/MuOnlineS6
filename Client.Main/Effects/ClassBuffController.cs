#nullable enable
using System.Collections.Generic;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Models;
using Client.Main.Objects.Effects;
using Client.Main.Objects.Player;
using Client.Main.Scenes;

namespace Client.Main.Effects
{
    /// <summary>Reconciles server buff state with players entering or leaving the current world.</summary>
    public sealed class ClassBuffController
    {
        private readonly BuffManager _buffs;
        private readonly HashSet<ushort> _players = new();
        private readonly Dictionary<ushort, ClassBuffEffect> _visuals = new();
        private readonly List<ushort> _remove = new();

        public ClassBuffController(BuffManager buffs)
        {
            _buffs = buffs;
            buffs.BuffStateChanged += OnBuffChanged;
        }

        private void OnBuffChanged(object? sender, BuffStateChangedEventArgs e)
        {
            if (e.EffectId is not (BuffEffectId.ManaShield or BuffEffectId.CriticalDamage or BuffEffectId.CriticalDamageMastery or BuffEffectId.Berserk or BuffEffectId.Reflection))
                return;
            ushort playerId = (ushort)(e.PlayerId & 0x7FFF);
            MuGame.ScheduleOnMainThread(() =>
            {
                if (_buffs.HasBuff(playerId, BuffEffectId.ManaShield) || _buffs.HasBuff(playerId, BuffEffectId.CriticalDamage) || _buffs.HasBuff(playerId, BuffEffectId.CriticalDamageMastery) || _buffs.HasBuff(playerId, BuffEffectId.Berserk) || _buffs.HasBuff(playerId, BuffEffectId.Reflection))
                    _players.Add(playerId);
                else
                    _players.Remove(playerId);
            });
        }

        public void Update()
        {
            var scene = MuGame.Instance?.ActiveScene as GameScene;
            var world = scene?.World as WalkableWorldControl;
            _remove.Clear();
            foreach (var pair in _visuals)
            {
                var effect = pair.Value;
                if (!_players.Contains(pair.Key) || world == null || effect.World != world ||
                    effect.Status == GameControlStatus.Disposed || effect.Owner.IsDead ||
                    !ReferenceEquals(FindPlayer(world, scene!, pair.Key), effect.Owner))
                    _remove.Add(pair.Key);
            }
            foreach (ushort id in _remove)
            {
                var effect = _visuals[id];
                effect.World?.RemoveObject(effect);
                effect.Dispose();
                _visuals.Remove(id);
            }
            if (world?.Status != GameControlStatus.Ready || scene == null)
                return;
            foreach (ushort id in _players)
            {
                if (_visuals.ContainsKey(id))
                    continue;
                var player = FindPlayer(world, scene, id);
                if (player?.Status != GameControlStatus.Ready || player.IsDead)
                    continue;
                var effect = new ClassBuffEffect(player, _buffs);
                _visuals.Add(id, effect);
                world.Objects.Add(effect);
                _ = effect.Load();
            }
        }

        private static PlayerObject? FindPlayer(WalkableWorldControl world, GameScene scene, ushort id) =>
            scene.Hero != null && (scene.Hero.NetworkId & 0x7FFF) == id ? scene.Hero : world.FindPlayerById(id);

        public void Clear()
        {
            foreach (ushort id in _players)
                _buffs.ClearPlayerBuffs(id);
            _players.Clear();
            foreach (var effect in _visuals.Values)
            {
                effect.World?.RemoveObject(effect);
                effect.Dispose();
            }
            _visuals.Clear();
        }
    }
}
