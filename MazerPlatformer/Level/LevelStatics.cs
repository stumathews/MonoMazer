//-----------------------------------------------------------------------

// <copyright file="LevelStatics.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using GameLibFramework.Animation;
using LanguageExt;
using Microsoft.Xna.Framework.Graphics;
using static MazerPlatformer.Component;
using static MazerPlatformer.Level;
using static MazerPlatformer.Statics;

namespace MazerPlatformer
{
    public static class LevelStatics
    {
        public static Option<string> CreateAssetFile(LevelDetails l) => string.IsNullOrEmpty(LevelDetailsMediator.GetPlayersSpriteFile(l))
                ? @"Sprites\dark_soldier-sword"
                : LevelDetailsMediator.GetPlayersSpriteFile(l);

        

        public static Option<AnimationInfo> CreatePlayerAnimation(string assetFile, Texture2D texture, LevelDetails l) => new AnimationInfo(
            texture: texture, assetFile,
            frameWidth: l?.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
            frameHeight: l?.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
            frameCount: l?.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount);

        public static Option<Player> CreatePlayer(Room player_room, AnimationInfo animation, LevelDetails level) => new Player(x: (int)player_room.GetCentre().X,
            y: (int)player_room.GetCentre().Y,
            width: level.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
            height: level.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
            animationInfo: animation);

        public static Option<Component> AddPlayerPointsComponent(Player p)
        {
            p.Components.Add(new Component(ComponentType.Points, 100));
            return GetPlayerPoints(p);
        }

        public static Option<Component> AddPlayerHealthComponent(Player p)
        {
            p.Components.Add(new Component(ComponentType.Health, 100));
            return GetPlayerHealth(p);
        }

        public static Either<IFailure, LevelNpcDetails> CreateLevelNpcDetails(Npc.NpcTypes type) => EnsureWithReturn(() =>
        {
            var details = new LevelNpcDetails
            {
                NpcType = type,
                Count = CharacterBuilder.DefaultNumPirates // template level file saves 5 of each type of npc
            };
            return details;
        });

        public static Either<IFailure, IEnumerable<Either<IFailure, LevelNpcDetails>>> AddCurrentNPCsToLevelFile(List<Npc> list, LevelDetails file)
        {
            return AddNpcsToLevelFile(list).AggregateFailures();

            IEnumerable<Either<IFailure, LevelNpcDetails>> AddNpcsToLevelFile(IEnumerable<Npc> characters)
            {
                var seenAssets = new System.Collections.Generic.HashSet<string>();

                foreach (var npcByAssetFile in NpcBehaviors.GetNPCsByAssetFile(characters))
                {
                    foreach (var npc in npcByAssetFile)
                    {
                        if (seenAssets.Contains(npcByAssetFile.Key)) break;

                        yield return
                            AddToLevelDetails(file, seenAssets, npcByAssetFile, npc);
                    }
                }
            }
        }

        private static Either<IFailure, LevelNpcDetails> AddToLevelDetails(LevelDetails file, System.Collections.Generic.HashSet<string> seenAssets, IGrouping<string, Npc> npcByAssetFile, Npc npc)
        {
            return from type in npc.GetNpcType().ToEither(NotFound.Create("Could not find Npc Type in NPC components"))
                   from details in CreateLevelNpcDetails(type)
                   from copy in CopyAnimationInfo(npc, details)
                   from add in AddNpcDetailsToLevelFile(file, details)
                   from added in AddToSeen(seenAssets, npcByAssetFile)
                   select details;
        }



        // Save Player info into level file
        public static Either<IFailure, Unit> SaveLevelFile(Player p, LevelDetails level, IFileSaver fileSaver, string levelFileName)
            => CopyAnimationInfo(p, level?.Player ?? new LevelPlayerDetails())
                 .Bind(unit => Ensure(() => fileSaver.SaveLevelFile(level, levelFileName)));

        public static Either<IFailure, Unit> CopyOrUpdateComponents(Character @from, LevelCharacterDetails to) => EnsuringBind(()
                => @from.Components.Map(fromComponent => fromComponent.Type == ComponentType.GameWorld || fromComponent.Type == ComponentType.Player
                ? ShortCircuitFailure.Create("Not needed").ToEitherFailure<Unit>()
                : to.Components.SingleOrFailure(x => x.Type == fromComponent.Type)
                               .BiBind(found => Nothingness(() => fromComponent.Value = found),
                                    notFound => Nothingness(() => to.Components.Add(fromComponent))))
                .AggregateUnitFailures()
                .IgnoreFailure());

        public static Either<IFailure, Unit> CopyAnimationInfo(Character @from, LevelCharacterDetails to) => EnsuringBind(() =>
        {
            to.SpriteHeight = @from.AnimationInfo.FrameHeight;
            to.SpriteWidth = @from.AnimationInfo.FrameWidth;
            to.SpriteFile = @from.AnimationInfo.AssetFile;
            to.SpriteFrameCount = @from.AnimationInfo.FrameCount;
            to.SpriteFrameTime = @from.AnimationInfo.FrameTime;
            to.MoveStep = 3;
            to.Components = to.Components ?? new List<Component>();
            return CopyOrUpdateComponents(@from, to);
        });

        public static Either<IFailure, Unit> AddNpcDetailsToLevelFile(LevelDetails levelDetails, LevelNpcDetails details) =>
                Ensure(() => { levelDetails.Npcs.Add(details); });

        public static Either<IFailure, bool> AddToSeen(System.Collections.Generic.HashSet<string> seenAssets, IGrouping<string, Npc> npcByAssetFile) => EnsuringBind<bool>(()
            => seenAssets.Add(npcByAssetFile.Key)
                           .FailIfFalse(InvalidDataFailure.Create($" {npcByAssetFile.Key} Could not added to seen assets")));


        // Make sure we actually have health or points for the player
        public static Option<Component> GetPlayerHealth(Player p) => p.FindComponentByType(ComponentType.Health);
        public static Option<Component> GetPlayerPoints(Player p) => p.FindComponentByType(ComponentType.Points);

        public static Option<Player> InitializePlayer(LevelDetails level, Player p)
        {
            level.Player.Components = level.Player.Components ?? new List<Component>();

            // Load any additional components from the level file
            level.Player.Components.Iter((comp) => p.AddComponent(comp.Type, comp.Value));
            return p;
        }

        public static Either<IFailure, Npc> AttachComponents(LevelNpcDetails levelNpc, Npc npc1)
        {
            levelNpc.Components.Iter((comp) => npc1.AddComponent(comp.Type, comp.Value));

            return npc1;
        }

        public static Either<IFailure, Unit> AddNpc(Npc npc, List<Npc> characters) => Ensure(action: ()
            => characters.Add(npc));

        public static Either<IFailure, List<Npc>> GenerateNPCsFromLevelFile(List<Npc> levelCharacters, LevelDetails file, CharacterBuilder npcBuilder, List<Room> rooms, Level level)
        {
            file.Npcs.Iter((levelNpc) =>
            {
                // Make as many NPCs as there are defined in the Level File
                Enumerable.Range(0, levelNpc.Count.Value).Iter((i) =>
                {
                    npcBuilder.CreateNpc(GetRandomRoom(rooms, level), levelNpc.SpriteFile,
                                        levelNpc.SpriteWidth ?? AnimationInfo.DefaultFrameWidth,
                                        levelNpc.SpriteHeight ?? AnimationInfo.DefaultFrameHeight,
                                        levelNpc.SpriteFrameCount ?? AnimationInfo.DefaultFrameCount,
                                        levelNpc.NpcType, levelNpc.MoveStep ?? Character.DefaultMoveStep)
                        .Bind((npc) => AttachComponents(levelNpc, npc))
                        .Bind((npc) => AddNpc(npc, levelCharacters));
                });
            });
            return levelCharacters;
        }

        public static Room GetRandomRoom(List<Room> rooms, Level level) => rooms[Level.RandomGenerator.Next(0, level.Rows * level.Cols)];

    }

    public static class LevelDetailsMediator
    {
        public static string GetPlayersSpriteFile(LevelDetails l)
        {
            return l?.Player?.SpriteFile;
        }
    }

    public static class NpcBehaviors
    {
        public static IEnumerable<IGrouping<string, Npc>> GetNPCsByAssetFile(IEnumerable<Npc> characters)
        {
            return characters.GroupBy(o => o.AnimationInfo.AssetFile);
        }
    }
}
