using Qud.API;
using UnityEngine;
using XRL;
using XRL.World;
using XRL.World.Parts.Skill;

[System.Serializable]
public class Item : IComposite
{
    public string Name;
    public long Id;
    public int Index;

    // Needed for deserialization
    private Item() { }

    public Item(string name, long id, int index)
    {
        Name = name;
        Id = id;
        Index = index;
    }

    public bool IsTrap()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Category == "trap";
    }

    public bool IsItem()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Type == "item";
    }

    public bool IsRandomItem()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Type == "randomItem";
    }

    public bool IsLiquid()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Type == "liquid";
    }

    public string Blueprint()
    {
        return APStaticData.Items[Name].Blueprint;
    }

    public string Type()
    {
        return APStaticData.Items[Name].Type;
    }

    public string Population()
    {
        return APStaticData.Items[Name].Population;
    }

    public int Amount()
    {
        return APStaticData.Items[Name].Amount;
    }
}

public static class Items
{
    public static void HandleReceivedItem(Item item)
    {
        if (item.IsTrap())
        {
            // Do not retrigger traps if they have been triggered already. Otherwise one could get a ton of traps at once
            // when reloading a checkpoint or starting a new chracter. This needs to work independent of the persistent game state,
            // thus this index is stored in the AP servers data storage.
            int seenIndex = APSession.GetDataStorageEntry<int?>("LastTrapSeenIndex") ?? 0;
            if (!APLocalOptions.RetriggerTraps && item.Index <= seenIndex)
            {
                GameLog.LogDebug(
                    $"Skipped received '{item.Name}' with index {item.Index} as it was already triggered (last seen index = {seenIndex})"
                );
                return;
            }

            if (The.Player.OnWorldMap())
            {
                APGame.Instance.Data.DelayedItems.Enqueue(item);
                GameLog.LogDebug(
                    $"Triggering of '{item.Name}' delayed until exiting the world map"
                );
                return;
            }
            else if (
                !APLocalOptions.AllowTrapsInSettlements && The.Player.CurrentZone.IsCheckpoint()
            )
            {
                APGame.Instance.Data.DelayedItems.Enqueue(item);
                GameLog.LogDebug(
                    $"Triggering of '{item.Name}' delayed until exiting the settlement"
                );
                return;
            }

            GameLog.LogGameplay(
                $"{{{{|&RReceived '{item.Name}'}}}}",
                APLocalOptions.PopupOnReceivedTrap
            );

            switch (item.Type())
            {
                case "grenades":
                    GrenadesTrap(item);
                    break;
                case "creatures":
                    CreaturesTrap(item);
                    break;
                default:
                    GameLog.LogError($"Unknown trap type '{item.Type()}' with name '{item.Name}'");
                    break;
            }

            // Make sure this trap doesn't trigger again
            APSession.SetDataStorageEntry("LastTrapSeenIndex", item.Index);
        }
        else if (item.IsItem())
        {
            GameLog.LogGameplay($"Received '{item.Name}'", APLocalOptions.PopupOnReceivedItem);

            for (int i = 0; i < item.Amount(); i++)
            {
                The.Player.Inventory.AddObject(item.Blueprint());
            }
        }
        else if (item.IsRandomItem())
        {
            // TODO mod chance
            var pop = APGame.Instance.GetRandomFromPopulation(item.Population());

            var bp = GameObjectFactory.Factory.GetBlueprint(pop.Blueprint);
            GameLog.LogGameplay(
                $"Received {pop.Number} '{bp.DisplayName()}'",
                APLocalOptions.PopupOnReceivedItem
            );

            for (int i = 0; i < pop.Number; i++)
            {
                The.Player.Inventory.AddObject(bp.Name);
            }
        }
        // else if (item.IsLiquid())
        // {
        //     GameLog.LogGameplay($"Received {item.Amount()} '{first.DisplayName}'", APLocalOptions.PopupOnReceivedItem);
        //     The.Player.GiveDrams(item.Amount(), item.Blueprint());
        // }
        // TODO
        else
        {
            switch (item.Name)
            {
                case "Ancient Knickknack":
                case "Prime Knickknack":
                case "Weirdwire Fusor":
                case "Barathrumite Token":
                case "Waydroid Repair Kit":
                case "Eschaton Transcoder":
                case "Baetyl Optronic Adapter":
                case "Consortium Token":
                case "Water Farmer Token":
                case "Hindren Token":
                case "Kyakukya Token":
                    GameLog.LogGameplay($"Received quest item '{item.Name}'", APLocalOptions.PopupOnReceivedItem);
                    return;
                case "Hit Points":
                    The.Player.GetPart<PlayerStatsMod>().AddHitPoints();
                    return;
                case "Attribute Point":
                    The.Player.GetPart<PlayerStatsMod>().AddAttributePoints();
                    return;
                case "Attribute Bonus":
                    The.Player.GetPart<PlayerStatsMod>().AddAttributeBonus();
                    return;
                case "Mutation Point":
                    The.Player.GetPart<PlayerStatsMod>().AddMutationPoints();
                    return;
                case "Skill Points":
                    The.Player.GetPart<PlayerStatsMod>().AddSkillPoints();
                    return;
                case "Rapid Mutation Advancement":
                    The.Player.GetPart<PlayerStatsMod>().RapidMutationAdvancement();
                    return;
                default:
                    GameLog.LogError($"Unknown item '{item.Name}' with id {item.Id}");
                    break;
            }
        }
    }

    private static void GrenadesTrap(Item item)
    {
        if (item.Blueprint() == "HandENuke")
        {
            int dist = Random.Range(10, 20);
            int countdown = Random.Range(10, 25);
            var obj = GameObjectFactory.create(item.Blueprint());
            var bomb = Tinkering_LayMine.CreateBomb(obj, The.Player, countdown);
            The.Player.GetCurrentCell().GetRandomLocalAdjacentCellAtRadius(dist).AddObject(bomb);
        }
        else
        {
            var amount = Random.Range(3, 10);
            for (var i = 0; i < amount; i++)
            {
                int dist = Random.Range(1, 10);
                int countdown = Random.Range(3, 7);
                var obj = GameObjectFactory.create(item.Blueprint());
                var bomb = Tinkering_LayMine.CreateBomb(obj, The.Player, countdown);
                The.Player.GetCurrentCell()
                    .GetRandomLocalAdjacentCellAtRadius(dist)
                    .AddObject(bomb);
            }
        }
    }

    private static void CreaturesTrap(Item item)
    {
        if (!PopulationManager.Populations.ContainsKey(item.Population()))
        {
            throw new System.Exception($"Unknown population {item.Population()}");
        }

        for (int a = 0; a < item.Amount(); a++)
        {
            // TODO reliable repeatable results
            var pop = PopulationManager.Populations[item.Population()].Generate();

            foreach (var it in pop)
            {

                for (int i = 0; i < it.Number; i++)
                {
                    int dist = Random.Range(2, 10);
                    var obj = GameObjectFactory.create(it.Blueprint);
                    The.Player.GetCurrentCell().GetRandomLocalAdjacentCellAtRadius(dist).AddObject(obj);
                }
            }
        }
    }
}
