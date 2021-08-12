using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.Drawing;
using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using GeonBit.UI;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{

    public static class MazerStatics
    {
        public static Either<IFailure, Unit> UpdateUi(GameTime gameTime, IGameUserInterface userInterface) 
            => Statics.Ensure(() => userInterface.Update(gameTime));

        public static Either<IFailure, ICommandManager> UpdateCommands(Either<IFailure, ICommandManager> gameCommands, GameTime time)
        {
            return from manager in gameCommands
                    select UpdateCommandManager(manager, time);

            ICommandManager UpdateCommandManager(ICommandManager manager, GameTime theTime)
            {
                manager.Update(theTime);
                return manager;
            }
        }


        public static Either<IFailure, ISpriteBatcher> BeginSpriteBatch(ISpriteBatcher spriteBatcher) 
            => from result in Statics.Ensure(() => spriteBatcher.Begin())
               select spriteBatcher;

        public static Either<IFailure, Unit> PlayMenuMusic(IMusicPlayer player, Song song) => Statics.Ensure(()
            => player.Play(song));

        public static Either<IFailure, Unit> ClearGraphicsDevice(Mazer.GameStates currentGameState, IGameGraphicsDevice graphicsDevice) =>
            Statics.Ensure(() =>
            {
                graphicsDevice.Clear(IsPlayingGame(currentGameState)
                    ? Color.CornflowerBlue
                    : Color.Silver);
            });

        public static bool IsPlayingGame(Mazer.GameStates currentGameState) 
            => currentGameState == Mazer.GameStates.Playing;

        public static bool IsStateEntered(State.StateChangeReason reason) 
            => reason == State.StateChangeReason.Enter;

        public static Either<IFailure, Unit> UpdateStateMachine(GameTime time, IFSM stateMachine) => Statics.Ensure(() => stateMachine.Update(time));

        public static Either<IFailure, Unit> DrawGameWorld(ISpriteBatcher spriteBatch, Either<IFailure, IGameWorld> gameWorld) => Statics.EnsuringBind(() =>
            from world in gameWorld
            from draw in world.Draw(spriteBatch)
            select Statics.Nothing);

        public static Either<IFailure, Unit> DrawPlayerStatistics(ISpriteBatcher spriteBatcher, IGameSpriteFont font, IGameGraphicsDevice graphicsDevice, int currentLevel, int playerHealth, int playerPoints) => Statics.Ensure(() =>
        {
            var leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10;
            spriteBatcher.DrawString(font, $"Level: {currentLevel}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y), Color.White);
            spriteBatcher.DrawString(font, $"Player Health: {playerHealth}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 30), Color.White);
            spriteBatcher.DrawString(font, $"Player Points: {playerPoints}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 60), Color.White);
        });

        // See how we transform the input and return the output that was modified or in 
        public static Either<IFailure, ISpriteBatcher> EndSpriteBatch(ISpriteBatcher spriteBatcher) =>
            from result in Statics.Ensure(spriteBatcher.End)
            select spriteBatcher;

        public static Either<IFailure, Unit> PrintGameStatistics(ISpriteBatcher spriteBatcher, GameTime time, IGameSpriteFont font, IGameGraphicsDevice graphicsDevice, int numGameObjects, int numGameCollisionsEvents, int numCollisionsWithPlayerAndNpCs, Character.CharacterStates characterState, Character.CharacterDirection characterDirection, Character.CharacterDirection characterCollisionDirection, Mazer.GameStates currentGameState) 
            => !IsPlayingGame(currentGameState) || !Diagnostics.ShowPlayerStats
            ? ShortCircuitFailure.Create("Not need to print game statistics").ToEitherFailure<Unit>()
            : Statics.Ensure(()=>
            {
                var leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10;
                spriteBatcher.DrawString(font, $"Game Object Count: {numGameObjects}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 90), Color.White);
                spriteBatcher.DrawString(font, $"Collision Events: {numGameCollisionsEvents}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 120), Color.White);
                spriteBatcher.DrawString(font, $"NPC Collisions: {numCollisionsWithPlayerAndNpCs}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 150), Color.White);
                spriteBatcher.DrawString(font, $"Frame rate(ms): {time.ElapsedGameTime.TotalMilliseconds}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 180), Color.White);
                spriteBatcher.DrawString(font, $"Player State: {characterState}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 210), Color.White);
                spriteBatcher.DrawString(font, $"Player Direction: {characterDirection}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 240), Color.White);
                spriteBatcher.DrawString(font, $"Player Coll Direction: {characterCollisionDirection}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 270), Color.White);
            });

        public static Either<IFailure, Unit> ResetPlayerStatistics(Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups, Either<IFailure, IGameWorld> theGameWorld) =>
            from playerHealth in (Either<IFailure, int>) setPlayerHealth()
            from playerPoints in (Either<IFailure, int>) setPlayerPoints()
            from playerPickups in (Either<IFailure, int>) setPLayerPickups()
            from gameWorld in theGameWorld
            from setResult in gameWorld.SetPlayerStatistics(playerHealth, playerPoints)
            select setResult;

        // Inform the game world that we're intending to reset the players state(vitals) 
        public static Either<IFailure, Unit> ResetPlayerStatistics(bool isFreshStart, Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups, Either<IFailure, IGameWorld> theGameWorld)
            => !isFreshStart
                ? ShortCircuitFailure.Create("Not Fresh Start").ToEitherFailure<Unit>().IgnoreFailure()
                : ResetPlayerStatistics(setPlayerHealth, setPlayerPoints, setPLayerPickups, theGameWorld);

        
        public static Either<IFailure, Unit> HideMenu(Action setMenuPanelNotVisible) => Statics.Ensure(setMenuPanelNotVisible);


        public static Either<IFailure, Unit> IncrementCollisionStats(GameObject gameObject, Action increamentNumCollisionsWithPlayer, Action incrementGameCollisionEvents) => Statics.Ensure(() =>
        {
            Statics.MaybeTrue(()=>gameObject.Type == GameObject.GameObjectType.Npc)
            .Iter((success)=>increamentNumCollisionsWithPlayer());

            incrementGameCollisionEvents();
        });

        public static Either<IFailure, Unit> SetPlayerDetails(Component.ComponentType type, int value, Func<int, int> setPlayerHealth, Func<int, int> setPlayerPoints)
        {
            switch (type)
            {
                case Component.ComponentType.Health:
                    setPlayerHealth(value);
                    break;
                case Component.ComponentType.Points:
                    setPlayerPoints(value);
                    break;
            }

            return Statics.Nothing.ToEither();
        }

        public static Either<IFailure, Unit> ShowMenu(Action showMenuFn) => Statics.Ensure(showMenuFn);

        public static Either<IFailure, Unit> StartOrContinueLevel(bool isFreshStart, Either<IFailure, IGameWorld> theGameWorld, Action setMenuPanelNotVisibleFunction, Func<Mazer.GameStates> setGameToPlayingState, Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups) =>
            from state in (Either<IFailure, Mazer.GameStates>)(setGameToPlayingState())
            from hide in HideMenu(setMenuPanelNotVisibleFunction)
            from gameWorld in theGameWorld
            from startOrResumeLevelMusic in gameWorld.StartOrResumeLevelMusic()
            from reset in ResetPlayerStatistics(isFreshStart, 
                setPlayerHealth, 
                setPlayerPoints, 
                setPLayerPickups, theGameWorld)
            select Statics.Nothing;

        // Not pure - depends on static state and changes it
        public static Unit EnableAllDiagnostics()
        {
            Diagnostics.DrawMaxPoint = !Diagnostics.DrawMaxPoint;
            Diagnostics.DrawSquareSideBounds = !Diagnostics.DrawSquareSideBounds;
            Diagnostics.DrawSquareBounds = !Diagnostics.DrawSquareBounds;
            Diagnostics.DrawGameObjectBounds = !Diagnostics.DrawGameObjectBounds;
            Diagnostics.DrawObjectInfoText = !Diagnostics.DrawObjectInfoText;
            Diagnostics.ShowPlayerStats = !Diagnostics.ShowPlayerStats;
            return Statics.Nothing;
        }

        public static Either<IFailure, Unit> StartLevel(int level, Either<IFailure, IGameWorld> theGameWorld, Action setMenuPanelNotVisibleFunction, Func<Mazer.GameStates> setGameToPlayingState, Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups, Func<bool, bool> setPlayerDied, bool isFreshStart = true, int? overridePlayerHealth = null, int? overridePlayerScore = null) =>
            from setPlayerNotDead in (Either<IFailure, bool>)( setPlayerDied(false))
            from gameWorld in theGameWorld
            from unload in gameWorld.UnloadContent()
            from load in gameWorld.LoadContent(level, overridePlayerHealth, overridePlayerScore)
            from init in gameWorld.Initialize() // We need to reinitialize things once we've reload content
            from start in StartOrContinueLevel(isFreshStart, theGameWorld, setMenuPanelNotVisibleFunction, setGameToPlayingState,  setPlayerHealth, setPlayerPoints, setPLayerPickups)
            select Statics.Nothing;

        public static Either<IFailure, IGameWorld> LoadGameWorldContent(Either<IFailure, IGameWorld> theGameWworld, int currentLevel, int playerHealth = 100, int playerPoints = 0) =>
        from world in theGameWworld
        from load in world.LoadContent(levelNumber: currentLevel, playerHealth, playerPoints)
            select world;

        public static Either<IFailure, Unit> SetGameFont(Action setGameFont) => Statics.Ensure(setGameFont);
        public static Either<IFailure, Unit> SetMenuMusic(Action setMenuMusic) => Statics.Ensure(setMenuMusic);
    }
}
