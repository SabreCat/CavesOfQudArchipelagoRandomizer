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
}
