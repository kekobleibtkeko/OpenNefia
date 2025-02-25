﻿using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Serialization.Manager.Attributes;

namespace OpenNefia.Content.TurnOrder
{
    [RegisterComponent]
    public class MapTurnOrderComponent : Component
    {
        public override string Name => "MapTurnOrder";

        /// <summary>
        /// How much time it takes for an entity to take a turn in this map.
        /// </summary>
        [DataField]
        public int TurnCost { get; set; } = 10000;

        /// <summary>
        /// Whether this map has just been entered by the player.
        /// </summary>
        [DataField]
        public bool IsFirstTurn { get; set; }
    }
}