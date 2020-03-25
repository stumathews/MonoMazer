using System;
using System.Collections.Generic;
using C3.XNA;
using GameLib.EventDriven;
using GameLibFramework.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static MazerPlatformer.Component;


namespace MazerPlatformer
{
    /// <summary>
    /// Main playing character
    /// </summary>
    public class Player : Character
    {
        public const string PlayerId = "Player";

        public delegate void DealthInfo(List<Component> playersComponents);
        
        public event DealthInfo OnDeath;
        public event EventHandler PlayerSpotted;

        public Player(int x, int y, int width, int height, AnimationInfo animationInfo) : base(x, y, PlayerId, width, height, GameObjectType.Player) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            base.Initialize();
            // Get notified when I collide with another object (collision handled in base class)
            OnCollision += HandleCollision;
        }

        // I can draw myself!
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (Diganostics.DrawPlayerRectangle)
                spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: Width, height: Height), color: Color.Gray);
        }

        // I can handle my own collisions
        public void HandleCollision(GameObject thisObject, GameObject otherObject)
        {
            NudgeOutOfCollision();

            // Change my health component to be affected by the hit points of the other object
            if (otherObject.Type != GameObjectType.Npc) return;

            var npcTypeComponent = otherObject.FindComponentByType(ComponentType.NpcType);
            var npcType = (Npc.NpcTypes)npcTypeComponent.Value;

            if (npcType == Npc.NpcTypes.Enemy)
            {
                var hitPoints = otherObject.FindComponentByType(ComponentType.HitPoints).Value;
                var myHealth = FindComponentByType(ComponentType.Health).Value;
                var newHealth = (int) myHealth - (int) hitPoints;
                UpdateComponentByType(ComponentType.Health, newHealth);

                if (newHealth < 0)
                    OnDeath?.Invoke(Components);
            }

            if (npcType == Npc.NpcTypes.Pickup)
            {
                var pickupPoints = (int) otherObject.FindComponentByType(ComponentType.Points).Value;
                var myPoints = (int)FindComponentByType(ComponentType.Points).Value;
                var levelPoints = myPoints + pickupPoints;
                UpdateComponentByType(ComponentType.Points, levelPoints);
            }
        }

        public void Seen() => PlayerSpotted?.Invoke(this, null);
    }
}
