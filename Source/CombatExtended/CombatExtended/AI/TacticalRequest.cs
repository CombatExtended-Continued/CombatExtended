using Verse;

namespace CombatExtended.AI
{
    public struct TacicalRequest : IExposable
    {
        public LocalTargetInfo target;

        public TacticalRequestType requestType;

        private int _createdAt;

        public TacicalRequest(Pawn pawn, TacticalRequestType requestType)
        {
            this.target = new LocalTargetInfo(pawn);
            this.requestType = requestType;
            this._createdAt = GenTicks.TicksGame;
        }

        public TacicalRequest(LocalTargetInfo target, TacticalRequestType requestType)
        {
            this.target = target;
            this.requestType = requestType;
            this._createdAt = GenTicks.TicksGame;
        }

        public bool Recent(int ticks)
        {
            return GenTicks.TicksGame - _createdAt <= ticks && target.IsValid;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref target, "target");
            Scribe_Values.Look(ref requestType, "TacticalRequestType");
            Scribe_Values.Look(ref _createdAt, "createdAt", -1);
        }
    }
}
