using HistoryKit;
using Qud.API;
using System;
using System.Collections.Generic;
using XRL;
using XRL.Annals;
using XRL.Collections;
using XRL.Core;
using XRL.Wish;
using XRL.World;

[HasWishCommand]
public class PlayerSecretsMod : IPart
{
    public override void Initialize()
    {
        BlockHistoricSiteAutomaticReveal();
    }

    public override bool WantEvent(int ID, int cascade)
    {
        return ID == SecretVisibilityChangedEvent.ID;
    }

    private static Dictionary<string,int> _sultanPeriodTable = null;
    private static Dictionary<string,int> sultanPeriodTable
    {
        get
        {
            if (_sultanPeriodTable == null)
            {
                SortedList<long,string> sultanDeaths = new();
                foreach (HistoricEntity e in QudHistoryHelpers.GetSultans(The.Game.sultanHistory))
                {
                    sultanDeaths.Add(e.lastYear, e.id);
                }
                Dictionary<string,int> tbl = new();
                int nextPeriod = 1;
                foreach (string id in sultanDeaths.Values)
                {
                    tbl.Add(id, nextPeriod);
                    nextPeriod += 1;
                }
                GameLog.LogDebug(
                    $"Computed a sultan history {nextPeriod-1} periods long"
                );
                _sultanPeriodTable = tbl;
            }
            return _sultanPeriodTable;
        }
    }

    private static bool IsSecretLocation(int secretsFound, int secretsPerLoc, int maxSecrets)
    {
        return (secretsFound == maxSecrets) ||
               (secretsFound < maxSecrets && secretsFound % secretsPerLoc == 0);
    }

    public override bool HandleEvent(SecretVisibilityChangedEvent E)
    {
        if (E.Entry is JournalSultanNote evtJsn)
        {
            string evtSultanId = evtJsn.SultanID;
            if (sultanPeriodTable[evtSultanId] is int sultanPeriod)
            {
                int seenAllSultans = 0;
                int seenSameSultan = 0;
                foreach (JournalSultanNote jsn in JournalAPI.SultanNotes)
                {
                    if (jsn.Revealed && !jsn.Attributes.Contains("sultanTombPropaganda"))
                    {
                        seenAllSultans += 1;
                        if (jsn.SultanID == evtSultanId)
                        {
                            seenSameSultan += 1;
                        }
                    }

                }

                if (IsSecretLocation(
                    seenAllSultans,
                    APGame.Instance.Data.SultanSecretsPerLocation,
                    APGame.Instance.Data.SultanSecretsRequired
                ))
                {
                    APGame.Instance.CheckLocation($"{seenAllSultans} Sultan Lore");
                }

                if (sultanPeriod == 6)
                {
                    if(APGame.Instance.Data.ReshephSecretsRequired > 0 ?
                        IsSecretLocation(
                            seenSameSultan,
                            APGame.Instance.Data.ReshephSecretsPerLocation,
                            APGame.Instance.Data.ReshephSecretsRequired
                        ) :
                        IsSecretLocation(
                            seenSameSultan,
                            APGame.Instance.Data.PeriodSultanSecretsPerLocation,
                            APGame.Instance.Data.PeriodSultanSecretsRequired
                        )
                    )
                    {
                        APGame.Instance.CheckLocation($"{seenSameSultan} Resheph Lore");
                    }
                }
                else
                {
                    if (IsSecretLocation(
                        seenSameSultan,
                        APGame.Instance.Data.PeriodSultanSecretsPerLocation,
                        APGame.Instance.Data.PeriodSultanSecretsRequired
                    ))
                    {
                        string count = seenSameSultan == 1 ? "" : $"{seenSameSultan} ";
                        string periodName = sultanPeriod switch
                        {
                            1 => "First",
                            2 => "Second",
                            3 => "Third",
                            4 => "Fourth",
                            _ => "Fifth"
                        };
                        APGame.Instance.CheckLocation($"{count}Lore from the {periodName} Sultanate");
                    }
                }
            }
        }

        return true;
    }

    private static readonly string _shadowRevealsRegionProp = "lonesurv1vor_archipelago_blockedRevealsRegion";

    protected void BlockHistoricSiteAutomaticReveal()
    {
        int orderCutoff = APGame.Instance.Data.SultanSecretsPerLocation;
        foreach (HistoricEvent evt in The.Game.sultanHistory.events)
        {
            if (!evt.HasEventProperty("revealsRegion") || evt.HasEventProperty(_shadowRevealsRegionProp))
            {
                continue;
            }
            string regionName = evt.GetEventProperty("revealsRegion");
            string orderProp = The.Game.GetStringGameState($"SultanDungeonPlaced_{regionName}");
            if (
                regionName == null
                || regionName == "unknown"
                || orderProp == null
                || !int.TryParse(orderProp, out int order)
            )
            {
                continue;
            }
            if (order < orderCutoff)
            {
                evt.SetEventProperty("revealsRegion", null);
                evt.SetEventProperty(_shadowRevealsRegionProp, regionName);
            }
        }
    }

    public void RevealHistoricSite(int SiteID)
    {
        HistoricEvent activatedEvent = null;
        string targetRegion = The.Game.GetStringGameState($"SultanDungeonPlacementOrder_{SiteID}");
        if (targetRegion == null)
        {
            GameLog.LogError($"Couldn't find the region of historic site {SiteID}");
            return;
        }
        foreach (HistoricEvent evt in The.Game.sultanHistory.events)
        {
            if (evt.GetEventProperty(_shadowRevealsRegionProp) == targetRegion)
            {
                evt.SetEventProperty("revealsRegion", targetRegion);
                evt.SetEventProperty(_shadowRevealsRegionProp, null);
                activatedEvent = evt;
            }
        }
        if (activatedEvent != null)
        {
            activatedEvent.PerformRegionReveal();
        }
        else
        {
            GameLog.LogDebug($"Revealed no location for historic site {SiteID} : {targetRegion}");
        }
    }

    [WishCommand(Command = "ap_revealsite")]
    public static bool RevealHistoricSite(string SiteID)
    {
        if(The.Player.TryGetPart<PlayerSecretsMod>(out var sm))
        {
            sm.RevealHistoricSite(int.Parse(SiteID));
        }

        return true;
    }
}
