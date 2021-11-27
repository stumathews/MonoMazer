//-----------------------------------------------------------------------

// <copyright file="Mazer.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLibFramework.EventDriven;
using Microsoft.Xna.Framework.Graphics;
using LanguageExt;
using Microsoft.Xna.Framework.Media;
using static MazerPlatformer.MazerStatics;
using static MazerPlatformer.Statics;
using Microsoft.Xna.Framework;
using GameLibFramework.Drawing;

namespace MazerPlatformer
{
    public class InfrastructureMediator
    {
        private Either<IFailure, IMusicPlayer> musicPlayer = UninitializedFailure.Create<IMusicPlayer>(nameof(musicPlayer));
        private Either<IFailure, ISpriteBatcher> _spriteBatcher = UninitializedFailure.Create<ISpriteBatcher>(nameof(_spriteBatcher));
        
        private Option<SpriteBatch> _spriteBatch;
        
        private Song _menuMusic;
        private GameMediator _gameMediator;
        private GraphicsDevice _graphicsDevice;

        private Either<IFailure, GameContentManager> _gameContentManager = UninitializedFailure.Create<GameContentManager>(nameof(_gameContentManager));        
        private Either<IFailure, IGameGraphicsDevice> _gameGraphicsDevice = UninitializedFailure.Create<IGameGraphicsDevice>(nameof(_gameGraphicsDevice));

        public Either<IFailure, Unit> DrawString(IGameSpriteFont spriteFont, string text, Vector2 position, Color color) 
            => _spriteBatcher.Bind(sb => Ensure(() => sb.DrawString(spriteFont, text ?? string.Empty, position, color), $"Could not Draw string in {nameof(DrawString)}"));

        public Either<IFailure, Unit> DrawCircle(Vector2 center, float radius, int sides, Color color, float thickness)
        {
            return from sb in _spriteBatcher
            from result in Ensure(()=>sb.DrawCircle(center, radius, sides, color, thickness), $"Could not DrawCircle in in {nameof(InfrastructureMediator)}")
            select Nothing;
        }
        public Either<IFailure, Unit> DrawRectangle(Rectangle rect, Color color, float thickness)
        {
            return from sb in _spriteBatcher
                   from result in Ensure(()=>sb.DrawRectangle(rect, color, thickness))
                   select Nothing;
        }
        public Either<IFailure, Unit> DrawCircle(Vector2 center, float radius, int sides, Color color)
        {
            return from sb in _spriteBatcher
                   from result in Ensure(()=> sb.DrawCircle(center, radius, sides, color))
                   select Nothing;
        }
        public Either<IFailure, Unit> DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness)
        {
            return from sb in _spriteBatcher
                   from result in Ensure(()=> sb.DrawLine(x1, y1, x2, y2, color, thickness))
                   select Nothing;
        }
        public Either<IFailure, Unit> Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
        {
             return from sb in _spriteBatcher
                   from result in Ensure(()=> sb.Draw(texture, destinationRectangle, sourceRectangle, color))
                   select Nothing;
        }
        public Either<IFailure, Unit> DrawRectangle(Rectangle rect, Color color)
        {
            return from sb in _spriteBatcher
                   from result in Ensure(()=> sb.DrawRectangle(rect, color))
                   select Nothing;
        }

        public Either<IFailure, Unit> PrintGameStatistics(GameTime time, IGameSpriteFont font, int numGameObjects, int numGameCollisionsEvents, int numCollisionsWithPlayerAndNpCs, Character.CharacterStates characterState, Character.CharacterDirection characterDirection, Character.CharacterDirection characterCollisionDirection, Mazer.GameStates currentGameState) 
            => !IsPlayingGame(currentGameState) || !Diagnostics.ShowPlayerStats
            ? ShortCircuitFailure.Create("Not need to print game statistics").ToEitherFailure<Unit>()
            :  from graphicsDevice in _gameGraphicsDevice
                from spriteBatcher in _spriteBatcher
                let leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10
                from a in Ensure(()=> spriteBatcher.DrawString(font, $"Game Object Count: {numGameObjects}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 90), Color.White))
                from b in Ensure(()=> spriteBatcher.DrawString(font, $"Collision Events: {numGameCollisionsEvents}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 120), Color.White))
                from c in Ensure(()=> spriteBatcher.DrawString(font, $"NPC Collisions: {numCollisionsWithPlayerAndNpCs}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 150), Color.White))
                from d in Ensure(()=> spriteBatcher.DrawString(font, $"Frame rate(ms): {time.ElapsedGameTime.TotalMilliseconds}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 180), Color.White))
                from e in Ensure(()=> spriteBatcher.DrawString(font, $"Player State: {characterState}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 210), Color.White))
                from f in Ensure(()=> spriteBatcher.DrawString(font, $"Player Direction: {characterDirection}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 240), Color.White))
                from g in Ensure(()=> spriteBatcher.DrawString(font, $"Player Coll Direction: {characterCollisionDirection}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 270), Color.White))
                select Nothing;

       
        public Either<IFailure, Unit> DrawPlayerStatistics(IGameSpriteFont font) => Ensure(() =>
        {
            var t = from graphicsDevice in _gameGraphicsDevice
                    from spriteBatcher in _spriteBatcher
            let leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10
            from _ in Ensure(()=>spriteBatcher.DrawString(font, $"Level: {_gameMediator.GetCurrentLevel()}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y), Color.White))
            from __ in Ensure(()=>spriteBatcher.DrawString(font, $"Player Health: {_gameMediator.GetPlayerHealth()}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 30), Color.White))
            from ___ in Ensure(()=>spriteBatcher.DrawString(font, $"Player Points: {_gameMediator.GetPlayerPoints()}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 60), Color.White))
            select Nothing;
        });

         

        public Either<IFailure, Unit> ClearGraphicsDevice(Mazer.GameStates currentGameState) =>
            Statics.Ensure(() =>
            {
                _gameGraphicsDevice.Iter(device => device.Clear(IsPlayingGame(currentGameState)
                    ? Color.CornflowerBlue
                    : Color.Silver));
            });

        public Either<IFailure, T> TryLoad<T>(string asset) => _gameContentManager.Bind(o => o.TryLoad<T>(asset));

        public static Either<IFailure, InfrastructureMediator> Create()
        {
            return new InfrastructureMediator();
        }

        public Either<IFailure, SpriteBatch> GetSpriteBatch() => _spriteBatch.ToEither().MapLeft( o => UninitializedFailure.Create("No Sprite Batch"));

        public Either<IFailure, ISpriteBatcher> GetSpriteBatcher() => _spriteBatcher;

        public Either<IFailure, Unit> CreateInfrastructure(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content, Mazer game) => Ensure(() =>
        {

            _graphicsDevice = graphicsDevice;

            _gameGraphicsDevice = new GameGraphicsDevice(graphicsDevice);
            _gameMediator = new GameMediator(game);
            
            _spriteBatch = new SpriteBatch(_graphicsDevice);
            _spriteBatcher = from spriteBatch in _spriteBatch.ToEither() 
                             let spritebatcher = new SpriteBatcher(spriteBatch)
                             select (ISpriteBatcher) spritebatcher;
            
            _gameContentManager = CreateContentManager(content); 
            _gameMediator.SetCommandManager(new CommandManager());

            content.RootDirectory = "Content";
            
            
        }, ExternalLibraryFailure.Create("Failed to initialize Game infrastructure"));

        public Either<IFailure, IGameWorld> CreateGameWorld(int defaultNumRows, int defaultNumCols)
            => from gameContentManager in _gameContentManager
                         from spriteBatcher in _spriteBatcher
                         from gameWorld in GameWorld.Create(gameContentManager, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height, defaultNumRows, defaultNumCols)
                         select gameWorld;

        public Either<IFailure, Unit> Initialize(Option<UiMediator> uiMediator) => Ensure(() =>
        {
         musicPlayer = new MusicPlayer();
            
        });

        public Either<IFailure, Unit> DrawGameWorld() => EnsuringBind(() => from gameWorld in _gameMediator.GetGameWorld()
                                                                            from spriteBatcher in _spriteBatcher
                                                                            from drawResult in gameWorld.Draw(this)
                                                                            select drawResult);
        public Either<IFailure, ISpriteBatcher> BeginSpriteBatch() => from spriteBatcher in _spriteBatcher
                                                                      from result in Statics.Ensure(() => spriteBatcher.Begin())
                                                                      select spriteBatcher;

        public Either<IFailure, ISpriteBatcher> EndSpriteBatch() => from spriteBatcher in _spriteBatcher
                                                                    from result in Ensure(spriteBatcher.End).Map(unit => spriteBatcher)
                                                                    select result;

       

        public Either<IFailure, GameContentManager> CreateContentManager(Microsoft.Xna.Framework.Content.ContentManager content) => EnsureWithReturn(() => new GameContentManager(content));

        public Either<IFailure, Song> SetMenuMusic(Song song) 
        {
        _menuMusic = song;
        return _menuMusic;
        }

        public Either<IFailure, Unit> PlayPauseMusic() 
            => musicPlayer.Bind(player => PlayMenuMusic(player, _menuMusic));

    }
}
