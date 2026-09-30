using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Localization;

namespace YesRelic2;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string Version = "0.1.1";
    public const string TestedHash = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    public static bool Compatible { get; private set; }

    public static void Initialize()
    {
        using var stream = File.OpenRead(typeof(RunManager).Assembly.Location);
        using var sha = SHA256.Create();
        Compatible = Convert.ToHexString(sha.ComputeHash(stream)) == TestedHash;
        Settings.Load();
        // Register even if compatibility changes, so the model identity remains known to saves.
        ModHelper.AddModelToPool<SharedRelicPool, YesRelic>();
        new Harmony("local.YesRelic2").PatchAll(Assembly.GetExecutingAssembly());
        Callable.From(Settings.AttachUi).CallDeferred();
        GD.Print($"[YesRelic2 {Version}] Loaded. Supported assembly: {Compatible}. F9 opens settings.");
        if (!Compatible) GD.PrintErr("[YesRelic2] Unverified game assembly: reward changes and starting grant are disabled.");
    }

    public static bool IsSinglePlayer(IRunState state) => Compatible && state != null && state.Players.Count == 1 &&
        RunManager.Instance?.NetService?.Type == NetGameType.Singleplayer;

    public static bool Chinese => LocManager.Instance?.Language is "zhs" or "zht";
    public static string L(string zh, string en) => Chinese ? zh : en;
}

[HarmonyPatch(typeof(RunManager), "InitializeNewRun")]
internal static class StartingRelicPatch
{
    static void Postfix(RunManager __instance)
    {
        var state = __instance.DebugOnlyGetState();
        if (!Entry.IsSinglePlayer(state) || !Settings.Current.StartWithRelic) return;
        var player = state.Players[0];
        if (player.GetRelic<YesRelic>() != null) return;
        var relic = (YesRelic)ModelDb.Relic<YesRelic>().ToMutable();
        relic.FloorAddedToDeck = 1;
        player.AddRelicInternal(relic);
        player.RelicGrabBag.Remove(relic);
        state.SharedRelicGrabBag.Remove(relic);
        SaveManager.Instance.MarkRelicAsSeen(relic);
    }
}

[HarmonyPatch(typeof(LocManager), nameof(LocManager.GetTable))]
internal static class LocalizationPatch
{
    static void Postfix(string name, LocTable __result)
    {
        if (name == "relics")
            __result.MergeWith(new Dictionary<string, string> {
                ["YES_RELIC.title"] = "Yes!",
                ["YES_RELIC.description"] = Entry.L(
                    "战斗卡牌奖励可以选择任意多张。拿牌后点击“完成选择”放弃剩余牌。有限次数时，同一场战斗首次从一组奖励中拿到第2张牌消耗1次，其余多选共享该次额度。",
                    "You may take any number of cards from combat card rewards. Choose Done to leave the rest. With limited uses, taking a second card from a reward spends one use; all rewards from that combat share it."),
                ["YES_RELIC.flavor"] = Entry.L("这张想要，那张也想要。", "This one. And that one. Yes!")
            });
        else if (name == "card_reward_ui")
            __result.MergeWith(new Dictionary<string, string> {
                ["OPTION_YESRELIC_DONE.name"] = Entry.L("完成选择", "Done")
            });
    }
}

