using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Quest
{
    /// <summary>
    /// A component for tracking progress in quests.
    /// </summary>
    [RegisterComponent]
    public class QuestProgressComponent : Component
    {
        public override string Name => "QuestProgress";

        [DataField]
        public Dictionary<PrototypeId<QuestPrototype>, QuestStatus> QuestProgress { get; } = new();
    }
}
