using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YesRelic2;

public sealed class YesRelic : RelicModel
{
    [SavedProperty] public int RemainingUses { get; set; } = -1;
    [SavedProperty] public int CombatSerial { get; set; }
    [SavedProperty] public int LastChargedCombat { get; set; } = -1;
    [SavedProperty] public bool ChargesInitialized { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;
    // Valid built-in paths provide a fallback for preloading. The displayed artwork is our embedded SVG.
    protected override string IconBaseName => "circlet";
    public override bool ShowCounter => RemainingUses >= 0;
    public override int DisplayAmount => System.Math.Max(0, RemainingUses);
    public override bool IsUsedUp => RemainingUses == 0 && LastChargedCombat != CombatSerial;
    public override bool IsAllowed(IRunState state) => Entry.IsSinglePlayer(state) &&
        Settings.Current.AddToCommonPool && !System.Linq.Enumerable.Any(state.Players, p => p.GetRelic<YesRelic>() != null);

    protected override void AfterCloned()
    {
        base.AfterCloned();
        if (!ChargesInitialized)
        {
            RemainingUses = Settings.Current.LimitedUses ? Settings.Current.UseCount : -1;
            ChargesInitialized = true;
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CombatSerial++;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    internal bool CanUse => ChargeRules.CanUse(RemainingUses, LastChargedCombat, CombatSerial);

    internal void OnSuccessfulPick(int count)
    {
        if (!ChargeRules.ShouldSpend(count, RemainingUses, LastChargedCombat, CombatSerial)) return;
        RemainingUses--;
        LastChargedCombat = CombatSerial;
        InvokeDisplayAmountChanged();
        Flash();
    }

    public override bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward reward)
        => player == Owner && RewardPatches.Active?.Reward == reward && CanUse &&
           System.Linq.Enumerable.Count(reward.Cards) > 1;
}
