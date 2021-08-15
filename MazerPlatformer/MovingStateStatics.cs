using LanguageExt;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public static class MovingStateStatics
    {
        public static bool ShouldChangeDirection(bool eqCol, bool eqRow) => !eqCol || !eqRow;
        public static bool IsPlayerSeenInCol(bool eqCol, IGameWorld gw, Player p, Npc npc) => eqCol && gw.IsPathAccessibleBetween(p, npc).ThrowIfFailed();
        public static bool IsPlayerSeenInRow(bool eqRow, IGameWorld gw, Player p, Npc npc) => eqRow && gw.IsPathAccessibleBetween(p, npc).ThrowIfFailed();
        public static bool GetSeenInCol(bool eqCol, bool eqRow, IGameWorld gw, Player p, Npc npc, int myRow, int playerRow) => MaybeTrue(()=>IsPlayerSeenInCol(eqCol, gw, p, npc)).ToEither().Bind<bool>((success)=>
        {
            MaybeTrue(()=>ShouldChangeDirection(eqCol, eqRow))
                .Iter((ok)=> npc.ChangeDirection(myRow < playerRow ? Character.CharacterDirection.Down : Character.CharacterDirection.Up));
                        
            return true;
        }).Match(Right: (b)=>true, Left:(failure)=>false);

        public static bool GetSeenInRow(bool eqCol, bool eqRow, IGameWorld gw, Player p, Npc npc, int myCol, int playerCol) => MaybeTrue(()=>IsPlayerSeenInRow(eqRow, gw, p, npc)).ToEither().Bind<bool>((success)=>
        {
            MaybeTrue(()=>ShouldChangeDirection(eqCol, eqRow))
                .Iter((ok)=>npc.ChangeDirection(myCol < playerCol ? Character.CharacterDirection.Right : Character.CharacterDirection.Left));

            return true;
        }).Match(Right: (b)=>true, Left:(failure)=>false);

        public static Either<IFailure, bool> ChangeDirection(bool sameCol, bool sameRow, IGameWorld gameWorld, Player player, Npc npc, int myRow, int playerRow, int myCol, int playerCol) => EnsureWithReturn(() 
            => GetSeenInCol(sameCol, sameRow, gameWorld, player, npc, myRow, playerRow) || 
               GetSeenInRow(sameCol, sameRow, gameWorld, player, npc, myCol, playerCol));
        
    }
}