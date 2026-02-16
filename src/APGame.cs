using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.UI;
using XRL.World;

[Serializable]
public class PersistentData : IComposite
{
    // TODO give these private setters

    public string Host;
    public string Name;
    public string Password;
    public bool LastConnectionSuccessful = false;
    public string Seed;

    public int Goal;
    public int MaxLevel;
    public int LocationsPerLevel;

    public int ItemIndex = 0;
    public int RandomObjectIndex = 0;

    [NonSerialized] // Custom as Queue does not support Json serialization
    public Queue<Item> DelayedItems = new();
    public Dictionary<string, long> MissingLocations = new();
    public Dictionary<string, long> CheckedLocations = new();

    public void Write(SerializationWriter Writer)
    {
        List<Item> list = new(DelayedItems);
        Writer.Write(list);
    }

    public void Read(SerializationReader Reader)
    {
        List<Item> list = Reader.ReadList<Item>();
        DelayedItems = new(list);
    }
}

public static class APLocalOptions
{
    public static bool PopupOnReceivedItem =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionPopupOnReceivedItem");
    public static bool PopupOnReceivedTrap =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionPopupOnReceivedTrap");
    public static bool AllowTrapsInSettlements =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionAllowTrapsInSettlements");
    public static bool RetriggerTraps =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionRetriggerTraps");
    public static bool DeathLink =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionDeathLink");
    public static bool EnableDebugLog =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionEnableDebugLog");
}

[Serializable]
public class APGame : IPlayerSystem
{
    public static APGame Instance => The.Game.GetSystem<APGame>();
    public PersistentData Data = new();


    public bool Setup()
    {
        try
        {
            Dictionary<string, object> slotData = new();

            while (true)
            {
                APSession.Disconnect();
                APEvents.ClearEvents();

                if (!Data.LastConnectionSuccessful)
                {
                    if (!AskConnectionInfo(out string host, out string name, out string password))
                    {
                        Popup.Show("Missing connection info");
                        return false;
                    }

                    Data.Host = host;
                    Data.Name = name;
                    Data.Password = password;
                }

                Data.LastConnectionSuccessful = APSession.Connect(
                    Data.Host,
                    Data.Name,
                    Data.Password,
                    out slotData,
                    out string[] errors
                );

                if (!Data.LastConnectionSuccessful)
                {
                    GameLog.LogError(
                        $"Couldn't connect to the archipelago server:\n\n{string.Join("\n", errors)}",
                        true
                    );
                    continue;
                }

                if (Data.Seed != null && Data.Seed != APSession.Seed)
                {
                    Data.LastConnectionSuccessful = false;
                    GameLog.LogError(
                        "Incompatible Save: The connected rooms seed has changed. This save game has been used for a different room and is not compatible.",
                        true
                    );
                    continue;
                }

                break;
            }

            Data.Seed = APSession.Seed;

            if (
                !slotData.TryGetValue("goal", out object goal)
                || !slotData.TryGetValue("locations_per_level", out object locationsPerLevel)
            )
            {
                GameLog.LogError("Couldn't fill slot data", true);
                return false;
            }

            Data.Goal = (int)(long)goal;
            Data.MaxLevel = (int)(long)50; // TODO TMP
            Data.LocationsPerLevel = (int)(long)locationsPerLevel;

            SyncLocations();

            if (APLocalOptions.DeathLink)
                APSession.EnableDeathLink();
        }
        catch (Exception E)
        {
            GameLog.DisplayException(E);
            return false;
        }

        return true;
    }

    private bool AskConnectionInfo(out string host, out string name, out string password)
    {
        name = null;
        password = null;

        host = Popup.AskString(
            "Archipelago host?",
            Default: Data.Host ?? "localhost:38281",
            ReturnNullForEscape: true
        );

        if (host == null)
            return false;

        name = Popup.AskString(
            "Archipelago slot name?",
            Default: Data.Name ?? "Player1",
            ReturnNullForEscape: true
        );

        if (name == null)
            return false;

        password = Popup.AskString(
            "Archipelago password?",
            Default: Data.Password ?? "",
            ReturnNullForEscape: true
        );

        if (password == null)
            return false;

        return true;
    }

    public bool IsLocation(string loc)
    {
        return Data.MissingLocations.ContainsKey(loc) || Data.CheckedLocations.ContainsKey(loc);
    }

    public bool LocationChecked(string loc)
    {
        if (Data.MissingLocations.ContainsKey(loc))
        {
            return false;
        }
        else if (Data.CheckedLocations.ContainsKey(loc))
        {
            return true;
        }
        else
        {
            throw new Exception($"Location '{loc}' is neither missing nor checked");
        }
    }

    public void CheckLocation(string loc)
    {
        try
        {
            if (!LocationChecked(loc))
            {
                long id = Data.MissingLocations[loc];
                APSession.CheckLocations(id);
                Data.MissingLocations.Remove(loc);
                Data.CheckedLocations.Add(loc, id);
                GameLog.LogDebug($"Checked location '{loc}'");
            }
            else
            {
                GameLog.LogDebug($"Ignoring already checked location '{loc}'");
            }
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
        }
    }

    public bool HasReceivedItem(string name)
    {
        try
        {
            return APSession.ReceivedItems.Any(it => it.Item2 == name);
        }
        catch (NullReferenceException)
        {
            return false;
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
            return false;
        }
    }

    private Random _objectRandom = null;

    // Only meant for received AP items (items or traps) - uses its own randomizer to always obtain the same results, even after reload
    public PopulationResult GetRandomFromPopulation(string population)
    {
        if (_objectRandom == null)
        {
            if (Data.Seed == null || Data.Seed == "")
            {
                throw new Exception("Seed not initialized yet");
            }

            _objectRandom = XRL.Rules.Stat.GetSeededRandomGenerator(Data.Seed);
            for (int i = 0; i < Data.RandomObjectIndex; i++)
            {
                _objectRandom.Next();
            }
        }

        if (!PopulationManager.Populations.ContainsKey(population))
        {
            throw new Exception($"Unknown population {population}");
        }

        var res = PopulationManager
            .Populations[population]
            // TODO this doesn't really work as expected as Generate() also contains randomness
            .Generate()
            .GetRandomElement(_objectRandom);
        Data.RandomObjectIndex++;
        return res;
    }

    public void SetGoalAchieved()
    {
        APSession.SetGoalAchieved();
    }

    public void SyncLocations()
    {
        try
        {
            var checkedLocations = Data.CheckedLocations.Values;
            APSession.CheckLocations(checkedLocations.ToArray());

            Dictionary<string, long> newCheckedLocations = new();
            foreach (var (id, name) in APSession.CheckedLocations)
            {
                newCheckedLocations.Add(name, id);
            }
            Dictionary<string, long> newMissingLocations = new();
            foreach (var (id, name) in APSession.MissingLocations)
            {
                newMissingLocations.Add(name, id);
            }

            Data.MissingLocations = newMissingLocations;
            Data.CheckedLocations = newCheckedLocations;

            GameLog.LogDebug(
                $"Locations synced: {newCheckedLocations.Count()}/{newMissingLocations.Count() + newCheckedLocations.Count()}"
            );
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
        }
    }

    public override void Register(XRLGame game, IEventRegistrar registrar)
    {
        registrar.Register(EndTurnEvent.ID);
        registrar.Register(ZoneActivatedEvent.ID);
        base.Register(game, registrar);
    }

    public override void RegisterPlayer(GameObject player, IEventRegistrar registrar)
    {
        registrar.Register(AfterDieEvent.ID);
        registrar.Register(BeforeRenderEvent.ID);
        base.RegisterPlayer(player, registrar);
    }

    public override bool HandleEvent(AfterDieEvent E)
    {
        if (APLocalOptions.DeathLink && (E.Reason == null || !E.Reason.StartsWith("DeathLink")))
            APSession.SendDeathLink(Data.Name, E.Reason ?? "Unknown reason");

        return base.HandleEvent(E);
    }

    public override bool HandleEvent(EndTurnEvent E)
    {
        // This does nothing if already set, so it's fine.
        // TODO but this is not the best place for switching deathlink
        if (APLocalOptions.DeathLink)
            APSession.EnableDeathLink();
        else
            APSession.DisableDeathLink();

        return base.HandleEvent(E);
    }

    public override bool HandleEvent(BeforeRenderEvent E)
    {
        ProcessMessages();
        ProcessDeathLink();
        ProcessReceivedItems();

        return base.HandleEvent(E);
    }

    public override bool HandleEvent(ZoneActivatedEvent E)
    {
        if (
            !E.Zone.IsWorldMap()
            && (APLocalOptions.AllowTrapsInSettlements || !E.Zone.IsCheckpoint())
        )
        {
            while (Data.DelayedItems.TryDequeue(out Item item))
            {
                Items.HandleReceivedItem(item);
            }
        }

        return base.HandleEvent(E);
    }

    private void ProcessMessages()
    {
        while (APEvents.Messages.TryDequeue(out QueuedLogMessage m))
        {
            if (m.Popup)
            {
                Popup.Show(m.Message, LogMessage: true);
            }
            else
            {
                XRL.Messages.MessageQueue.AddPlayerMessage(m.Message);
            }
        }
    }

    private void ProcessDeathLink()
    {
        if (!APLocalOptions.DeathLink)
            return;

        (string source, string cause) = APEvents.GetDeathLink();

        if (source != null && source != "")
        {
            The.Player.Die(null, null, $"DeathLink from {source}: {cause}");
        }
    }


    private void ProcessReceivedItems()
    {
        while (APEvents.ReceivedItems.TryDequeue(out Item item))
        {
            if (item.Index < Data.ItemIndex)
            {
                return;
            }
            else if (item.Index == Data.ItemIndex)
            {
                GameLog.LogDebug($"Skipped all processed items up to index {item.Index}");
                return;
            }

            Items.HandleReceivedItem(item);

            Data.ItemIndex = item.Index;
        }
    }
}
