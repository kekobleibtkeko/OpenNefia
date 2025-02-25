﻿using OpenNefia.Content.CharaInfo;
using OpenNefia.Content.Feats;
using OpenNefia.Content.Prototypes;
using OpenNefia.Content.UI;
using OpenNefia.Core.Audio;
using OpenNefia.Core.GameObjects;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Log;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Prototypes;
using OpenNefia.Core.UI;
using OpenNefia.Core.Utility;

namespace OpenNefia.Content.CharaMake
{
    [Localize("Elona.CharaMake.FeatSelect")]
    public class CharaMakeFeatWindowLayer : CharaMakeLayer
    {
        [Dependency] private readonly IFeatsSystem _feats = default!;

        public const string ResultName = "feats";

        private readonly Dictionary<PrototypeId<FeatPrototype>, FeatLevel> SelectedFeats = new();
        private int FeatCount;

        [Child] private FeatWindow FeatWindow;

        public CharaMakeFeatWindowLayer()
        {
            FeatWindow = new FeatWindow(new CharaMakeFeatWindowBehavior(this));
        }

        public override void Initialize(CharaMakeData args)
        {
            base.Initialize(args);
            Reset();
        }

        private void AddFeat(FeatWindow.FeatNameAndDesc.Feat feat)
        {
            FeatCount--;
            var protoId = feat.Prototype.GetStrongID();
            var level = SelectedFeats.GetValueOrInsert(protoId, () => new FeatLevel(0));
            level.Level.Base += 1;

            if (FeatCount <= 0)
            {
                Finish(new CharaMakeResult(new Dictionary<string, object>
                {
                    { ResultName, SelectedFeats }
                }));
            }
        }

        private void Reset()
        {
            SelectedFeats.Clear();
            FeatCount = 3;
            if (Data.TryGetCharaMakeResult(CharaMakeRaceSelectLayer.ResultName, out RacePrototype? race))
            {
                foreach (var feat in race.InitialFeats)
                    SelectedFeats[feat.Key] = new FeatLevel(feat.Value);
            }
            FeatWindow.RefreshData();
        }

        public override void OnQuery()
        {
            base.OnQuery();
            Sounds.Play(Protos.Sound.Feat);
        }

        public override void GetPreferredBounds(out UIBox2 bounds)
        {
            FeatWindow.GetPreferredSize(out var size);
            UiUtils.GetCenteredParams(size.X, size.Y, out bounds, yOffset: 10);
        }

        public override void GrabFocus()
        {
            base.GrabFocus();
            FeatWindow.GrabFocus();
        }

        public override void SetSize(float width, float height)
        {
            base.SetSize(width, height);
            FeatWindow.SetSize(Width, Height);
        }

        public override void SetPosition(float x, float y)
        {
            base.SetPosition(x, y);
            FeatWindow.SetPosition(X, Y);
        }

        public override void Draw()
        {
            base.Draw();
            FeatWindow.Draw();
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            FeatWindow.Update(dt);
        }

        public override void ApplyStep(EntityUid entity)
        {
            base.ApplyStep(entity);
            if (!Data.TryGetCharaMakeResult<Dictionary<PrototypeId<FeatPrototype>, FeatLevel>>(ResultName, out var feats))
                return;

            if (!EntityManager.TryGetComponent<FeatsComponent>(entity, out var featsComponent))
            {
                Logger.WarningS("charamake", "No FeatsComponent present on entity");
                return;
            }

            foreach (var (featId, level) in feats)
            {
                featsComponent.Feats[featId] = level;
            }
        }

        private class CharaMakeFeatWindowBehavior : IFeatWindowBehavior
        {
            private CharaMakeFeatWindowLayer Layer;

            public CharaMakeFeatWindowBehavior(CharaMakeFeatWindowLayer layer)
            {
                Layer = layer;
            }

            public int GetNumberOfFeatsAcquirable() => Layer.FeatCount;

            public IReadOnlyDictionary<PrototypeId<FeatPrototype>, FeatLevel> GetGainedFeats()
                => Layer.SelectedFeats;

            public void OnFeatSelected(FeatWindow.FeatNameAndDesc.Feat feat)
                => Layer.AddFeat(feat);
        }
    }
}

