//-----------------------------------------------------------------------

// <copyright file="Mazer.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using Microsoft.Xna.Framework;
using GeonBit.UI;
using GeonBit.UI.Entities;
using LanguageExt;
using static MazerPlatformer.MazerStatics;
using static MazerPlatformer.Statics;
using static MazerPlatformer.Mazer;
using GameLibFramework.Drawing;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MazerPlatformer
{
    public class UiMediator
    {
        private Panel _mainMenuPanel;
        private Panel _gameOverPanel;
        private Panel _controlsPanel;
        private GameMediator _gameMediator;
        private Either<IFailure, IGameUserInterface> _gameUserInterface = UninitializedFailure.Create<IGameUserInterface>(nameof(_gameUserInterface));

        public UiMediator(GameMediator gameMediator, SpriteBatch spriteBatch)
        {
            _gameMediator = gameMediator;
            _gameUserInterface = new GameUserInterface(spriteBatch);
        }

        public Either<IFailure, Unit> DrawUi() => _gameUserInterface.Bind( o => Ensure(()=>o.Draw()));

        public Either<IFailure, Unit> UpdateUi(GameTime gameTime) 
            => Statics.Ensure(() => _gameUserInterface.Iter( userInterface => userInterface.Update(gameTime)));

         public Either<IFailure, Unit> InitializeUi(Microsoft.Xna.Framework.Content.ContentManager content )
            => Ensure(() => UserInterface.Initialize(content, BuiltinThemes.editor))
                .Bind(unit => CreatePanels())
                .Bind(unit => SetupMainMenuPanel())
                .Bind(unit => SetupInstructionsPanel())
                .Bind(unit => SetupGameOverMenu(_gameOverPanel))
                .Bind(unit => AddPanelsToUi());

         public Either<IFailure, Unit> ShowGameOverScreen() =>
             SetupGameOverMenu(_gameOverPanel)
                .Bind(unit => MakeGameOverPanelVisible());

        public Either<IFailure, Unit> MakeGameOverPanelVisible() 
            => Ensure(()=> _gameOverPanel.Visible = true);

        public Either<IFailure, Unit> AddPanelsToUi() => Ensure(() =>
        {
            UserInterface.Active.AddEntity(_mainMenuPanel);
            UserInterface.Active.AddEntity(_controlsPanel);
            UserInterface.Active.AddEntity(_gameOverPanel);
        });

        public Either<IFailure, Unit> CreatePanels() 
            => Ensure(() => _mainMenuPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default))
                .EnsuringBind(panel => (Either<IFailure, Panel>)(_gameOverPanel = new Panel()))
                .EnsuringBind(panel => (Either<IFailure, Panel>)(_controlsPanel = new Panel(size: new Vector2(500, 500), skin: PanelSkin.Default)))
                .EnsuringMap(panel => Nothing);

        internal static Either<IFailure, UiMediator> Create(Option<GameMediator> gameMediator, Option<InfrastructureMediator> infrastructureMediator)
            =>  from theGameMediator in gameMediator.ToEither(InvalidDataFailure.Create("Invalid game mediator"))
                from theInfrastructoreMediator in infrastructureMediator.ToEither(InvalidDataFailure.Create("Invalid infrastructire mediator"))
                from theSpritebatch in theInfrastructoreMediator.GetSpriteBatch()
                select new UiMediator(theGameMediator, theSpritebatch);

        public Either<IFailure, Unit> SetupMainMenuPanel() => Ensure(() =>
        {
            var diagnostics = new Button("Diagnostics On/Off");
            var controlsButton = new Button("Controls", ButtonSkin.Fancy);
            var quitButton = new Button(text: "Quit Game", skin: ButtonSkin.Alternative);
            var startGameButton = new Button("New");
            var saveGameButton = new Button("Save");
            var loadGameButton = new Button("Load");

            HideMenu(SetMenuPanelNotVisibleFn);

            _mainMenuPanel.AdjustHeightAutomatically = true;
            _mainMenuPanel.AddChild(new Header("Main Menu"));
            _mainMenuPanel.AddChild(new HorizontalLine());
            _mainMenuPanel.AddChild(new Paragraph("Welcome to Mazer", Anchor.AutoCenter));
            _mainMenuPanel.AddChild(startGameButton);
            _mainMenuPanel.AddChild(saveGameButton);
            _mainMenuPanel.AddChild(loadGameButton);
            _mainMenuPanel.AddChild(controlsButton);
            _mainMenuPanel.AddChild(diagnostics);
            _mainMenuPanel.AddChild(quitButton);

            startGameButton.OnClick += (Entity entity) =>
            {
                _gameMediator.SetCurrentLevel(1);
                StartLevel(_gameMediator.GetCurrentLevel(), _gameMediator.GetGameWorld(), SetMenuPanelNotVisibleFn, _gameMediator.SetGameToPlayingState, _gameMediator.ResetPlayerHealth, _gameMediator.ResetPlayerPoints, _gameMediator.ResetPlayerPickups, _gameMediator.SetPlayerDied);
            };

            saveGameButton.OnClick += (e) =>
            {
                _gameMediator.GetGameWorld().Bind(world => world.SaveLevel());
                StartOrContinueLevel(isFreshStart: false, theGameWorld: _gameMediator.GetGameWorld(), SetMenuPanelNotVisibleFn, _gameMediator.SetGameToPlayingState, _gameMediator.ResetPlayerHealth, _gameMediator.ResetPlayerPoints, _gameMediator.ResetPlayerPickups);
            };

            loadGameButton.OnClick += (e) => StartLevel(_gameMediator.GetCurrentLevel(), _gameMediator.GetGameWorld(), SetMenuPanelNotVisibleFn, _gameMediator.SetGameToPlayingState, _gameMediator.ResetPlayerHealth, _gameMediator.ResetPlayerPoints, _gameMediator.ResetPlayerPickups, _gameMediator.SetPlayerDied, isFreshStart: false);
            diagnostics.OnClick += (Entity entity) => EnableAllDiagnostics();
            quitButton.OnClick += (Entity entity) => QuitGame().ThrowIfFailed();
            controlsButton.OnClick += (entity) => _controlsPanel.Visible = true;
        }, ExternalLibraryFailure.Create("Unable to setup main menu panel"));

        public void SetMenuPanelNotVisibleFn() => _mainMenuPanel.Visible = false;

        public Either<IFailure, Unit> SetupInstructionsPanel() => Ensure(() =>
        {
            var closeControlsPanelButton = new Button("Back");

            _controlsPanel.Visible = false;
            _controlsPanel.AdjustHeightAutomatically = true;
            _controlsPanel.AddChild(new Header("Mazer's Controls"));
            _controlsPanel.AddChild(new RichParagraph(
                "Hi welcome to {{BOLD}}Mazer{{DEFAULT}}, the goal of the game is to {{YELLOW}}collect{{DEFAULT}} all the balloons, while avoiding the enemies.\n\n" +
                "A level is cleared when all the ballons are collected.\n\n" +
                "You can move the player using the {{YELLOW}}arrows keys{{DEFAULT}}.\n\n" +
                "You have the ability to walk through walls but your enemies can't - however any walls you do remove will allow enemies to see and follow you!\n\n" +
                "{{BOLD}}Good Luck!"));
            _controlsPanel.AddChild(closeControlsPanelButton);
            closeControlsPanelButton.OnClick += (entity) => _controlsPanel.Visible = false;
        }, ExternalLibraryFailure.Create("Failed to setup instructions panel"));

        public Either<IFailure, Unit> SetupGameOverMenu(Panel gameOverPanel) => Ensure(() =>
        {
            var closeButton = new Button("Return to main menu");
            var restartLevel = new Button("Try again");
            var quit = new Button("Quit game");

            gameOverPanel.ClearChildren();
            gameOverPanel.AddChild(new Header("You died!"));
            gameOverPanel.AddChild(new RichParagraph("You had {{YELLOW}}" + _gameMediator.GetPlayerPoints() + "{{DEFAULT}} points.{{DEFAULT}}"));
            gameOverPanel.AddChild(new RichParagraph("You picked up {{YELLOW}}" + _gameMediator.GetPlayerPickups() +
                                                      "{{DEFAULT}} pick-ups.{{DEFAULT}}"));
            gameOverPanel.AddChild(new RichParagraph("You reach level {{YELLOW}}" + _gameMediator.GetCurrentLevel() + "{{DEFAULT}}.{{DEFAULT}}\n"));
            gameOverPanel.AddChild(new RichParagraph("Try again to {{BOLD}}improve!\n"));
            gameOverPanel.AddChild(restartLevel);
            gameOverPanel.AddChild(closeButton);
            gameOverPanel.Visible = false;

            closeButton.OnClick += (button) =>
            {
                _gameMediator.SetPlayerDied(false);
                gameOverPanel.Visible = false;
                
                _gameMediator.SetCurentGameState(GameStates.Paused);
            };

            restartLevel.OnClick += (button) =>
            {
                _gameMediator.SetPlayerDied(false);
                gameOverPanel.Visible = false;
                StartLevel(_gameMediator.GetCurrentLevel(), _gameMediator.GetGameWorld(), SetMenuPanelNotVisibleFn, _gameMediator.SetGameToPlayingState, _gameMediator.ResetPlayerHealth, _gameMediator.ResetPlayerPoints, _gameMediator.ResetPlayerPickups, _gameMediator.SetPlayerDied);
            };

            quit.OnClick += (b) => QuitGame().ThrowIfFailed();
        });

        private Either<IFailure, Unit> QuitGame() => Ensure(_gameMediator.Exit);

        public Either<IFailure, Unit> ShowMenu() => Statics.Ensure(() => _mainMenuPanel.Visible = true);
    }
}
