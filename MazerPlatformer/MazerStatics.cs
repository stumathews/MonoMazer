using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameLibFramework.EventDriven;
using GameLibFramework.FSM;
using GeonBit.UI;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace MazerPlatformer
{
    public static class MazerStatics
    {
        public static Either<IFailure, Unit> UpdateUi(GameTime gameTime) => Statics.Ensure(() => UserInterface.Active.Update(gameTime));

        public static Either<IFailure, CommandManager> UpdateCommands(Either<IFailure, CommandManager> gameCommands, GameTime time)
        {
            return 
                from manager in gameCommands
                select UpdateCommandManager(manager, time);

            CommandManager UpdateCommandManager(CommandManager manager, GameTime theTime)
            {
                manager.Update(theTime);
                return manager;
            }
        }

        public static Either<IFailure, SpriteBatch> BeginSpriteBatch(SpriteBatch spriteBatch)
        {
            return from result in Statics.Ensure(() => spriteBatch.Begin())
                select spriteBatch;
        }

        public static Either<IFailure, Unit> PlayMenuMusic(Song song) => Statics.Ensure(()
            => MediaPlayer.Play(song));

        public static Either<IFailure, Unit> ClearGraphicsDevice(Mazer.GameStates currentGameState, GraphicsDevice graphicsDevice) =>
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

        public static Either<IFailure, Unit> UpdateStateMachine(GameTime time, FSM stateMachine) => Statics.Ensure(() => stateMachine.Update(time));

        public static Either<IFailure, Unit> DrawGameWorld(SpriteBatch spriteBatch, Either<IFailure, GameWorld> gameWorld) => Statics.EnsuringBind(() =>
            from world in gameWorld
            from draw in world.Draw(spriteBatch)
            select Statics.Nothing);

        public static Either<IFailure, Unit> DrawPlayerStatistics(SpriteBatch spriteBatch, SpriteFont font, GraphicsDevice graphicsDevice, int currentLevel, int playerHealth, int playerPoints) => Statics.Ensure(() =>
        {
            var leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10;
            spriteBatch.DrawString(font, $"Level: {currentLevel}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y), Color.White);
            spriteBatch.DrawString(font, $"Player Health: {playerHealth}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 30), Color.White);
            spriteBatch.DrawString(font, $"Player Points: {playerPoints}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 60), Color.White);
        });

        // See how we transform the input and return the output that was modified or in 
        public static Either<IFailure, SpriteBatch> EndSpriteBatch(SpriteBatch spriteBatch) =>
            from result in Statics.Ensure(spriteBatch.End)
            select spriteBatch;

        public static Either<IFailure, Unit> PrintGameStatistics(SpriteBatch spriteBatch, GameTime time, SpriteFont font, GraphicsDevice graphicsDevice, int numGameObjects, int numGameCollisionsEvents, int numCollisionsWithPlayerAndNpCs, Character.CharacterStates characterState, Character.CharacterDirection characterDirection, Character.CharacterDirection characterCollisionDirection, Mazer.GameStates currentGameState) => !IsPlayingGame(currentGameState) || !Diagnostics.ShowPlayerStats
            ? ShortCircuitFailure.Create("Not need to print game statistics").ToEitherFailure<Unit>()
            : Statics.Ensure(()=>
            {
                var leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10;
                spriteBatch.DrawString(font, $"Game Object Count: {numGameObjects}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 90), Color.White);
                spriteBatch.DrawString(font, $"Collision Events: {numGameCollisionsEvents}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 120), Color.White);
                spriteBatch.DrawString(font, $"NPC Collisions: {numCollisionsWithPlayerAndNpCs}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 150), Color.White);
                spriteBatch.DrawString(font, $"Frame rate(ms): {time.ElapsedGameTime.TotalMilliseconds}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 180), Color.White);
                spriteBatch.DrawString(font, $"Player State: {characterState}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 210), Color.White);
                spriteBatch.DrawString(font, $"Player Direction: {characterDirection}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 240), Color.White);
                spriteBatch.DrawString(font, $"Player Coll Direction: {characterCollisionDirection}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 270), Color.White);
            });

        public static Either<IFailure, Unit> ResetPlayerStatistics(Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups, Either<IFailure, GameWorld> theGameWorld) =>
            from playerHealth in (Either<IFailure, int>) setPlayerHealth()
            from playerPoints in (Either<IFailure, int>) setPlayerPoints()
            from playerPickups in (Either<IFailure, int>) setPLayerPickups()
            from gameWorld in theGameWorld
            from setResult in gameWorld.SetPlayerStatistics(playerHealth, playerPoints)
            select setResult;

        // Inform the game world that we're intending to reset the players state(vitals) 
        public static Either<IFailure, Unit> ResetPlayerStatistics(bool isFreshStart, Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups, Either<IFailure, GameWorld> theGameWorld)
            => !isFreshStart
                ? ShortCircuitFailure.Create("Not Fresh Start").ToEitherFailure<Unit>().IgnoreFailure()
                : ResetPlayerStatistics(setPlayerHealth, setPlayerPoints, setPLayerPickups, theGameWorld);

        public static Either<IFailure, Unit> HideMenu(Action setMenuPanelNotVisible) => Statics.Ensure(setMenuPanelNotVisible);

        public static Either<IFailure, Unit> IncrementCollisionStats(GameObject gameObject, Action increamentNumCollisionsWithPlayer, Action incrementGameCollisionEvents) => Statics.Ensure(() =>
        {
            if (gameObject.Type == GameObject.GameObjectType.Npc) 
                increamentNumCollisionsWithPlayer();

            incrementGameCollisionEvents();
        });

        public static Either<IFailure, Unit> SetPlayerDetails(Component.ComponentType type, int value, Action<int> setPlayerHealth, Action<int> setPlayerPoints)
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

        public static Either<IFailure, Unit> PauseGame(Func<Mazer.GameStates> setGamePausedFunc, Action showMenuFn) =>
            from currentGameState in (Either<IFailure, Mazer.GameStates>) setGamePausedFunc()
            from result in ShowMenu(showMenuFn)
            select result;

        public static Either<IFailure, Unit> StartOrContinueLevel(bool isFreshStart, Either<IFailure, GameWorld> theGameWorld, Action setMenuPanelNotVisibleFunction, Func<Mazer.GameStates> setGameToPlayingState, Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups) =>
            from state in (Either<IFailure, Mazer.GameStates>)(setGameToPlayingState())
            from hide in HideMenu(setMenuPanelNotVisibleFunction)
            from gameWorld in theGameWorld
            from startOrResumeLevelMusic in gameWorld.StartOrResumeLevelMusic()
            from reset in ResetPlayerStatistics(isFreshStart, 
                setPlayerHealth, 
                setPlayerPoints, 
                setPLayerPickups, theGameWorld)
            select Statics.Nothing;
    }
}
