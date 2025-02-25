﻿using OpenNefia.Content.GameObjects.Pickable;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Logic;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.UI.Element;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HspIdsInv = OpenNefia.Core.Prototypes.HspIds<OpenNefia.Content.Inventory.InvElonaId>;

namespace OpenNefia.Content.Inventory
{
    public class PickUpInventoryBehavior : BaseInventoryBehavior
    {
        [Dependency] private readonly IVerbSystem _verbSystem = default!;

        public override HspIdsInv HspIds { get; } = HspIdsInv.From122(new(id: 3));

        public override string WindowTitle => Loc.GetString("Elona.Inventory.Behavior.PickUp.WindowTitle");
        public override UiElement MakeIcon() => InventoryHelpers.MakeIcon(InventoryIcon.PickUp);

        public override IEnumerable<IInventorySource> GetSources(InventoryContext context)
        {
            yield return new GroundInvSource(context.User);
        }

        public override string GetQueryText(InventoryContext context)
        {
            return Loc.GetString("Elona.Inventory.Behavior.PickUp.QueryText");
        }

        public override bool IsAccepted(InventoryContext context, EntityUid item)
        {
            var verb = new Verb(PickableSystem.VerbIDPickUp);
            return _verbSystem.GetLocalVerbs(context.User, item).Contains(verb);
        }

        public override InventoryResult OnSelect(InventoryContext context, EntityUid item, int amount)
        {
            var verb = new Verb(PickableSystem.VerbIDPickUp);
            var result = _verbSystem.ExecuteVerb(context.User, item, verb);

            // TODO harvest quest

            return new InventoryResult.Continuing();
        }

        public override InventoryResult AfterFilter(InventoryContext context, IReadOnlyList<EntityUid> filteredItems)
        {
            if (filteredItems.Count == 0)
            {
                return new InventoryResult.Finished(TurnResult.Aborted);
            }

            return new InventoryResult.Continuing();
        }
    }
}
