﻿using OpenNefia.Content.Rendering;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.Serialization.Manager.Attributes;

namespace OpenNefia.Content.Charas
{
    [Prototype("Elona.Portrait")]
    public class PortraitPrototype : IPrototype, IHspIds<int>, IAtlasRegionProvider
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        /// <inheritdoc/>
        [DataField]
        [NeverPushInheritance]
        public HspIds<int>? HspIds { get; }

        [DataField(required: true)]
        public TileSpecifier Image { get; } = null!;

        public IEnumerable<AtlasRegion> GetAtlasRegions()
        {
            yield return new(ContentAtlasNames.Portrait, $"{ID}:Default", Image);
        }
    }
}
