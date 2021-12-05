//-----------------------------------------------------------------------

// <copyright file="MazerStatics.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

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


        

        public static Either<IFailure, Unit> PlayMenuMusic(IMusicPlayer player, Song song) => Statics.Ensure(()
            => player.Play(song));

        

        public static bool IsPlayingGame(Mazer.GameStates currentGameState) 
            => currentGameState == Mazer.GameStates.Playing;

        public static bool IsStateEntered(State.StateChangeReason reason) 
            => reason == State.StateChangeReason.Enter;

       

        public static Either<IFailure, Unit> DrawPlayerStatistics(Option<InfrastructureMediator> infrastruture, IGameSpriteFont font, IGameGraphicsDevice graphicsDevice, int currentLevel, int playerHealth, int playerPoints) => Statics.EnsuringBind(() =>
        {
            return from infra in infrastruture.ToEither()
            let leftSidePosition = graphicsDevice.Viewport.TitleSafeArea.X + 10
            from level in infra.DrawString(font, $"Level: {currentLevel}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y), Color.White)
            from health in infra.DrawString(font, $"Player Health: {playerHealth}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 30), Color.White)
            from points in infra.DrawString(font, $"Player Points: {playerPoints}", new Vector2(leftSidePosition, graphicsDevice.Viewport.TitleSafeArea.Y + 60), Color.White)
            select Statics.Success;
        });

        // See how we transform the input and return the output that was modified or in 
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
            Statics.WhenTrue(()=>gameObject.Type == GameObject.GameObjectType.Npc)
            .Iter((success)=>increamentNumCollisionsWithPlayer());

            incrementGameCollisionEvents();
        });

        public static Either<IFailure, Unit> SetPlayerDetails(Component.ComponentType type, int value, Func<int, int> setPlayerHealth, Func<int, int> setPlayerPoints)
            => Statics.Switcher(Statics.Cases()
                                        .AddCase(Statics.when(type == Component.ComponentType.Health, then: () => setPlayerHealth(value)))
                                        .AddCase(Statics.when(type == Component.ComponentType.Points, then: () => setPlayerPoints(value))), ShortCircuitFailure.Create("Unknown Type"));

        public static Either<IFailure, Unit> StartOrContinueLevel(bool isFreshStart, Either<IFailure, IGameWorld> theGameWorld, Action setMenuPanelNotVisibleFunction, Func<Mazer.GameStates> setGameToPlayingState, Func<int> setPlayerHealth, Func<int> setPlayerPoints, Func<int> setPLayerPickups) 
            => Statics.Ensure(()=>setGameToPlayingState())
                        .Bind(unit => HideMenu(setMenuPanelNotVisibleFunction))
                        .Bind(unit => theGameWorld)
                        .Bind(gameWorld => gameWorld.StartOrResumeLevelMusic())
                        .Bind(unit => ResetPlayerStatistics(isFreshStart, setPlayerHealth, setPlayerPoints, setPLayerPickups, theGameWorld));

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
            Statics.Ensure(()=> setPlayerDied(false))
            .Bind(unit => theGameWorld)
            .Bind(gameWorld => gameWorld.UnloadContent().Map(unit => gameWorld))
            .Bind(gameWorld => gameWorld.LoadContent(level, overridePlayerHealth, overridePlayerScore).Map(unit => gameWorld))
            .Bind(gameWorld => gameWorld.Initialize()) // We need to reinitialize things once we've reload content
            .Bind(unit => StartOrContinueLevel(isFreshStart, theGameWorld, setMenuPanelNotVisibleFunction, setGameToPlayingState,  setPlayerHealth, setPlayerPoints, setPLayerPickups));

        /// <summary>
        /// Loads game world content
        /// </summary>
        /// <param name="theGameWorld"></param>
        /// <param name="currentLevel"></param>
        /// <param name="playerHealth"></param>
        /// <param name="playerPoints"></param>
        /// <returns></returns>
        public static Either<IFailure, IGameWorld> LoadGameWorldContent(Either<IFailure, IGameWorld> theGameWorld, int currentLevel, int playerHealth = 100, int playerPoints = 0)
            => theGameWorld
            .Bind(world => world.LoadContent(levelNumber: currentLevel, playerHealth, playerPoints)
            .Map(unit => world));

        public static Either<IFailure, Unit> SetGameFont(Action setGameFont) 
            => Statics.Ensure(setGameFont);
        public static Either<IFailure, Unit> SetMenuMusic(Action setMenuMusic)
            => Statics.Ensure(setMenuMusic);

        /// <summary>
        /// Sets up the Infrastructure mediator
        /// </summary>
        /// <param name="im">InfrastructureMediator</param>
        /// <param name="ui">UiMediator</param>
        /// <returns></returns>
        public static InfrastructureMediator InitialiseInfrastructureMediator(InfrastructureMediator im, UiMediator ui)
        {
            im.Initialize(ui);
            return im;
        }

        public static UiMediator InitializeUI(UiMediator ui, Microsoft.Xna.Framework.Content.ContentManager content)
        {
            ui.InitializeUi(content);
            return ui;
        }

        public static Either<IFailure, Song> TryLoadAndSetMusic(InfrastructureMediator im)
        {
            return im.TryLoad<Song>("Music/bgm_menu")
                .Map(song => 
                {
                    im.SetMenuMusic(song);
                    return song;
                });
        }

    }
}
