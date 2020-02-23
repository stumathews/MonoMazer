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
        private readonly Dictionary<string, GameObject> _gameObjects = new Dictionary<string, GameObject>(); // Dict allows quick lookup by Id
        public readonly Player Player;
        private readonly Random _random = new Random();

        

        public GameWorld(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, int rows, int cols)
        {
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
            Rows = rows;
            Cols = cols;

            var level = new Level(graphicsDevice, spriteBatch, removeRandomSides: Diganostics.RandomSides);
            
            var rooms = level.Make(Rows, Cols);

            var cellWidth = GraphicsDevice.Viewport.Width / cols;
            var cellHeight = GraphicsDevice.Viewport.Height / rows;

            var playerRoom = rooms[_random.Next(0, Rows * Cols)];

            var playerPositionWithinRoom = new Vector2(
                x: playerRoom.InitialPosition.X + (float)(0.5 * cellWidth), 
                y: playerRoom.InitialPosition.Y+ (float)(0.5 * cellHeight));

            Player = new Player(playerPositionWithinRoom, Player.PlayerId, new Vector2((float)(0.5 * cellWidth), (float)(0.5 * cellHeight)));

            foreach (var room in rooms)
            {
                _gameObjects.Add(room.Id, room);
            }
            _gameObjects.Add(Player.PlayerId, Player);
            
        }

        
        public override void Draw(SpriteBatch spriteBatch)
        {
            foreach (var gameObject in _gameObjects)
            {
                
                var obj = gameObject.Value; 
                obj.Draw(spriteBatch);
                
            }
        }

        public override void Update(GameTime gameTime, GameWorld gameWorld)
        {
            foreach (var gameItem in _gameObjects)
            {
                var obj = gameItem.Value;
                obj.Update(gameTime, gameWorld);
            }
        }
    }
}