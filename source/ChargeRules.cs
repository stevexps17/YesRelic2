namespace YesRelic2;

// Pure rules shared with the regression tests. A charge buys one combat's multi-picks.
public static class ChargeRules
{
    public static bool CanUse(int remaining, int lastChargedCombat, int combat)
        => remaining < 0 || remaining > 0 || lastChargedCombat == combat;

    public static bool ShouldSpend(int successfulPicks, int remaining, int lastChargedCombat, int combat)
        => successfulPicks >= 2 && remaining > 0 && lastChargedCombat != combat;
}
