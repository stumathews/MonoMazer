using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    public class GameWorld : PerFrame
    {
        private GraphicsDevice GraphicsDevice { get; }
        private SpriteBatch SpriteBatch { get; }
        private int Rows { get; }
        private int Cols { get; }
        public readonly List<GameObject> GameObjects = new List<GameObject>();
        public readonly Player Player;
        private readonly Random _random = new Random();

        

        public GameWorld(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, int rows, int cols)
        {
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
            Rows = rows;
            Cols = cols;

            var level = new Level(graphicsDevice, spriteBatch, removeRandomSides: true);
            
            var rooms = level.Make(Rows, Cols);

            var cellWidth = GraphicsDevice.Viewport.Width / cols;
            var cellHeight = GraphicsDevice.Viewport.Height / rows;

            var playerRoom = rooms[_random.Next(0, Rows * Cols)];

            var playerPositionWithinRoom = new Vector2(
                x: playerRoom.InitialPosition.X + (float)(0.5 * cellWidth), 
                y: playerRoom.InitialPosition.Y+ (float)(0.5 * cellHeight));

            Player = new Player(playerPositionWithinRoom, "Player");
            
            GameObjects.AddRange(rooms);
            GameObjects.Add(Player);
            
        }

        
        public override void Draw(SpriteBatch spriteBatch)
        {
            foreach (var gameObject in GameObjects)
            {
                gameObject.Draw(spriteBatch);
            }
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            foreach (var gameObject in GameObjects)
            {
                gameObject.Update(gameTime, gameWorld);
            }
        }
    }
}