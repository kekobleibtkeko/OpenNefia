using OpenNefia.Core.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Quest
{
    public enum QuestStatusType
    {
        Unstarted,
        Started,
        Failed,
        Completed
    }

    [DataDefinition]
    public class QuestStatus
    {
        [DataField]
        public QuestStatusType Status { get; set; }
        [DataField]
        public List<string> CompletedSteps { get; } = new();
    }
}
