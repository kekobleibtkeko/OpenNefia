﻿using OpenNefia.Content.Equipment;
using OpenNefia.Core.Areas;
using OpenNefia.Core.Containers;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;

namespace OpenNefia.Content.Inventory
{
    /// <summary>
    /// Contains a character's items and slots for equipment.
    /// </summary>
    [RegisterComponent]
    public class InventoryComponent : Component, ISerializationHooks
    {
        public static readonly ContainerId ContainerIdInventory = new("Elona.Inventory");

        /// <inheritdoc />
        public override string Name => "Inventory";

        public Container Container { get; private set; } = default!;

        /// <summary>
        /// Maximum item weight this entity can hold. Null means "unlimited".
        /// </summary>
        [DataField]
        public int? MaxWeight { get; set; } = 0;

        protected override void Initialize()
        {
            base.Initialize();

            Container = ContainerHelpers.EnsureContainer<Container>(Owner, ContainerIdInventory);
        }

        bool ISerializationHooks.AfterCompare(object? other)
        {
            if (other is not InventoryComponent otherInv)
                return false;

            // Don't stack if either inventory is full.
            if (Container.ContainedEntities.Count >= 0 || otherInv.Container.ContainedEntities.Count > 0)
            {
                return false;
            }

            return true;
        }
    }
}
