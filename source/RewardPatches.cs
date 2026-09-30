using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rewards;

namespace YesRelic2;

internal sealed class RewardSession
{
    public readonly CardReward Reward;
    public readonly YesRelic Relic;
    public int Picks;
    public int RenderedPicks;
    public bool Busy;
    public List<CardRewardAlternative> Alternatives;
    public RewardSession(CardReward reward, YesRelic relic) { Reward = reward; Relic = relic; }
    public void PickSucceeded()
    {
        Picks++;
        Relic.OnSuccessfulPick(Picks);
        Reward.CanReroll = false;
        // The vanilla reward loop retains this list: mutate it rather than replacing its reference.
        // Once a card was taken, skip/heal/sacrifice/reroll are mutually exclusive with the card reward.
        if (Alternatives != null)
        {
            Alternatives.Clear();
            Alternatives.Add(new CardRewardAlternative("YESRELIC_DONE", PostAlternateCardRewardAction.EndSelectionAndCompleteReward));
        }
    }
}

[HarmonyPatch]
internal static class RewardPatches
{
    internal static RewardSession Active;
    static readonly System.Reflection.PropertyInfo Options = AccessTools.Property(typeof(CardReward), "Options");
    internal static bool Eligible(CardReward reward)
    {
        if (!Entry.IsSinglePlayer(reward.Player.RunState)) return false;
        var relic = reward.Player.GetRelic<YesRelic>();
        return relic != null && !relic.IsMelted && relic.CanUse &&
            ((CardCreationOptions)Options.GetValue(reward)).Source == CardCreationSource.Encounter;
    }

    [HarmonyPatch(typeof(CardReward), "OnSelect"), HarmonyPrefix]
    static void Begin(CardReward __instance, out RewardSession __state)
    {
        __state = null;
        if (Active != null || !Eligible(__instance)) return;
        __state = Active = new RewardSession(__instance, __instance.Player.GetRelic<YesRelic>());
    }

    [HarmonyPatch(typeof(CardReward), "OnSelect"), HarmonyPostfix]
    static void End(ref Task<bool> __result, RewardSession __state)
    {
        if (__state != null) __result = Cleanup(__result, __state);
    }

    static async Task<bool> Cleanup(Task<bool> original, RewardSession session)
    {
        try { return await original; }
        finally { if (Active == session) Active = null; }
    }

    [HarmonyPatch(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate)), HarmonyPostfix]
    static void Alternatives(CardReward cardReward, IReadOnlyList<CardRewardAlternative> __result)
    {
        if (Active?.Reward == cardReward) Active.Alternatives = __result as List<CardRewardAlternative>;
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), new Type[] {
        typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) }), HarmonyPrefix]
    static void BeforeAdd(CardModel card, PileType newPileType, out RewardSession __state)
    {
        __state = Active != null && newPileType == PileType.Deck && Active.Reward.Cards.Contains(card) ? Active : null;
        if (__state != null) __state.Busy = true;
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), new Type[] {
        typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) }), HarmonyPostfix]
    static void AfterAdd(ref Task<CardPileAddResult> __result, RewardSession __state)
    {
        if (__state != null) __result = TrackAdd(__result, __state);
    }

    static async Task<CardPileAddResult> TrackAdd(Task<CardPileAddResult> task, RewardSession session)
    {
        try
        {
            var result = await task;
            if (result.success) session.PickSucceeded();
            return result;
        }
        finally { session.Busy = false; }
    }

    [HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.OptionSelected)), HarmonyPrefix]
    static void Refresh(NCardRewardSelectionScreen __instance)
    {
        var session = Active;
        if (session == null || session.Picks == session.RenderedPicks || session.Alternatives == null) return;
        session.RenderedPicks = session.Picks;
        var cards = (List<CardCreationResult>)AccessTools.Field(typeof(CardReward), "_cards").GetValue(session.Reward);
        __instance.RefreshOptions(cards, session.Alternatives);
    }

    [HarmonyPatch(typeof(NCardRewardSelectionScreen), "SelectCard"), HarmonyPrefix]
    static bool GuardCard(NCardRewardSelectionScreen __instance, NCardHolder cardHolder)
    {
        if (Active == null) return true;
        return Ready(__instance) && Active.Reward.Cards.Contains(cardHolder.CardModel);
    }

    [HarmonyPatch(typeof(NCardRewardSelectionScreen), "OnAlternateRewardSelected"), HarmonyPrefix]
    static bool GuardAlternative(NCardRewardSelectionScreen __instance, int index)
        => Active == null || (Ready(__instance) && index >= 0 && index < (Active.Alternatives?.Count ?? 0));

    static bool Ready(NCardRewardSelectionScreen screen)
    {
        var completion = AccessTools.Field(typeof(NCardRewardSelectionScreen), "_completionSource").GetValue(screen) as TaskCompletionSource<int?>;
        return !Active.Busy && completion != null && !completion.Task.IsCompleted;
    }
}
