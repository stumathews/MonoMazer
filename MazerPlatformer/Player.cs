using System;
using C3.XNA;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace MazerPlatformer
{
    /// <summary>
    /// Main playing character
    /// </summary>
    public class Player : Character
    {
        public const string PlayerId = "Player";

        private bool _ignoreCollisions;
        
        private readonly CommandManager _playerCommands = new CommandManager();

        public Player(int x, int y, int w, int h, AnimationInfo animationInfo) : base(x, y, PlayerId, w, h, GameObjectType.Player) 
            => AnimationInfo = animationInfo;

        //public override event StateChanged OnStateChanged = delegate { };
        //public override event DirectionChanged OnDirectionChanged = delegate { };
        //public override event CollisionDirectionChanged OnCollisionDirectionChanged = delegate { };

        
        public override void Initialize()
        {
            // Get notified when I collide with another object (collision handled in base class)
            OnCollision += Player_OnCollision;

            // Note the player movment commands are managed by the top level UI

            _playerCommands.AddKeyDownCommand(Keys.Space, (gt) => _ignoreCollisions = true);
            _playerCommands.AddKeyUpCommand(Keys.Space, (gt) => _ignoreCollisions = false);
            _playerCommands.OnKeyUp += (object sender, KeyboardEventArgs e) => SetState(CharacterStates.Idle);            

            
            CharacterMovingState = new CharacterMovingState(CharacterStates.Moving.ToString(), this);
            CollisionState = new CollisionState(CharacterStates.Colliding.ToString(), this);
            CharacterIdleState = new CharacterIdleState(CharacterStates.Idle.ToString(), this);

            InitializeCharacter();
        }


        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            _playerCommands.Update(gameTime);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }

     
        public override void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);

            if (Diganostics.DrawPlayerRectangle)
                spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: W, height: H), color: Color.Gray);

            DrawObjectDiganostics(spriteBatch);
        }

        
        /// <summary>
        /// I collided with something, set my current state to colliding
        /// </summary>
        /// <param name="object1"></param>
        /// <param name="object2"></param>
        private void Player_OnCollision(GameObject object1, GameObject object2)
        {
            if(!_ignoreCollisions)
                CurrentState = CharacterStates.Colliding;
        }
    }
}
