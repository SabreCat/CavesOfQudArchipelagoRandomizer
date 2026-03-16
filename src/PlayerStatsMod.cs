using HarmonyLib;
using Qud.UI;
using System.Collections.Generic;
using XRL;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Parts;

public class PlayerStatsMod : IPart
{
    public override bool WantEvent(int ID, int cascade)
    {
        return ID == AwardedXPEvent.ID || ID == GetLevelUpPointsEvent.ID;
    }

    public override bool HandleEvent(AwardedXPEvent E)
    {
        Dictionary<int, string> xpTable = new();
        for (int level = 1; level < APGame.Instance.Data.MaxLevel; level++)
        {
            var curXP = Leveler.GetXPForLevel(level);
            var diffXP = Leveler.GetXPForLevel(level + 1) - curXP;
            for (int step = 0; step < APGame.Instance.Data.LocationsPerLevel; step++)
            {
                if (level == 0 && step == 1)
                    continue;

                var stepXP = curXP + diffXP * step / APGame.Instance.Data.LocationsPerLevel;
                xpTable.Add(stepXP, $"Level {level}.{step}");
            }
        }
        xpTable.Add(
            Leveler.GetXPForLevel(APGame.Instance.Data.MaxLevel),
            $"{APGame.Instance.Data.MaxLevel}.0"
        );

        foreach (var (xp, loc) in xpTable)
        {
            if (xp > E.AmountBefore && xp <= E.AmountBefore + E.Amount)
            {
                APGame.Instance.CheckLocation(loc);
            }
        }

        return true;
    }

    public override bool HandleEvent(GetLevelUpPointsEvent E)
    {
        E.HitPoints = 0;
        E.AttributePoints = 0;
        E.AttributeBonus = 0;
        E.MutationPoints = 0;
        E.SkillPoints = 0;
        E.RapidAdvancement = 0;

        return true;
    }

    public void AddHitPoints()
    {
        // TODO the usual retroactive application when toughness raises
        var leveler = ParentObject.GetPart<Leveler>();
        int gain = leveler.RollHP(ParentObject.genotypeEntry.BaseHPGain);
        ParentObject.GetStat("Hitpoints").BaseValue += gain;

        GameLog.LogGameplay($"Granted {gain} hitpoints!", APLocalOptions.PopupOnReceivedItem);
    }

    public void AddAttributePoints()
    {
        ParentObject.GetStat("AP").BaseValue += 1;

        GameLog.LogGameplay("Granted one attribute point!", APLocalOptions.PopupOnReceivedItem);
    }

    public void AddAttributeBonus()
    {
        ParentObject.GetStat("Strength").BaseValue += 1;
        ParentObject.GetStat("Intelligence").BaseValue += 1;
        ParentObject.GetStat("Willpower").BaseValue += 1;
        ParentObject.GetStat("Agility").BaseValue += 1;
        ParentObject.GetStat("Toughness").BaseValue += 1;
        ParentObject.GetStat("Ego").BaseValue += 1;

        GameLog.LogGameplay("Granted one of each attribute!", APLocalOptions.PopupOnReceivedItem);
    }

    public void AddLicensePoints(int amount)
    {
        ParentObject.ModIntProperty("CyberneticsLicenses", amount, false);
        GameLog.LogGameplay($"Granted {amount} cybernetics license points!", APLocalOptions.PopupOnReceivedItem);
    }

    public void AddMutationPoints()
    {
        var leveler = ParentObject.GetPart<Leveler>();
        if (ParentObject.IsMutant())
        {
            // TODO this is not quite correct, use GetFor
            int gain = leveler.RollMP(ParentObject.genotypeEntry.BaseMPGain);
            ParentObject.GainMP(gain);
            GameLog.LogGameplay($"Granted {gain} mutation points!", APLocalOptions.PopupOnReceivedItem);
        }
        else
        {
            // Player rolled a True Kin but left the YAML option as Mutant
            GameLog.LogGameplay("Ignored received mutation points as you are not a mutant");
        }
    }

    public void AddSkillPoints()
    {
        // TODO the usual retroactive application when intelligence raises
        var leveler = ParentObject.GetPart<Leveler>();
        int gain = leveler.RollSP(ParentObject.genotypeEntry.BaseSPGain);
        ParentObject.GetStat("SP").BaseValue += gain;
        GameLog.LogGameplay($"Granted {gain} skill points!", APLocalOptions.PopupOnReceivedItem);
    }

    public void RapidMutationAdvancement()
    {
        var leveler = ParentObject.GetPart<Leveler>();
        if (ParentObject.IsEsper())
        {
            ParentObject.GetStat("Ego").BaseValue += 1;
            GameLog.LogGameplay("Granted +1 Ego!", APLocalOptions.PopupOnReceivedItem);
        }
        else if (ParentObject.IsMutant())
        {
            Leveler.RapidAdvancement(3, ParentObject);
            // A popup already appears when receiving rapid mutation, so just log it
            GameLog.LogGameplay($"Granted a rapid mutation advancement!", false);
        }
        else
        {
            // Player rolled a True Kin but left the YAML option as Mutant
            GameLog.LogGameplay("Ignored rapid mutation advancement as you are not a mutant");
        }
    }
}

namespace Archipelago.HarmonyPatches
{
    [HarmonyPatch(typeof(Qud.UI.CyberneticsTerminalScreen), nameof(Qud.UI.CyberneticsTerminalScreen.HandleSelect))]
    class Patch
    {
        static bool Prefix(CyberneticsTerminalLineData element)
        {
            if (element.Text == "Upgrade Your License")
            {
                Popup.Show("The machine beeps plaintively, unable to reach its long-dead licensing authority.");
                return false;
            }
            return true;
        }
    }
}
