using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public class CollidingState : NpcState
    {
        public CollidingState(string name, Npc npc) : base(name, npc) { }

        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "C";
            Npc.SetAsIdle();

        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Npc.NpcState = Npc.NpcStates.Deciding;
        }
    }
}