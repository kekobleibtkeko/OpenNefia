﻿using OpenNefia.Content.Logic;
using OpenNefia.Content.Prototypes;
using OpenNefia.Content.UI;
using OpenNefia.Core.Audio;
using OpenNefia.Core.Game;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Log;
using OpenNefia.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.Quest
{
    public enum QuestProgressResultType
    {
        Progressed,
        Started,
        NoChange,
        Completed,
    }

    public enum GetQuestNodeType
    {
        Failed,
        Succeeded,
        Branch,
        End
    }

    public interface IQuestSystem : IEntitySystem
    {
        GetQuestNodeType GetCurrentNode(PrototypeId<QuestPrototype> id, out IQuestNode? node);
        QuestProgressResultType ProgressQuest(PrototypeId<QuestPrototype> id, string? branch = null, bool broadcast = true);
        bool FailQuest(PrototypeId<QuestPrototype> id);
    }

    public sealed class QuestSystem : EntitySystem, IQuestSystem
    {
        [Dependency] private readonly IMessage _mes = default!;
        [Dependency] private readonly IPrototypeManager _protos = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override void Initialize()
        {
        }

        public GetQuestNodeType GetCurrentNode(PrototypeId<QuestPrototype> id, out IQuestNode? node)
        {
            node = null;
            var progress = _entMan.EnsureComponent<QuestProgressComponent>(GameSession.Player);

            if (!_protos.TryIndex(id, out var questProto))
            {
                Logger.WarningS("quest", $"Unable to get current node for {id}, unable to resolve prototype");
                return GetQuestNodeType.Failed;
            }

            if (!progress.QuestProgress.TryGetValue(id, out var status))
            {
                status = new QuestStatus();
                progress.QuestProgress[id] = status;
            }

            node = questProto.Node.GetNextOrCurrent(id, progress);
            return node.Type switch
            {
                QuestNodeType.Branch => GetQuestNodeType.Branch,
                QuestNodeType.End => GetQuestNodeType.End,
                _ => GetQuestNodeType.Succeeded,
            };
        }

        public QuestProgressResultType ProgressQuest(PrototypeId<QuestPrototype> id, string? branch = null, bool broadcast = true)
        {
            var progress = _entMan.EnsureComponent<QuestProgressComponent>(GameSession.Player);

            var nodeStatus = GetCurrentNode(id, out var current);
            if (current == null)
            {
                Logger.WarningS("quest", $"Node for quest {id} was null, unable to progress");
                return QuestProgressResultType.NoChange;
            }

            var status = progress.QuestProgress[id];
            if (status.Status == QuestStatusType.Unstarted)
            {
                status.Status = QuestStatusType.Started;
                if (broadcast)
                    BroadcastQuestChange();
                return QuestProgressResultType.Started;
            }

            switch (nodeStatus)
            {
                case GetQuestNodeType.Failed:
                    Logger.WarningS("quest", $"Unable to get current node for quest {id}");
                    return QuestProgressResultType.NoChange;
                case GetQuestNodeType.Branch:
                    if (string.IsNullOrEmpty(branch))
                    {
                        Logger.WarningS("quest", $"Current node has branches, but no branch was given to progress to for quest {id}");
                        return QuestProgressResultType.NoChange;
                    }
                    else
                    {
                        status.CompletedSteps.Add(branch);
                        if (broadcast)
                            BroadcastQuestChange();
                        return QuestProgressResultType.Progressed;
                    }
                default:
                    status.CompletedSteps.Add(current.ID);

                    var nextRes = GetCurrentNode(id, out var next);
                    if (nextRes != GetQuestNodeType.Failed && next!.Type == QuestNodeType.End)
                    {
                        status.CompletedSteps.Add(next.ID);
                        status.Status = QuestStatusType.Completed;
                        return QuestProgressResultType.Completed;
                    }
                    if (broadcast)
                        BroadcastQuestChange();
                    return QuestProgressResultType.Progressed;
            }
        }

        private void BroadcastQuestChange()
        {
            Sounds.Play(Protos.Sound.Write1);
            _mes.Newline();
            _mes.Display(Loc.GetString("Elona.Quest.JournalUpdated"), UiColors.MesYellow);
        }

        public bool FailQuest(PrototypeId<QuestPrototype> id)
        {
            var progress = _entMan.EnsureComponent<QuestProgressComponent>(GameSession.Player);
            if (!progress.QuestProgress.TryGetValue(id, out var status))
            {
                status = new QuestStatus
                {
                    Status = QuestStatusType.Failed
                };
                progress.QuestProgress[id] = status;
                return true;
            }

            if (status.Status == QuestStatusType.Completed || status.Status == QuestStatusType.Failed)
            {
                Logger.WarningS("quest", $"Attempted to fail a {status.Status} quest, aborting");
                return false;
            }

            status.Status = QuestStatusType.Failed;
            return true;
        }
    }
}
