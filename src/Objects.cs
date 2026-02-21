using System;
using XRL.World;

namespace XRL.World.Parts
{
    [Serializable]
    public class APTaken : IPart
    {
        public string APLocation = "";

        public override bool WantEvent(int ID, int cascade)
        {
            return ID == TakenEvent.ID;
        }

        public override bool HandleEvent(TakenEvent E)
        {
            if (E.Actor.IsPlayer() && APGame.Instance.IsLocation(this.APLocation) && !APGame.Instance.LocationChecked(this.APLocation))
            {
                APGame.Instance.CheckLocation(this.APLocation);
            }

            return base.HandleEvent(E);
        }
    }

    public class APWaydroid : IPart {
        public override bool WantEvent(int ID, int cascade)
        {
            return ID == GetInventoryActionsEvent.ID;
        }

        public override bool HandleEvent(GetInventoryActionsEvent E)
        {
            var EventData = E;
            if (E.Actor.IsPlayer() && !APGame.Instance.HasReceivedItem("Waydroid Repair Kit"))
            {
                EventData.Actions.Remove("Repair");
            }

            return base.HandleEvent(EventData);
        }
    }
}
