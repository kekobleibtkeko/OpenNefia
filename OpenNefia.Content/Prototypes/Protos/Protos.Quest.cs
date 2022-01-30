using QuestPrototypeId = OpenNefia.Core.Prototypes.PrototypeId<OpenNefia.Content.Quest.QuestPrototype>;

namespace OpenNefia.Content.Prototypes
{
    public static partial class Protos
    {
        public static class Quest
        {
            #pragma warning disable format

            public static readonly QuestPrototypeId MainQuest   = new($"Elona.{nameof(MainQuest)}");

            #pragma warning restore format
        }
    }
}
