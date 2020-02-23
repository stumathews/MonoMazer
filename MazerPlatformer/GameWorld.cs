using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MazerPlatformer
{
    /* Game world is contains the elements that can be updated/drawn each frame */
    public class GameWorld : PerFrame
    {
        private GraphicsDevice GraphicsDevice { get; }
        private int Rows { get; } // Rows Of rooms
        private int Cols { get; } // Columns of rooms

        /* Game objects within the gameworld */
        private readonly Dictionary<string, GameObject> _gameObjects = new Dictionary<string, GameObject>(); // Quick lookup by Id

        /* Special player game object */
        public readonly Player Player;

        /* Used to remove walls randonly throughout level and place the player randomly in a room */
        private readonly Random _random = new Random();

        public GameWorld(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, int rows, int cols)
        {
            GraphicsDevice = graphicsDevice;

            Rows = rows;
            Cols = cols;

            var level = new Level(graphicsDevice, spriteBatch, removeRandomSides: Diganostics.RandomSides);
            
            var rooms = level.Make(Rows, Cols);

            var cellWidth = GraphicsDevice.Viewport.Width / cols;
            var cellHeight = GraphicsDevice.Viewport.Height / rows;

            var playerRoom = rooms[_random.Next(0, Rows * Cols)];

            var playerPositionWithinRoom = new Vector2(
                x: playerRoom.X + (float)(0.5 * cellWidth), 
                y: playerRoom.Y+ (float)(0.5 * cellHeight));

            Player = new Player(x: (int)playerPositionWithinRoom.X, y: (int)playerPositionWithinRoom.Y, w: 15, h: 15);

            foreach (var room in rooms)
            {
                _gameObjects.Add(room.Id, room);
            }
            _gameObjects.Add(Player.PlayerId, Player);
            
        }

        
        public override void Draw(SpriteBatch spriteBatch)
        {
            /* We ask each game object within the game world to draw itself */
            foreach (KeyValuePair<string, GameObject> pair in _gameObjects)
            {
                var gameObject = pair.Value; 
                gameObject.Draw(spriteBatch);
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