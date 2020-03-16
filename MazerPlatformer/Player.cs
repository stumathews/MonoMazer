﻿using System;
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
        private readonly CommandManager _playerCommands = CommandManager.GetInstance();

        public Player(int x, int y, int w, int h, AnimationInfo animationInfo) : base(x, y, PlayerId, w, h, GameObjectType.Player) 
            => AnimationInfo = animationInfo;

        public override void Initialize()
        {
            AddComponent(ComponentType.Health, 100); // start off with 100 health
            AddComponent(ComponentType.Points, 0); // start off with 0 points
            // Get notified when I collide with another object (collision handled in base class)
            OnCollision += HandleCollision;
            
            // Can some of this go into a factory/builder?
            InitializeCharacter();
        }


        // I can update myself
        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            base.Update(gameTime, gameWorld);
            _playerCommands.Update(gameTime);
            Animation.Update(gameTime, (int)GetCentre().X, (int)GetCentre().Y);
        }

        // I can draw myself
        public override void Draw(SpriteBatch spriteBatch)
        {
            Animation.Draw(spriteBatch);

            if (Diganostics.DrawPlayerRectangle)
                spriteBatch.DrawRectangle(rect: new Rectangle(x: X, y: Y, width: W, height: H), color: Color.Gray);

            DrawObjectDiagnostics(spriteBatch);
        }

        // I can handle my own collisions
        public void HandleCollision(GameObject thisObject, GameObject otherObject)
        {
            CanMove = false;

            // Artificially nudge the player out of the collision
            switch (LastCollisionDirection)
            {
                case CharacterDirection.Up:
                    Y += 1;
                    break;
                case CharacterDirection.Down:
                    Y -= 1;
                    break;
                case CharacterDirection.Left:
                    X += 1;
                    break;
                case CharacterDirection.Right:
                    X -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Change my health component to be affected by the hitpoints of the other object
            if (otherObject.Type == GameObjectType.Npc)
            {
                var npcTypeComponent = otherObject.FindComponentByType(ComponentType.NpcType);
                var npcType = (Npc.NpcTypes)npcTypeComponent.Value;

                if (npcType == Npc.NpcTypes.Enemy)
                {
                    var hitPoints = otherObject.FindComponentByType(ComponentType.HitPoints).Value;
                    var myHealth = FindComponentByType(ComponentType.Health).Value;
                    UpdateComponentByType(ComponentType.Health, (int) myHealth - (int) hitPoints);
                }

                if (npcType == Npc.NpcTypes.Pickup)
                {
                    var pickupPoints = (int) otherObject.FindComponentByType(ComponentType.Points).Value;
                    var myPoints = (int)FindComponentByType(ComponentType.Points).Value;
                    var levelPoints = myPoints + pickupPoints;
                    
                    UpdateComponentByType(ComponentType.Points, levelPoints);
                }
            }
        }
    }
}
