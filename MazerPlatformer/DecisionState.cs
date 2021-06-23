using Microsoft.Xna.Framework;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class DecisionState : NpcState
    {
        public DecisionState(string name, Npc npc) : base(name, npc) {}

        // relies on external library
        public override void Enter(object owner)
        {
            
            Npc.InfoText = "D";
            Npc.CanMove = false;
            Npc.SetAsIdle();
        }

        // relies on external library
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);

           IsWithin(100, gameTime).ShortCirtcutOnTrue()
                        .Bind((boolean) => Npc.SwapDirection())
                        .IfRight((u) => Npc.NpcState = Npc.NpcStates.Moving);
        }
    }
}