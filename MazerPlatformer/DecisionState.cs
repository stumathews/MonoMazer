using Microsoft.Xna.Framework;

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

            // skip doing anything for a few secs
            if (IsWithin(100, gameTime).ThrowIfFailed())
                return;

           
            Npc.SwapDirection();
            

            // then move
            if (!Npc.IsColliding)
                Npc.NpcState = Npc.NpcStates.Moving;
        }
    }
}