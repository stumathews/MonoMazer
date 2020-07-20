using Microsoft.Xna.Framework;

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

            var x =
                from playerComponent in Npc.FindComponentByType(Component.ComponentType.Player)
                select Npc.FindComponentByType(Component.ComponentType.GameWorld)
                    .Map(component => (GameWorld) component.Value)
                    .Iter(gameWorld =>
                    {
                        var player = (Player) playerComponent.Value;
                        var npcRoom = gameWorld.GetRoomIn(Npc).ThrowIfNone(NotFound.Create("Room unexpectedly not found"));
                        var myRow = gameWorld.ToRoomRow(Npc).ThrowIfNone(NotFound.Create("Could not find room row for NPC"));
                        var myCol = gameWorld.ToRoomColumn(Npc).ThrowIfNone(NotFound.Create("Could not find room col for NPC"));
                        var playerRow = gameWorld.ToRoomRow(player).ThrowIfNone(NotFound.Create("Could not find room row for player"));
                        var playerCol = gameWorld.ToRoomColumn(player).ThrowIfNone(NotFound.Create("Could not find room col for player"));
                        var sameRow = playerRow == myRow;
                        var sameCol = playerCol == myCol;
                        Npc.SubInfoText = $"R={myRow} C={myCol}";
                        player.SubInfoText = $"R={playerRow}C={playerCol}";

                        var playerSeen = false;
                        if (Npc.BoundingSphere.Intersects(npcRoom.BoundingSphere))
                        {
                            Character.CharacterDirection newDir;
                            var changeDirection = !sameCol || !sameRow;
                            if (sameCol && gameWorld.IsPathAccessibleBetween(player, Npc).ThrowIfFailed())
                            {
                                newDir = myRow < playerRow
                                    ? Character.CharacterDirection.Down
                                    : Character.CharacterDirection.Up;
                                if (changeDirection)
                                    Npc.ChangeDirection(newDir);
                                playerSeen = true;

                            }
                            else if (sameRow && gameWorld.IsPathAccessibleBetween(player, Npc).ThrowIfFailed())
                            {
                                newDir = myCol < playerCol
                                    ? Character.CharacterDirection.Right
                                    : Character.CharacterDirection.Left;
                                if (changeDirection)
                                    Npc.ChangeDirection(newDir);
                                playerSeen = true;
                            }
                        }

                        if (playerSeen && _spottedPlayerTimeout.IsTimedOut())
                        {
                            player.Seen();
                            _spottedPlayerTimeout.Reset();
                        }

                        Npc.MoveInDirection(Npc.CurrentDirection, gameTime);
                    }).ToSuccess();
        }
    }
}