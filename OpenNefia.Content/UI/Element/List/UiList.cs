﻿using OpenNefia.Core;
using OpenNefia.Core.Audio;
using OpenNefia.Core.Maths;
using OpenNefia.Core.UI;
using OpenNefia.Core.UI.Element;
using OpenNefia.Content.Prototypes;
using System.Collections;
using OpenNefia.Core.Utility;
using OpenNefia.Core.Input;
using OpenNefia.Core.UserInterface;

namespace OpenNefia.Content.UI.Element.List
{
    public class UiList<T> : UiElement, IUiList<T>, IRawInputControl
    {
        public const float DEFAULT_ITEM_HEIGHT = 19f;

        protected IList<UiListCell<T>> AllCells { get; }
        public virtual IReadOnlyList<UiListCell<T>> DisplayedCells => (IReadOnlyList<UiListCell<T>>)AllCells;

        public float ItemHeight { get; }
        public float ItemOffsetX { get; }

        public bool HighlightSelected { get; set; }
        public bool SelectOnActivate { get; set; }

        private int _SelectedIndex;
        private bool _needsUpdate;

        public int SelectedIndex
        {
            get => _SelectedIndex;
            set
            {
                _SelectedIndex = Math.Clamp(value, 0, DisplayedCells.Count());
            }
        }

        public IUiListCell<T>? SelectedCell
        {
            get
            {
                if (DisplayedCells.Count == 0 || SelectedIndex < 0 || SelectedIndex >= DisplayedCells.Count)
                    return null;

                return DisplayedCells[SelectedIndex];
            }
        }

        protected readonly Dictionary<int, UiListChoiceKey> ChoiceKeys = new();

        public event UiListEventHandler<T>? OnSelected;
        public event UiListEventHandler<T>? OnActivated;

        public UiList(IEnumerable<UiListCell<T>>? cells = null, int itemOffsetX = 0)
        {
            if (cells == null)
                cells = new List<UiListCell<T>>();

            ItemHeight = DEFAULT_ITEM_HEIGHT;
            ItemOffsetX = 0;
            HighlightSelected = true;
            SelectOnActivate = true;

            AllCells = cells.ToList();

            _needsUpdate = true;

            OnKeyBindDown += HandleKeyBindDown;
            EventFilter = UIEventFilterMode.Pass;
            CanControlFocus = true;
            CanKeyboardFocus = true;
        }

        protected virtual void UpdateDisplayedCells(bool setSize)
        {
            foreach (var child in Children.ToList())
            {
                // Don't unparent things like UiPageText.
                if (AllCells.Contains(child))
                    RemoveChild(child);
            }

            ChoiceKeys.Clear();
            for (var i = 0; i < DisplayedCells.Count; i++)
            {
                var cell = DisplayedCells[i];
                cell.IndexInList = i;
                if (cell.Key == null)
                {
                    cell.Key = UiListChoiceKey.MakeDefault(i);
                }
                ChoiceKeys[i] = cell.Key;
                UiHelpers.AddChildrenRecursive(this, cell);
            }

            if (setSize)
            {
                // Set the size/position of the child list cells.
                SetSize(Width, Height);
                SetPosition(X, Y);
            }
        }

        public UiList(IEnumerable<T> items, int itemOffsetX = 0)
            : this(MakeDefaultList(items), itemOffsetX)
        {
        }

        protected virtual void HandleKeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.UISelect)
            {
                Activate(SelectedIndex);
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.UIClick)
            {
                if (UserInterfaceManager.CurrentlyHovered == SelectedCell)
                {
                    Activate(SelectedIndex);
                    args.Handle();
                }
            }
            else if (args.Function == EngineKeyFunctions.UIUp)
            {
                Sounds.Play(Protos.Sound.Cursor1);
                IncrementIndex(-1);
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.UIDown)
            {
                Sounds.Play(Protos.Sound.Cursor1);
                IncrementIndex(1);
                args.Handle();
            }
        }

        public bool RawKeyEvent(in GuiRawKeyEvent guiRawEvent)
        {
            if (guiRawEvent.Action != RawKeyAction.Down)
                return false;

            for (int index = 0; index < ChoiceKeys.Count; index++)
            {
                var choiceKey = ChoiceKeys[index];
                if (choiceKey.Key == guiRawEvent.Key)
                {
                    Activate(index);
                    return true;
                }
            }

            return false;
        }

        public override void Localize(LocaleKey key)
        {
            for (int i = 0; i < AllCells.Count; i++)
            {
                var cell = AllCells[i];
                cell.Localize(key.With(cell.LocalizeKey ?? i.ToString()));
            }
        }

        private static IEnumerable<UiListCell<TItem>> MakeDefaultList<TItem>(IEnumerable<TItem> items)
        {
            UiListCell<TItem> MakeListCell(TItem item, int index)
            {
                if (item is IUiListItem)
                {
                    var listItem = (IUiListItem)item;
                    return new UiListCell<TItem>(item, listItem.GetChoiceText(index), listItem.GetChoiceKey(index));
                }
                else
                {
                    return new UiListCell<TItem>(item, $"{item}", UiListChoiceKey.MakeDefault(index));
                }
            }
            return items.Select(MakeListCell);
        }

        #region Data Creation

        public override List<UiKeyHint> MakeKeyHints()
        {
            return new List<UiKeyHint>();
        }

        #endregion

        #region List Handling

        protected virtual void HandleSelect(UiListEventArgs<T> e)
        {
            OnSelected?.Invoke(this, e);
        }

        protected virtual void HandleActivate(UiListEventArgs<T> e)
        {
            OnActivated?.Invoke(this, e);
        }

        public virtual bool CanSelect(int index)
        {
            return index >= 0 && index < DisplayedCells.Count;
        }

        public void IncrementIndex(int delta)
        {
            if (DisplayedCells.Count == 0)
                return;

            var newIndex = SelectedIndex + delta;
            var sign = Math.Sign(delta);

            while (!CanSelect(newIndex) && newIndex != SelectedIndex)
            {
                newIndex += sign;
                if (newIndex < 0)
                    newIndex = DisplayedCells.Count - 1;
                else if (newIndex >= DisplayedCells.Count)
                    newIndex = 0;
            }
            Select(newIndex);
        }

        public void Select(int index)
        {
            if (!CanSelect(index))
                return;

            SelectedIndex = index;
            HandleSelect(new UiListEventArgs<T>(DisplayedCells[index], index));
        }

        public virtual bool CanActivate(int index)
        {
            return index >= 0 && index < DisplayedCells.Count;
        }

        public void Activate(int index)
        {
            if (!CanActivate(index))
            {
                return;
            }

            if (SelectOnActivate)
                Select(index);

            HandleActivate(new UiListEventArgs<T>(DisplayedCells[index], index));
        }

        public virtual void SetCells(IEnumerable<UiListCell<T>> items, bool dispose = true)
        {
            var index = SelectedIndex;
            if (dispose)
            {
                foreach (var cell in AllCells)
                    cell.Dispose();
            }
            AllCells.Clear();
            AllCells.AddRange(items);
            if (DisplayedCells.Count > 0)
                Select(Math.Clamp(index, 0, DisplayedCells.Count - 1));
            UpdateAllCells();
        }

        public void CreateAndSetCells(IEnumerable<T> items)
        {
            SetCells(MakeDefaultList(items));
        }

        #endregion

        #region UI Handling

        public override void SetPosition(float x, float y)
        {
            base.SetPosition(x, y);

            var iy = Y;

            for (int index = 0; index < DisplayedCells.Count; index++)
            {
                var cell = DisplayedCells[index];
                cell.XOffset = ItemOffsetX;
                cell.SetPosition(X, iy);
                
                iy += cell.Height;
            }
        }

        public override void GetPreferredSize(out Vector2 size)
        {
            size = Vector2.Zero;

            for (int index = 0; index < DisplayedCells.Count; index++)
            {
                var cell = DisplayedCells[index];
                cell.GetPreferredSize(out var cellSize);
                size.X = MathF.Max(size.X, cellSize.X);
                size.Y += MathF.Max(cellSize.Y, ItemHeight);
            }
        }

        public override void SetSize(float width, float height)
        {
            var totalHeight = 0f;

            for (int index = 0; index < DisplayedCells.Count; index++)
            {
                var cell = DisplayedCells[index];
                cell.GetPreferredSize(out var cellSize);
                var cellHeight = MathF.Max(cellSize.Y, ItemHeight);
                cell.SetSize(width, cellHeight);
                width = MathF.Max(width, cell.Width);
                totalHeight += cell.Height;
            }

            base.SetSize(width, Math.Max(height, totalHeight));
        }

        protected void UpdateAllCells()
        {
            UpdateDisplayedCells(setSize: true);
            _needsUpdate = false;
        }

        public override void Update(float dt)
        {
            if (_needsUpdate)
            {
                UpdateAllCells();
            }

            for (int i = 0; i < DisplayedCells.Count; i++)
            {
                var cell = DisplayedCells[i];
                cell.Update(dt);
            }
        }

        public override void Draw()
        {
            for (int index = 0; index < DisplayedCells.Count; index++)
            {
                var cell = DisplayedCells[index];
                cell.Draw();

                if (HighlightSelected && index == SelectedIndex)
                {
                    cell.DrawHighlight();
                }
            }
        }

        public override void Dispose()
        {
            foreach (var cell in AllCells)
            {
                cell.Dispose();
            }
        }

        #endregion

        #region IReadOnlyList implementation

        public int Count => AllCells.Count;
        public bool IsReadOnly => AllCells.IsReadOnly;

        public UiListCell<T> this[int index]
        {
            get => AllCells[index];
            set
            {
                AllCells[index] = value;
                _needsUpdate = true;
            }
        }
        public int IndexOf(UiListCell<T> item)
        {
            return AllCells.IndexOf(item);
        }

        public bool Contains(UiListCell<T> item) => AllCells.Contains(item);
        public void CopyTo(UiListCell<T>[] array, int arrayIndex) => AllCells.CopyTo(array, arrayIndex);

        public bool IsFixedSize => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public IEnumerator<UiListCell<T>> GetEnumerator() => AllCells.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => AllCells.GetEnumerator();

        #endregion
    }
}
