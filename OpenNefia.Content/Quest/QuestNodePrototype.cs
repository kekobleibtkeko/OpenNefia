using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Quest
{
    [Prototype("Elona.QuestNode")]
    public class QuestNodePrototype : IPrototype
    {
        [DataField("id")]
        public string ID { get; } = default!;

        [DataField]
        public IQuestNode Node { get; } = default!;
    }
}
