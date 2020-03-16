using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Animation;
using GameLibFramework.FSM;

namespace MazerPlatformer
{
    public class Npc : Character
    {
        public enum NpcTypes
        {
            Pickup,
            Enemy
        };

        private State _wonderingState;

        public Npc(int x, int y, string id, int w, int h, GameObjectType type, AnimationInfo animationInfo) : base(x, y, id, w, h, type) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            _wonderingState = new WonderingState("default", this);

            AddState(_wonderingState);
            base.Initialize();
            Animation.Idle = false;
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);
            DrawObjectDiagnostics(spriteBatch);
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }

        public override void CollisionOccuredWith(GameObject otherObject)
        {
            // Collided with another object
            NudgeOutOfCollision();

        }
    }

    public class WonderingState : State
    {
        public Npc Owner { get; }

        public WonderingState(string name, Npc owner) : base(name)
        {
            Owner = owner;
        }

        public override void Update(object owner, GameTime gameTime)
        {
            base.Update(owner, gameTime);
            Console.WriteLine("Currently in Wondering state");
            
            if (Owner.IsColliding)
            {
                Owner.CanMove = false;
                switch (Owner.CurrentDirection)
                {
                    case Character.CharacterDirection.Up:
                        Owner.MoveInDirection(Character.CharacterDirection.Down, gameTime);
                        break;
                    case Character.CharacterDirection.Down:
                        Owner.MoveInDirection(Character.CharacterDirection.Up, gameTime);
                        break;
                    case Character.CharacterDirection.Left:
                        Owner.MoveInDirection(Character.CharacterDirection.Right, gameTime);
                        break;
                    case Character.CharacterDirection.Right:
                        Owner.MoveInDirection(Character.CharacterDirection.Left, gameTime);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Owner.MoveInDirection(Owner.CurrentDirection, gameTime);

            
        }
    }
}
