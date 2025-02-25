﻿using OpenNefia.Content.Logic;
using OpenNefia.Core.Audio;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Logic;
using OpenNefia.Content.Prototypes;
using OpenNefia.Content.Effects;
using OpenNefia.Core.IoC;
using OpenNefia.Content.DisplayName;
using OpenNefia.Core.Locale;
using OpenNefia.Content.EntityGen;
using OpenNefia.Content.Effects;

namespace OpenNefia.Content.GameObjects
{
    public class DrinkableSystem : EntitySystem
    {
        public const string VerbIDDrink = "Elona.Drink";

        [Dependency] private readonly IAudioManager _sounds = default!;
        [Dependency] private readonly IStackSystem _stackSystem = default!;
        [Dependency] private readonly IEntityGen _entityGen = default!;
        [Dependency] private readonly IMessagesManager _mes = default!;
        [Dependency] private readonly IDisplayNameSystem _displayNames = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DrinkableComponent, GetVerbsEventArgs>(HandleGetVerbs, nameof(HandleGetVerbs));
            SubscribeLocalEvent<ExecuteVerbEventArgs>(HandleExecuteVerb, nameof(HandleExecuteVerb));
            SubscribeLocalEvent<DrinkableComponent, DoDrinkEventArgs>(HandleDoDrink, nameof(HandleDoDrink));
            SubscribeLocalEvent<DrinkableComponent, ThrownEntityImpactedOtherEvent>(HandleImpactOther, nameof(HandleImpactOther));
            SubscribeLocalEvent<DrinkableComponent, ThrownEntityImpactedGroundEvent>(HandleImpactGround, nameof(HandleImpactGround));
            SubscribeLocalEvent<PotionPuddleComponent, EntitySteppedOnEvent>(HandlePotionPuddleSteppedOn, nameof(HandlePotionPuddleSteppedOn));
        }

        private void HandleGetVerbs(EntityUid potion, DrinkableComponent drinkableComp, GetVerbsEventArgs args)
        {
            args.Verbs.Add(new Verb(VerbIDDrink));
        }

        private void HandleExecuteVerb(ExecuteVerbEventArgs args)
        {
            if (args.Handled)
                return;

            switch (args.Verb.ID)
            {
                case VerbIDDrink:
                    Raise(args.Target, new DoDrinkEventArgs(args.Source), args);
                    break;
            }
        }

        private void HandleDoDrink(EntityUid target, DrinkableComponent drinkable, DoDrinkEventArgs args)
        {
            args.Handle(Drink(target, args.Drinker, drinkable));
        }

        private TurnResult Drink(EntityUid target, EntityUid drinker,
            DrinkableComponent? drinkable = null)
        {
            if (!Resolve(target, ref drinkable))
                return TurnResult.Failed;

            if (!EntityManager.TryGetComponent(drinker, out SpatialComponent sourceSpatial))
                return TurnResult.Failed;

            if (!_stackSystem.TrySplit(target, 1, out var split))
                return TurnResult.Failed;

            _mes.Display($"{_displayNames.GetDisplayName(drinker)} drinks {_displayNames.GetDisplayName(split)}.");

            _sounds.Play(Protos.Sound.Drink1, sourceSpatial.MapPosition);

            var result = drinkable.Effect?.Apply(drinker, sourceSpatial.MapPosition, drinker, drinkable.Args)
                ?? EffectResult.Succeeded;

            EntityManager.DeleteEntity(split);

            return result.ToTurnResult();
        }

        private void HandleImpactOther(EntityUid thrown, DrinkableComponent potionComp, ThrownEntityImpactedOtherEvent args)
        {
            _mes.Display(Loc.GetString("Elona.Throwable.Hits", ("entity", args.ImpactedWith)));
            _sounds.Play(Protos.Sound.Crush2, args.Coords);

            potionComp.Effect?.Apply(args.Thrower, args.Coords, args.ImpactedWith, potionComp.Args);

            EntityManager.DeleteEntity(thrown);
        }

        private void HandleImpactGround(EntityUid thrown, DrinkableComponent potionComp, ThrownEntityImpactedGroundEvent args)
        {
            _mes.Display($"{_displayNames.GetDisplayName(thrown)} shatters.");
            _sounds.Play(Protos.Sound.Crush2, args.Coords);

            var puddle = _entityGen.SpawnEntity(Protos.Mef.Potion, args.Coords);

            if (puddle == null)
                return;

            if (EntityManager.TryGetComponent(puddle.Value, out ChipComponent chipCompPuddle)
                && EntityManager.TryGetComponent(thrown, out ChipComponent chipCompPotion))
            {
                chipCompPuddle.Color = chipCompPotion.Color;
            }
            if (EntityManager.TryGetComponent(puddle.Value, out PotionPuddleComponent puddleComp))
            {
                puddleComp.Effect = potionComp.Effect;
                puddleComp.Args = potionComp.Args;
            }
        
            EntityManager.DeleteEntity(thrown);
        }

        private void HandlePotionPuddleSteppedOn(EntityUid source, PotionPuddleComponent potionComp, EntitySteppedOnEvent args)
        {
            _sounds.Play(Protos.Sound.Water, args.Coords);

            potionComp.Effect?.Apply(source, args.Coords, args.Stepper, potionComp.Args);

            EntityManager.DeleteEntity(source);
        }
    }

    public class DoDrinkEventArgs : TurnResultEntityEventArgs
    {
        public readonly EntityUid Drinker;

        public DoDrinkEventArgs(EntityUid drinker)
        {
            Drinker = drinker;
        }
    }
}
