//-----------------------------------------------------------------------

// <copyright file="IGameWorld.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using GameLib.EventDriven;
using LanguageExt;
using Microsoft.Xna.Framework;

namespace MazerPlatformer
{
    public interface IGameWorld
    {
        EventMediator EventMediator {get;set;}
        Either<IFailure, Unit> Draw(Option<InfrastructureMediator> infrastructure);
        int GetRoomHeight();
        Option<Room> GetRoomIn(GameObject gameObject);
        int GetRoomWidth();
        Either<IFailure, Unit> Initialize();
        Either<IFailure, bool> IsPathAccessibleBetween(GameObject obj1, GameObject obj2);
        Either<IFailure, Unit> LoadContent(int levelNumber, int? overridePlayerHealth = null, int? overridePlayerScore = null);
        Either<IFailure, Unit> MovePlayer(Character.CharacterDirection direction, GameTime dt);
        Either<IFailure, Unit> OnKeyUp(object sender, KeyboardEventArgs keyboardEventArgs);
        Either<IFailure, Unit> SaveLevel();
        Either<IFailure, Unit> SetPlayerStatistics(int health = 100, int points = 0);
        Either<IFailure, Unit> StartOrResumeLevelMusic();
        Either<IFailure, Unit> UnloadContent();
        Either<IFailure, Unit> Update(GameTime gameTime);
    }
}
