using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;
using LanguageExt;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public class Npc : Character
    {
        public enum NpcTypes
        {
            Unknown,
            Pickup,
            Enemy
        };

        public enum NpcStates
        {
            Moving,
            Deciding,
            Colliding
        };

        // By default the NPC start off  in the Deciding state
        public NpcStates NpcState { get; set; } = NpcStates.Deciding;
        
        public Npc(int x, int y, string id, int width, int height, GameObjectType type, AnimationInfo animationInfo, int moveStep = 3) : base(x, y, id, width, height, type, moveStep) 
            => AnimationInfo = animationInfo;

        public override Either<IFailure, Unit> Initialize() 
            => base.Initialize().Iter(unit =>
            {
                OnCollision += HandleCollision;
                Animation.Idle = false;

                // NPCs start off with random directions
                CurrentDirection = GetRandomEnumValue<CharacterDirection>();
            });

        private Either<IFailure, Unit> HandleCollision(GameObject thisObject, GameObject otherObject)
        {
            NpcState = NpcStates.Colliding;
            return otherObject.IsPlayer() ? Nothing : NudgeOutOfCollision();
        }
    }
}
