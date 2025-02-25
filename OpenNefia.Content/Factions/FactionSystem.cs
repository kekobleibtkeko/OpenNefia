﻿using OpenNefia.Analyzers;  
using OpenNefia.Content.Parties;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;

namespace OpenNefia.Content.Factions
{
    public interface IFactionSystem : IEntitySystem
    {
        Relation GetRelationTowards(EntityUid us, EntityUid them);
    }

    public class FactionSystem : EntitySystem, IFactionSystem
    {
        [Dependency] private readonly IPartySystem _partySystem = default!;
        [Dependency] private readonly IGameSessionManager _gameSession = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<FactionComponent, CalculateRelationEventArgs>(HandleCalculateRelation, nameof(HandleCalculateRelation));
        }

        /// <inheritdoc/>
        public Relation GetRelationTowards(EntityUid us, EntityUid them)
        {
            var ev = new CalculateRelationEventArgs(them);
            RaiseLocalEvent(us, ref ev);
            return ev.Relation;
        }

        public Relation CompareRelations(Relation ourRelation, Relation theirRelation)
        {
            // Allies like each other...
            if (ourRelation == Relation.Ally && theirRelation == Relation.Ally)
                return Relation.Ally;

            // ...and so do enemies.
            if (ourRelation == Relation.Enemy && theirRelation == Relation.Enemy)
                return Relation.Ally;

            // Non-enemies dislike enemies...
            if (ourRelation >= Relation.Hate)
            {
                if (theirRelation <= Relation.Enemy)
                {
                    return Relation.Enemy;
                }
            }
            // ...and vice-versa.
            else
            {
                if (theirRelation >= Relation.Hate)
                {
                    return Relation.Enemy;
                }
            }

            // Whichever relation is more hostile wins out.
            return (Relation)Math.Min((int)ourRelation, (int)theirRelation);
        }

        private void HandleCalculateRelation(EntityUid us, FactionComponent ourFaction, ref CalculateRelationEventArgs args)
        {
            args.Relation = CalculateRelationDefault(us, args.Target, ourFaction);
        }

        public Relation CalculateRelationDefault(EntityUid us, EntityUid them,
            FactionComponent? ourFaction = null,
            FactionComponent? theirFaction = null)
        {
            if (us == them)
            {
                // Love thyself.
                return Relation.Ally;
            }

            us = _partySystem.GetSupremeCommander(us)?.Owner ?? us;
            them = _partySystem.GetSupremeCommander(them)?.Owner ?? them;

            // If either entity lacks a faction component, then they should be treated as neutral.
            // This prevents the AI from targeting inanimate things like doors.
            if (!Resolve(us, ref ourFaction, logMissing: false) || !Resolve(them, ref theirFaction, logMissing: false))
            {
                return Relation.Neutral;
            }

            var ourRelation = GetBaseRelation(us, ourFaction);
            var theirRelation = GetBaseRelation(them, theirFaction);

            return CompareRelations(ourRelation, theirRelation);
        }

        private Relation GetBaseRelation(EntityUid entity, FactionComponent faction)
        {
            if (_gameSession.IsPlayer(entity))
            {
                return Relation.Ally;
            }
            else
            {
                return faction.RelationToPlayer;
            }
        }
    }

    [EventArgsUsage(EventArgsTargets.ByRef)]
    public struct CalculateRelationEventArgs
    {
        public EntityUid Target { get; }

        public Relation Relation { get; set; } = Relation.Neutral;

        public CalculateRelationEventArgs(EntityUid target)
        {
            Target = target;
        }
    }
}