// Forsworn Director by soybean_alien
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForswornDirector;

// Flat +20 stat boosts, applied through the game's own BonusStats source system so they show up
// in the hero's stat breakdown as "Forsworn Director" and come off cleanly when toggled off.
// Bonus sources are rebuilt from items/talents on load and aren't saved, so nothing sticks.
internal static class Boosts
{
    internal const int Amount = 20;
    internal const string Tag = "Forsworn Director";

    internal static readonly (string Key, string Label, BonusStatsSource.Source Source)[] Stats =
    {
        ("Damage", "Damage", BonusStatsSource.Source.Damage),
        ("ShieldGeneration", "Shield Generation", BonusStatsSource.Source.Shielding),
        ("APGeneration", "AP Generation", BonusStatsSource.Source.APRegen),
        ("Armor", "Armor", BonusStatsSource.Source.Armor),
        ("UltimateGeneration", "Ultimate Generation", BonusStatsSource.Source.UltGen),
        ("Movement", "Movement", BonusStatsSource.Source.Movement),
        ("Thorns", "Thorns", BonusStatsSource.Source.Thorns),
        ("MaxHealth", "Max Health", BonusStatsSource.Source.MaxHealth),
    };

    private static bool _warned;

    // Last synced boost mask per BonusStats pointer. Walking the contributions dictionary costs
    // several IL2CPP calls per stat, so heroes are only re-synced when their wanted set changes,
    // plus a full check every 10 ticks to catch bonus objects the game rebuilt.
    private static readonly Dictionary<IntPtr, int> Synced = new();
    private static int _tick;

    internal static void Tick()
    {
        try
        {
            var verify = ++_tick % 10 == 0;
            if (Synced.Count > 64)
                Synced.Clear();

            foreach (var character in Plugin.Heroes)
            {
                var stats = character != null ? character.mechanicStats : null;
                var settings = stats != null ? Plugin.For(character.characterClass) : null;
                var bonus = settings != null ? stats.bonusStats : null;
                if (bonus == null)
                    continue;

                var wanted = Wanted(settings);
                if (!verify && Synced.TryGetValue(bonus.Pointer, out var last) && last == wanted)
                    continue;

                Sync(stats, settings);
                Synced[bonus.Pointer] = wanted;
            }
        }
        catch (Exception exception)
        {
            if (!_warned)
                Plugin.Instance.Log.LogWarning($"Stat boost sync failed: {exception}");
            _warned = true;
        }
    }

    private static int Wanted(ClassSettings settings)
    {
        if (!Plugin.EffectsEnabled)
            return 0;

        var mask = 0;
        for (var index = 0; index < Stats.Length; index++)
        {
            if (settings.Boosts[Stats[index].Key].Value)
                mask |= 1 << index;
        }

        return mask;
    }

    private static void Sync(MechanicStats stats, ClassSettings settings)
    {
        var bonus = stats != null ? stats.bonusStats : null;
        if (bonus == null)
            return;

        var active = Plugin.EffectsEnabled;
        foreach (var (key, _, source) in Stats)
        {
            var want = active && settings.Boosts[key].Value;
            var mine = FindMine(bonus, source);
            if (want && mine == null)
            {
                bonus.AddSource(new BonusStatsSource(source, Amount, Tag));
                if (FindMine(bonus, source) == null && !_warned)
                {
                    // Never re-add blindly: a missing contribution would stack +20 every tick.
                    _warned = true;
                    Plugin.Instance.Log.LogWarning($"Boost {key} did not register as a contribution; boosts disabled for safety.");
                    settings.Boosts[key].Value = false;
                }
            }
            else if (!want && mine != null)
            {
                bonus.RemoveSource(mine);
            }
        }
    }

    private static BonusStatsSource FindMine(BonusStats bonus, BonusStatsSource.Source source)
    {
        var contributions = bonus.contributions;
        if (contributions == null || !contributions.ContainsKey(source))
            return null;

        var list = contributions[source];
        if (list == null)
            return null;

        for (var index = 0; index < list.Count; index++)
        {
            var item = list[index];
            if (item != null && item.name == Tag)
                return item;
        }

        return null;
    }
}

// Upgrade Tokens are tracked per hero in AbilityUpgradeManager's synced dictionary.
internal static class UpgradeTokens
{
    internal static void Tick()
    {
        if (!Plugin.EffectsEnabled || !Plugin.InfiniteUpgradeTokens.Value)
            return;

        try
        {
            // Run balance: CharacterClass.Metadata.extraUpgradeTokens (saved as HeroSaveData.upgradeTokenCount).
            foreach (var character in Plugin.Heroes)
            {
                var characterClass = character != null ? character.characterClass : null;
                if (characterClass == null || characterClass.metadata == null)
                    continue;

                var current = characterClass.metadata.extraUpgradeTokens;
                if (current != Plugin.CurrencyReserve)
                {
                    characterClass.metadata.extraUpgradeTokens = Plugin.CurrencyReserve;
                    Once($"Upgrade tokens topped up ({characterClass.heroClassName}: {current} -> {Plugin.CurrencyReserve}).");
                }
            }

            // Upgrade events keep their own synced copy while they're open.
            var manager = UnityEngine.Object.FindObjectOfType<AbilityUpgradeManager>();
            var tokens = manager != null ? manager.upgradeTokens : null;
            if (tokens == null)
                return;

            foreach (HeroClassRegistry.Name name in Enum.GetValues(typeof(HeroClassRegistry.Name)))
            {
                if (tokens.ContainsKey(name) && tokens[name] != Plugin.CurrencyReserve)
                    tokens[name] = Plugin.CurrencyReserve;
            }
        }
        catch (Exception exception)
        {
            Once($"Upgrade token top-up failed: {exception}");
        }
    }

    private static readonly HashSet<string> Logged = new();

    private static void Once(string message)
    {
        if (Logged.Add(message))
            Plugin.Instance.Log.LogInfo(message);
    }
}
