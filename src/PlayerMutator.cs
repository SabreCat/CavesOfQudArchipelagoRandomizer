using System;
using XRL;
using XRL.World;

[PlayerMutator]
public class APPlayerMutator : IPlayerMutator
{
    public void mutate(GameObject player)
    {
        var apg = The.Game.RequireSystem<APGame>();

        if (!apg.Setup())
        {
            The.Game.Running = false;
            The.Game.DeathCategory = "exit";
            The.Game.DeathReason = "<nodeath>";
            // TODO how to reliably return to menu on abort? CUrrently this only works if something shows a popup during Setup()
            return;
        }

        player.RequirePart<PlayerStatsMod>();
        player.RequirePart<PlayerQuestMod>();
    }
}

[HasCallAfterGameLoaded]
public class APLoadGameHandler
{
    // TODO use game loaded event?
    [CallAfterGameLoaded]
    public static void OnGameLoaded()
    {
        var apg = The.Game.RequireSystem<APGame>();

        if (!apg.Setup())
        {
            The.Game.Running = false;
            The.Game.DeathCategory = "exit";
            The.Game.DeathReason = "<nodeath>";
            // TODO how to reliably return to menu on abort? CUrrently this only works if something shows a popup during Setup()
            return;
        }

        The.Player.RequirePart<PlayerStatsMod>();
        The.Player.RequirePart<PlayerQuestMod>();
    }
}
