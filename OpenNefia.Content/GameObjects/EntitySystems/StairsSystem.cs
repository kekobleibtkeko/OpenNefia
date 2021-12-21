﻿using OpenNefia.Core.Audio;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNefia.Content.Prototypes;

namespace OpenNefia.Content.GameObjects
{
    public class StairsSystem : EntitySystem
    {
        public const string VerbIDAscend = "Elona.Ascend";
        public const string VerbIDDescend = "Elona.Descend";
        public const string VerbIDActivate = "Elona.Activate";

        [Dependency] private readonly IAudioSystem _sounds = default!;
        [Dependency] private readonly MapEntranceSystem _mapEntrances = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<StairsComponent, GetVerbsEventArgs>(HandleGetVerbs, nameof(HandleGetVerbs));
            SubscribeLocalEvent<ExecuteVerbEventArgs>(HandleExecuteVerb, nameof(HandleExecuteVerb));
        }

        private void HandleGetVerbs(EntityUid uid, StairsComponent component, GetVerbsEventArgs args)
        {
            switch (component.Direction)
            {
                case StairsDirection.Up:
                    args.Verbs.Add(new Verb(VerbIDAscend));
                    break;
                case StairsDirection.Down:
                    args.Verbs.Add(new Verb(VerbIDDescend));
                    break;
            }

            args.Verbs.Add(new Verb(VerbIDActivate));
        }

        private void HandleExecuteVerb(ExecuteVerbEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.Verb.ID)
            {
                case VerbIDAscend:
                case VerbIDDescend:
                case VerbIDActivate:
                    args.Handle(UseStairs(args.Target, args.Source));
                    break;
            }
        }

        private TurnResult UseStairs(EntityUid entrance, EntityUid user,
            StairsComponent? stairs = null,
            MapEntranceComponent? mapEntrance = null)
        {
            if (!Resolve(entrance, ref stairs, ref mapEntrance))
                return TurnResult.Failed;

            _sounds.Play(Protos.Sound.Exitmap1);

            return _mapEntrances.UseMapEntrance(entrance, user, mapEntrance);
        }
    }
}
