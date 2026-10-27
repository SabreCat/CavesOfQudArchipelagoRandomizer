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
        return ID == SecretVisibilityChangedEvent.ID
               || ID == ZoneActivatedEvent.ID;
    }

    public static List<string> historicSiteNames = new List<string>
    {
        // order should match world generation order (low->high tier)
        "Historic Site of the Fifth Sultanate",
        "Older Historic Site of the Fifth Sultanate",
        "Historic Site of the Fourth Sultanate",
        "Older Historic Site of the Fourth Sultanate",
        "Historic Site of the Third Sultanate",
        "Older Historic Site of the Third Sultanate",
        "Historic Site of the Second Sultanate",
        "Historic Site of the First Sultanate",
    }

    private static Dictionary<string,int> _historicSiteNamesToIndex = null;
    public static Dictionary<string,int> historicSiteNamesToIndex
    {
        get
        {
            if (_historicSiteNamesToIndex == null)
            {
                Dictionary<string,int> tbl = new();
                for (int i = 0; i < historicSiteNames.Count; i++)
                {
                    tbl.Add(historicSiteNames[i], i);
                }
                _historicSiteNamesToIndex = tbl;
            }
            return _historicSiteNamesToIndex;
        }
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
                _sultanPeriodTable = tbl;
            }
            return _sultanPeriodTable;
        }
    }

    private static bool IsSecretLocation(int secretsFound, int secretsPerLoc, int maxSecrets)
    {
        return secretsFound == maxSecrets
               || secretsFound < maxSecrets && secretsFound % secretsPerLoc == 0;
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
        int dungeonCount = APGame.Instance.Data.SultanSecretsPerLocation;
        foreach (HistoricEvent evt in The.Game.sultanHistory.events)
        {
            if (!evt.HasEventProperty("revealsRegion") || evt.HasEventProperty(_shadowRevealsRegionProp))
            {
                continue;
            }
            string regionName = evt.GetEventProperty("revealsRegion");
            string strDungeonIndex = ;
            if (
                regionName == null
                || regionName == "unknown"
                || !int.TryParse(The.Game.GetStringGameState($"SultanDungeonPlaced_{regionName}"), out int dungeonIndex)
            )
            {
                continue;
            }
            if (dungeonIndex < dungeonCount)
            {
                evt.SetEventProperty("revealsRegion", null);
                evt.SetEventProperty(_shadowRevealsRegionProp, regionName);
            }
        }
    }

    public void RevealHistoricSite(int SiteIndex)
    {
        string targetRegion = The.Game.GetStringGameState($"SultanDungeonPlacementOrder_{SiteIndex}");
        HistoricEvent activatedEvent = null;
        if (targetRegion == null)
        {
            GameLog.LogError($"Couldn't find the region of historic site {SiteIndex}");
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
            GameLog.LogDebug($"Revealed no location for historic site {SiteIndex} : {targetRegion}");
        }
    }

    [WishCommand(Command = "ap_revealsite")]
    public static bool RevealHistoricSite(string SiteIndex)
    {
        if(The.Player.TryGetPart<PlayerSecretsMod>(out var sm))
        {
            sm.RevealHistoricSite(int.Parse(SiteIndex));
        }

        return true;
    }

    public bool HandleEvent(ZoneActivatedEvent E)
    {
        string historicSite = The.ZoneManager.GetZoneProperty("HistoricSite");
        if (
            !historicSite.IsNullOrEmpty()
            && int.TryParse(The.Game.GetStringGameState($"SultanDungeonPlaced_{historicSite}"); , out int dungeonIndex)
        )
        {
            MaybeCheckVisitedHistoricSite(dungeonIndex);

            string zoneBelowId = The.ZoneManager.GetZoneFromIDAndDirection(E.Zone.ZoneID, "d");
            if (
                !The.ZoneManager.TryGetZoneProperty(zoneBelowId, "HistoricSite", out string belowHistoricSite)
                && belowHistoricSite == historicSite
            )
            {
                MaybeCheckExploredHistoricSite(dungeonIndex);
            }
        }
    }

    public void MaybeCheckVisitedHistoricSite(int dungeonIndex)
    {
        if (dungeonIndex < APGame.Instance.Data.HistoricSiteVisitLocations)
        {
            APGame.Instance.CheckLocation($"Visit {historicSiteNames[dungeonIndex]}");
        }

        if (dungeonIndex < APGame.Instance.Data.HistoricSiteExploreLocations)
        {
            APGame.Instance.CheckLocation($"Explore {historicSiteNames[dungeonIndex]}");
        }
    }
}
