﻿using OpenNefia.Core.Graphics;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Maths;
using OpenNefia.Core.Timing;
using OpenNefia.Core.UI.Wisp.Styling;

namespace OpenNefia.Core.UI.Wisp
{
    /// <summary>
    /// TODO: Intent is for this to be merged into <see cref="UserInterface.IUserInterfaceManagerInternal"/>.
    /// </summary>
    public interface IWispManager
    {
        /// <summary>
        ///     Default style sheet that applies to all controls
        ///     that do not have a more specific style sheet via <see cref="Control.Stylesheet"/>.
        /// </summary>
        Stylesheet? Stylesheet { get; set; }

        void AddRoot(WispRoot root);
        void RemoveRoot(WispRoot root);
        void QueueStyleUpdate(WispControl control);
        void QueueArrangeUpdate(WispControl control);
        void QueueMeasureUpdate(WispControl control);
        void FrameUpdate(FrameEventArgs args);

        /// <summary>
        /// Gets a style fallback.
        /// </summary>
        /// <seealso cref="IStylesheetManager.GetStyleFallback{T}"/>
        T GetStyleFallback<T>();
    }

    public sealed class WispManager : IWispManager
    {
        [Dependency] private readonly IGraphics _graphics = default!;
        [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;

        private readonly Queue<WispControl> _styleUpdateQueue = new();
        private readonly Queue<WispControl> _measureUpdateQueue = new();
        private readonly Queue<WispControl> _arrangeUpdateQueue = new();

        private readonly List<WispRoot> _roots = new();

        private Stylesheet? _stylesheet;
        public Stylesheet? Stylesheet
        {
            get => _stylesheet;
            set
            {
                _stylesheet = value;

                foreach (var root in _roots)
                {
                    if (root.Stylesheet == null)
                    {
                        root.StylesheetUpdateRecursive();
                    }
                }
            }
        }

        public void AddRoot(WispRoot root)
        {
            _roots.Add(root);
        }

        public void RemoveRoot(WispRoot root)
        {
            _roots.Remove(root);
        }

        public void QueueStyleUpdate(WispControl control)
        {
            _styleUpdateQueue.Enqueue(control);
        }

        public void QueueMeasureUpdate(WispControl control)
        {
            _measureUpdateQueue.Enqueue(control);
            _arrangeUpdateQueue.Enqueue(control);
        }

        public void QueueArrangeUpdate(WispControl control)
        {
            _arrangeUpdateQueue.Enqueue(control);
        }

        public void FrameUpdate(FrameEventArgs args)
        {
            // Process queued style & layout updates.
            while (_styleUpdateQueue.Count != 0)
            {
                var control = _styleUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                control.DoStyleUpdate();
            }

            while (_measureUpdateQueue.Count != 0)
            {
                var control = _measureUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                RunMeasure(control);
            }

            while (_arrangeUpdateQueue.Count != 0)
            {
                var control = _arrangeUpdateQueue.Dequeue();

                if (control.Disposed)
                {
                    continue;
                }

                RunArrange(control);
            }

            // count down tooltip delay if we're not showing one yet and
            // are hovering the mouse over a control without moving it
            //if (_tooltipDelay != null && !showingTooltip)
            //{
            //    _tooltipTimer += args.DeltaSeconds;
            //    if (_tooltipTimer >= _tooltipDelay)
            //    {
            //        _showTooltip();
            //    }
            //}
        }

        private void RunMeasure(WispControl control)
        {
            if (control.IsMeasureValid || !control.IsInsideTree)
                return;

            if (control.WispParent != null)
            {
                RunMeasure(control.WispParent);
            }

            if (control is WispRoot)
            {
                control.Measure(_graphics.WindowSize);
            }
            else if (control.PreviousMeasure.HasValue)
            {
                control.Measure(control.PreviousMeasure.Value);
            }
        }

        private void RunArrange(WispControl control)
        {
            if (control.IsArrangeValid || !control.IsInsideTree)
                return;

            if (control.WispParent != null)
            {
                RunArrange(control.WispParent);
            }

            if (control is WispRoot root)
            {
                control.Arrange(UIBox2.FromDimensions(Vector2.Zero, _graphics.WindowSize));
            }
            else if (control.PreviousArrange.HasValue)
            {
                control.Arrange(control.PreviousArrange.Value);
            }
        }

        public T GetStyleFallback<T>() => _stylesheetManager.GetStyleFallback<T>();
    }
}
