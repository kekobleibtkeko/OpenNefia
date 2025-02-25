﻿using OpenNefia.Content.UI.Element;
using OpenNefia.Content.Prototypes;
using OpenNefia.Core.Graphics;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Rendering;
using OpenNefia.Core.UI;
using OpenNefia.Core.UI.Element;
using OpenNefia.Core.UI.Layer;
using OpenNefia.Core.UI.Element;
using OpenNefia.Content.UI.Element.Containers;
using OpenNefia.Core.Input;
using OpenNefia.Core.Maths;
using OpenNefia.Content.World;
using OpenNefia.Core.GameObjects;
using OpenNefia.Content.UI.Layer;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Game;
using OpenNefia.Content.Charas;
using OpenNefia.Content.Skills;
using OpenNefia.Content.Levels;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Prototypes;
using OpenNefia.Content.Equipment;
using OpenNefia.Core.Stats;
using OpenNefia.Content.DisplayName;
using OpenNefia.Content.Currency;
using OpenNefia.Content.Hud;
using static OpenNefia.Content.Hud.HudAttributeWidget;

namespace OpenNefia.Content.UI.Hud
{
    public class HudLayer : UiLayer, IHudLayer, IBacklog
    {
        [Flags]
        public enum WidgetDrawFlags
        {
            Never   = 0,
            Normal  = 1 << 0,
            Backlog = 1 << 1,
            Always  = Normal + Backlog,
        }

        public enum WidgetAnchor
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
        }

        private sealed class WidgetInstance
        {
            public BaseHudWidget Widget { get; set; }
            public Vector2 Position { get; set; }
            public Func<Vector2> SizeFunc { get; set; }
            public WidgetDrawFlags DrawFlags { get; set; }
            public WidgetAnchor Anchor { get; set; }

            public WidgetInstance(BaseHudWidget widget, WidgetAnchor anchor = default, Vector2 position = default, Func<Vector2> size = default!,
                WidgetDrawFlags flags = WidgetDrawFlags.Always)
            {
                Widget = widget;
                Widget.Initialize();
                Position = position;
                SizeFunc = size ?? (() => new());
                DrawFlags = flags;
                Anchor = anchor;
            }
        }

        [Dependency] private readonly IFieldLayer _field = default!;
        [Dependency] private readonly IGraphics _graphics = default!;

        private List<WidgetInstance> Widgets = new();

        public HudMessageBoxWidget HudMessageWindow { get; private set; } = default!;

        public UIBox2 GameBounds => new(0, 0, _graphics.WindowSize.X, _graphics.WindowSize.Y - HudMinimapWidget.MinimapHeight);
        public IBacklog Backlog => HudMessageWindow;
        public IHudMessageWindow MessageWindow => HudMessageWindow;
        public bool IsShowingBacklog => HudMessageWindow.IsShowingBacklog;

        [Child] private UiFpsCounter FpsCounter;
        [Child] private UiMessageWindowBacking MessageBoxBacking = default!;
        [Child] private UiMessageWindowBacking BacklogBacking = default!;
        [Child] private UiHudBar HudBar = default!;

        public const int HudZOrder = 20000000;

        public HudLayer()
        {
            IoCManager.InjectDependencies(this);

            FpsCounter = new UiFpsCounter();
            MessageBoxBacking = new UiMessageWindowBacking();
            BacklogBacking = new UiMessageWindowBacking(UiMessageWindowBacking.MessageBackingType.Expanded);
            HudBar = new UiHudBar();
        }

        public void Initialize()
        {
            CanKeyboardFocus = true;

            AddDefaultWidgets();
            
            // This is so the widgets will have the correct UI scaling.
            foreach (var widget in Widgets)
            {
                UiHelpers.AddChildrenRecursive(this, widget.Widget);
            }

            _field.OnScreenRefresh += OnScreenRefresh;
        }

        private void AddDefaultWidgets()
        {
            HudMessageWindow = new HudMessageBoxWidget();

            Widgets.Add(new(HudMessageWindow, WidgetAnchor.BottomLeft, 
                new(HudMinimapWidget.MinimapWidth + 25, -84), 
                () => new(Width - HudMinimapWidget.MinimapWidth - 75, 
                0)));

            Widgets.Add(new(new HudMinimapWidget(), WidgetAnchor.BottomLeft, new(0, -HudMinimapWidget.MinimapHeight)));
            Widgets.Add(new(new HudExpWidget(), WidgetAnchor.BottomLeft, new(5, -104)));
            Widgets.Add(new(new HudAreaNameWidget(), WidgetAnchor.BottomLeft, new(HudMinimapWidget.MinimapWidth + 18, -17)));
            Widgets.Add(new(new HudDateWidget(), WidgetAnchor.TopLeft, new(80, 8)));
            Widgets.Add(new(new HudClockWidget(), WidgetAnchor.TopLeft, new(0, 0)));

            var iconX = 285;
            foreach (HudSkillIconType type in Enum.GetValues<HudSkillIconType>())
            {
                if (type > HudSkillIconType.Cha)
                    iconX += 5;
                Widgets.Add(new(new HudAttributeWidget(type), WidgetAnchor.BottomLeft, new(iconX, -17)));
                iconX += 47;
            }

            Widgets.Add(new(new HudHPBarWidget(), WidgetAnchor.BottomLeft, new(260, -100), flags: WidgetDrawFlags.Normal));
            Widgets.Add(new(new HudMPBarWidget(), WidgetAnchor.BottomLeft, new(400, -100), flags: WidgetDrawFlags.Normal));

            Widgets.Add(new(new HudGoldWidget(), WidgetAnchor.BottomRight, new(-220, -104), flags: WidgetDrawFlags.Normal));
            Widgets.Add(new(new HudPlatinumWidget(), WidgetAnchor.BottomRight, new(-90, -104), flags: WidgetDrawFlags.Normal));
        }

        private void UpdateWidgets()
        {
            foreach (var widget in Widgets)
            {
                widget.Widget.UpdateWidget();
            }
        }

        private void OnScreenRefresh()
        {
            UpdateWidgets();
        }

        public override void SetSize(float width, float height)
        {
            // TODO remove
            LayerUIScale = _graphics.WindowScale;

            base.SetSize(width, height);

            FpsCounter.SetSize(400, 500);
            MessageBoxBacking.SetSize(width + 200, 72);
            BacklogBacking.SetSize(width + 200, 600);
            HudBar.SetSize(Width + 200, UiHudBar.HudBarHeight);

            foreach (var widget in Widgets)
            {
                var size = widget.SizeFunc();
                widget.Widget.SetSize(size.X, size.Y);
            }
        }

        public override void SetPosition(float x, float y)
        {
            base.SetPosition(x, y);

            foreach(var widget in Widgets)
            {
                Vector2 anchor = widget.Anchor switch
                {
                    WidgetAnchor.BottomLeft => new(0, Height),
                    WidgetAnchor.BottomRight => new(Width, Height),
                    WidgetAnchor.TopRight => new(Width, 0),
                    _ => new(0, 0),
                };
                widget.Widget.SetPosition(anchor.X + widget.Position.X, anchor.Y + widget.Position.Y);
            }

            FpsCounter.Update(0); // so that TextWidth is available
            FpsCounter.SetPosition(Width - FpsCounter.Text.TextWidth - 5, 5);
            MessageBoxBacking.SetPosition(0, Height - HudMinimapWidget.MinimapHeight);
            HudBar.SetPosition(0, Height - 18);
            BacklogBacking.SetPosition(127, Height - 467);
        }

        public override void Update(float dt)
        {
            HudMessageWindow.Update(dt);
            FpsCounter.Update(dt);
        }

        public override void Draw()
        {
            GraphicsEx.SetColor(Color.White);

            if (IsShowingBacklog)
            {
                BacklogBacking.Draw();
            }

            MessageBoxBacking.Draw();
            HudBar.Draw();
            HudMessageWindow.Draw();

            foreach (var widget in Widgets)
            {
                if (IsShowingBacklog && !widget.DrawFlags.HasFlag(WidgetDrawFlags.Backlog))
                    continue;

                GraphicsEx.SetColor(Color.White);
                widget.Widget.Draw();
            }

            FpsCounter.Draw();
        }

        public override void Dispose()
        {
            HudMessageWindow.Dispose();
            FpsCounter.Dispose();
            _field.OnScreenRefresh -= OnScreenRefresh;
        }

        public void ToggleBacklog(bool visible)
        {
        }

        public void ClearWidgets()
        {
            foreach (var widget in Widgets)
            {
                RemoveChild(widget.Widget);
            }
            Widgets.Clear();
        }
    }
}
