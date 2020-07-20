using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class CollidingState : NpcState
    {
        public CollidingState(string name, Npc npc) : base(name, npc) { }

        // these are coming from the external library
        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "C";
            Npc.SetAsIdle();

        }

        // this is coming from the external library
        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Npc.NpcState = Npc.NpcStates.Deciding;
        }
    }
}