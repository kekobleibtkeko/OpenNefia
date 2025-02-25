﻿using OpenNefia.Core.GameObjects;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.Serialization.Manager.Attributes;

namespace OpenNefia.Content.Charas
{
    [RegisterComponent]
    public class CharaComponent : Component
    {
        public override string Name => "Chara";

        [DataField]
        public string Alias { get; set; } = string.Empty;

        /// <summary>
        /// Race of this character.
        /// </summary>
        /// <remarks>
        /// NOTE: Do not set this manually, skills/stats/components won't be updated.
        /// </remarks>
        [DataField(required: true)]
        public PrototypeId<RacePrototype> Race { get; set; } = default!;

        /// <summary>
        /// Class of this character.
        /// </summary>
        /// <remarks>
        /// NOTE: Do not set this manually, skills/stats/components won't be updated.
        /// </remarks>
        [DataField(required: true)]
        public PrototypeId<ClassPrototype> Class { get; set; } = default!;

        [DataField]
        public Gender Gender { get; set; } = Gender.Unknown;

        [ComponentDependency]
        private MetaDataComponent? _metaData;

        private CharaLivenessState _liveness;
        public CharaLivenessState Liveness
        {
            get => _liveness;
            set
            {
                _liveness = value;
                _metaData!.Liveness = GetGeneralLivenessState(value);
            }
        }

        private static EntityGameLiveness GetGeneralLivenessState(CharaLivenessState charaLiveness)
        {
            switch (charaLiveness)
            {
                case CharaLivenessState.Alive:
                    return EntityGameLiveness.Alive;
                case CharaLivenessState.PetDead:
                case CharaLivenessState.PetWait:
                case CharaLivenessState.VillagerDead:
                    return EntityGameLiveness.Hidden;
                case CharaLivenessState.Dead:
                default:
                    return EntityGameLiveness.DeadAndBuried;
            }
        }
    }

    public enum CharaLivenessState
    {
        Alive,
        PetDead,
        PetWait,
        VillagerDead,
        Dead,
    }

    public enum Gender : int
    {
        Male = 0,
        Female = 1,
        Unknown = 2
    }
}