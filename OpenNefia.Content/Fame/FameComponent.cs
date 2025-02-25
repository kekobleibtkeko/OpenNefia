﻿using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Serialization.Manager.Attributes;
using OpenNefia.Core.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Fame
{
    [RegisterComponent]
    public sealed class FameComponent : Component
    {
        public override string Name => "Fame";

        [DataField]
        public Stat<int> Fame { get; set; } = new(0);
    }
}
