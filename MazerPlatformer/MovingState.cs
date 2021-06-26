using LanguageExt;
using Microsoft.Xna.Framework;
using static MazerPlatformer.Statics;
using static MazerPlatformer.MovingStateStatics;

namespace MazerPlatformer
{
    public class MovingState : NpcState
    {
        public MovingState(string name, Npc npc) : base(name, npc) {}

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
            base.Update(owner, gameTime);
            _spottedPlayerTimeout.Update(gameTime);

            var updatePipeline =
                from playerComponent in Npc.FindComponentByType(Component.ComponentType.Player).ToEither(NotFound.Create("player component not found"))
                from player in TryCastToT<Player>(playerComponent.Value)
                from component in Npc.FindComponentByType(Component.ComponentType.GameWorld).ToEither(NotFound.Create("gameworld compoennt not found"))
                from gameWorld in TryCastToT<GameWorld>(component.Value)
                let npcRoom = gameWorld.GetRoomIn(Npc).ThrowIfNone(NotFound.Create("Room unexpectedly not found"))
                let myRow = gameWorld.ToRoomRow(Npc).ThrowIfNone(NotFound.Create("Could not find room row for NPC"))
                let myCol = gameWorld.ToRoomColumn(Npc).ThrowIfNone(NotFound.Create("Could not find room col for NPC"))
                let playerRow = gameWorld.ToRoomRow(player).ThrowIfNone(NotFound.Create("Could not find room row for player"))
                let playerCol = gameWorld.ToRoomColumn(player).ThrowIfNone(NotFound.Create("Could not find room col for player"))
            select CheckForCollision(gameWorld, player, npcRoom, myRow, myCol, playerRow, playerCol);

            updatePipeline.ThrowIfFailed();

            Either<IFailure, Unit> CheckForCollision(GameWorld gameWorld, Player player, Room npcRoom, int myRow, int myCol, int playerRow, int playerCol) => EnsuringBind(() =>
            {
                // Diagnostics
                Npc.SubInfoText = $"R={myRow} C={myCol}";
                player.SubInfoText = $"R={playerRow}C={playerCol}";

                var playerSeen = ChangeDirectionIfHitRoom(npcRoom, playerRow, myRow, playerCol, myCol, gameWorld, player);

                if (!playerSeen || !_spottedPlayerTimeout.IsTimedOut())
                    return Npc.MoveInDirection(Npc.CurrentDirection, gameTime);

                player.Seen();

                _spottedPlayerTimeout.Reset();

                return Npc.MoveInDirection(Npc.CurrentDirection, gameTime);
            });

            

            bool ChangeDirectionIfHitRoom(Room npcRoom, int playerRow, int myRow, int playerCol, int myCol, GameWorld gameWorld, Player player)
            {
                if (Npc.BoundingSphere.Intersects(npcRoom.BoundingSphere))
                {
                    var sameRow = playerRow == myRow;
                    var sameCol = playerCol == myCol;

                    // Reports if player was seen when changing direction
                    return ChangeDirection(sameCol, sameRow, gameWorld, player, Npc, myRow, playerRow, myCol, playerCol).ThrowIfFailed();
                }

                return false;
            }
        }
    }
}