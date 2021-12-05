//-----------------------------------------------------------------------

// <copyright file="MovingState.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using LanguageExt;
using Microsoft.Xna.Framework;
using static MazerPlatformer.Statics;
using static MazerPlatformer.GameWorldStatics;
namespace MazerPlatformer
{

    public class MovingState : NpcState
    {
        MovingStateBlackboardController _blackBoardControl;
        MovingStateBlackBoard _knowledge;

        public MovingState(string name, Npc npc) : base(name, npc) 
        {
            _knowledge = new MovingStateBlackBoard();
            _blackBoardControl = new MovingStateBlackboardController(_knowledge);
        }

        private readonly SimpleGameTimeTimer _spottedPlayerTimeout = new SimpleGameTimeTimer(5000);

        // relies on definition of external library
        public override void Enter(object owner)
        {
            base.Enter(owner);
            Npc.InfoText = "M";
            _spottedPlayerTimeout.Start();
        }

        // relies on definition of external library
        public override void Update(object owner, GameTime gameTime)
        {
            // Update Blackboard
            var updatePipeline =
                from baseupdate in Ensure(()=> base.Update(owner, gameTime))
                from timerUpdate in _spottedPlayerTimeout.Update(gameTime)
                from playerComponent in Npc.FindComponentByType(Component.ComponentType.Player).ToEither(NotFound.Create("player component not found"))
                from player in TryCastToT<Player>(playerComponent.Value)
                from component in Npc.FindComponentByType(Component.ComponentType.GameWorld).ToEither(NotFound.Create("gameworld compoennt not found"))
                from gameWorld in TryCastToT<GameWorld>(component.Value)
                from npcRoom in gameWorld.GetRoomIn(Npc).ToEither(NotFound.Create("Room unexpectedly not found"))
                from myRow in ToRoomRow(Npc, gameWorld.GetRoomHeight()).ToEither(NotFound.Create("Could not find room row for NPC"))
                from myCol in ToRoomColumn(Npc, gameWorld.GetRoomWidth()).ToEither(NotFound.Create("Could not find room col for NPC"))
                from playerRow in ToRoomRow(player, gameWorld.GetRoomHeight()).ToEither(NotFound.Create("Could not find room row for player"))
                from playerCol in ToRoomColumn(player, gameWorld.GetRoomWidth()).ToEither(NotFound.Create("Could not find room col for player"))
                from setNpcTextOk in SetNpcText(player, myRow, myCol, playerRow, playerCol)            
                from updatedBlackboard in _knowledge.Update(gameWorld, player, npcRoom, myRow, myCol, playerRow, playerCol, gameTime, Npc)
                from experts in _blackBoardControl.Update()
            select CheckForCollision(player, myRow, myCol, playerRow, playerCol, gameTime);

            updatePipeline.ThrowIfFailed();            
        }

        Either<IFailure, Unit> CheckForCollision(Player player, int myRow, int myCol, int playerRow, int playerCol, GameTime gameTime) => EnsuringBind(() =>
        {
            // Our experts have already determined if the the NPC is colliding with Player and if there is clear line of 
            var playerSeen = _knowledge.CollidingWithRoomAndPlayerSighted;

            // Change direction when the player is seen and there is line of sight between NPC and player
            WhenTrue(()=> playerSeen)
                .Map(seen => WhenTrue(()=> _knowledge.IsInSameColAsPlayer && playerSeen)
                                .Iter(yes => Npc.ChangeDirection(myRow < playerRow ? Character.CharacterDirection.Down : Character.CharacterDirection.Up)))
                .Map(unit => WhenTrue(()=> _knowledge.IsInSameRowAsPlayer && playerSeen)
                                .Iter(yes => Npc.ChangeDirection(myCol < playerCol ? Character.CharacterDirection.Right : Character.CharacterDirection.Left)));

            // Player not seen: continue moving in your current direction
            if (!playerSeen || !_spottedPlayerTimeout.IsTimedOut())
                return Npc.MoveInDirection(Npc.CurrentDirection, gameTime);

            player.Seen();

            _spottedPlayerTimeout.Reset();

            return Npc.MoveInDirection(Npc.CurrentDirection, gameTime);
        });

        private Either<IFailure, Unit> SetNpcText(Player player, int myRow, int myCol, int playerRow, int playerCol) => Ensure(()=>
        {
            // Diagnostics
            Npc.SubInfoText = $"R={myRow} C={myCol}";
            player.SubInfoText = $"R={playerRow}C={playerCol}";
        });
    }
}
