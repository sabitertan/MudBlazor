﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor.Interfaces;
using MudBlazor.Utilities;

namespace MudBlazor
{
    public partial class MudChipSet : MudComponentBase, IDisposable
    {
        protected string Classname =>
            new CssBuilder("mud-chipset")
                .AddClass(Class)
                .Build();

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Allows to select more than one chip.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public bool MultiSelection { get; set; } = false;

        /// <summary>
        /// Will not allow to deselect the selected chip in single selection mode.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public bool Mandatory { get; set; } = false;

        /// <summary>
        /// Will make all chips closable.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public bool AllClosable { get; set; } = false;

        /// <summary>
        ///  Will show a check-mark for the selected components.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Appearance)]
        public bool Filter
        {
            get => _filter;
            set
            {
                if (_filter == value)
                    return;
                _filter = value;
                StateHasChanged();
                foreach (IMudStateHasChanged chip in _chips)
                    chip.StateHasChanged();
            }
        }

        /// <summary>
        ///  Will make all chips read only.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// The currently selected chip in Choice mode
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public MudChip SelectedChip
        {
            get { return _chips.FirstOrDefault(x => x.IsSelected); }
            set
            {
                if (value == null)
                {
                    foreach (var chip in _chips)
                    {
                        chip.IsSelected = false;
                    }
                }
                else
                {
                    foreach (var chip in _chips)
                    {
                        chip.IsSelected = (chip == value);
                    }
                }
                this.InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Called when the selected chip changes, in Choice mode
        /// </summary>
        [Parameter]
        public EventCallback<MudChip> SelectedChipChanged { get; set; }

        /// <summary>
        /// The currently selected chips in Filter mode
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public MudChip[] SelectedChips
        {
            get { return _chips.Where(x => x.IsSelected).ToArray(); }
            set
            {
                if (value == null || value.Length == 0)
                {
                    foreach (var chip in _chips)
                    {
                        chip.IsSelected = false;
                    }
                    _lastSelectedValues = null;
                }
                else
                {
                    var selected = new HashSet<MudChip>(value);
                    foreach (var chip in _chips)
                    {
                        chip.IsSelected = selected.Contains(chip);
                    }
                }
                StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (_selectedValues == null)
                _selectedValues = new HashSet<object>(_comparer);
            _initialSelectedValues = new HashSet<object>(_selectedValues, _comparer);
        }

        private IEqualityComparer<object> _comparer;
        private HashSet<object> _selectedValues;
        private HashSet<object> _initialSelectedValues;

        /// <summary>
        /// The Comparer to use for comparing selected values internally.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public IEqualityComparer<object> Comparer
        {
            get => _comparer;
            set
            {
                _comparer = value;
                // Apply comparer and refresh selected values
                _selectedValues = new HashSet<object>(_selectedValues, _comparer);
                SelectedValues = _selectedValues;
            }
        }

        /// <summary>
        /// Called when the selection changed, in Filter mode
        /// </summary>
        [Parameter]
        public EventCallback<MudChip[]> SelectedChipsChanged { get; set; }

        /// <summary>
        /// The current selected value.
        /// Note: make the list Clickable for item selection to work.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.ChipSet.Behavior)]
        public ICollection<object> SelectedValues
        {
            get => _selectedValues;
            set
            {
                if (value == null)
                    SetSelectedValuesAsync(Array.Empty<object>());
                else
                    SetSelectedValuesAsync(value.ToArray()).AndForget();
            }
        }

        /// <summary>
        /// Called whenever the selection changed
        /// </summary>
        [Parameter]
        public EventCallback<ICollection<object>> SelectedValuesChanged { get; set; }

        internal Task SetSelectedValuesAsync(object[] values)
        {
            HashSet<object> newValues = null;
            if (values == null)
                values = Array.Empty<object>();
            if (MultiSelection)
                newValues = new HashSet<object>(values, _comparer);
            else
            {
                newValues = new HashSet<object>(_comparer);
                if (values.Length > 0)
                    newValues.Add(values.First());
            }
            // avoid update with same values
            if (_selectedValues.IsEqualTo(newValues))
                return Task.CompletedTask;
            _selectedValues = newValues;
            foreach (var chip in _chips.ToArray())
            {
                var isSelected = _selectedValues.Contains(chip.Value);
                chip.IsSelected = isSelected;
            }
            return NotifySelectionAsync();
        }

        /// <summary>
        /// Called when a Chip was deleted (by click on the close icon)
        /// </summary>
        [Parameter]
        public EventCallback<MudChip> OnClose { get; set; }

        internal Task AddAsync(MudChip chip)
        {
            _chips.Add(chip);
            if (_selectedValues.Contains(chip.Value))
                chip.IsSelected = true;
            return CheckDefaultAsync(chip);
        }

        internal Task RemoveAsync(MudChip chip)
        {
            _chips.Remove(chip);
            if (chip.IsSelected)
            {
                _selectedValues.Remove(chip.Value);
                return NotifySelectionAsync();
            }
            return Task.CompletedTask;
        }

        private async Task CheckDefaultAsync(MudChip chip)
        {
            if (!MultiSelection)
                return;
            if (chip.DefaultProcessed)
                return;
            chip.DefaultProcessed = true;
            if (chip.Default == null)
                return;
            var oldSelected = chip.IsSelected;
            chip.IsSelected = chip.Default == true;
            if (chip.IsSelected != oldSelected)
            {
                if (chip.IsSelected)
                    _selectedValues.Add(chip.Value);
                else
                    _selectedValues.Remove(chip.Value);
                await NotifySelectionAsync();
            }
        }

        private HashSet<MudChip> _chips = new();
        private bool _filter;

        internal Task OnChipClickedAsync(MudChip chip)
        {
            var wasSelected = chip.IsSelected;
            if (MultiSelection)
            {
                chip.IsSelected = !chip.IsSelected;
            }
            else
            {
                foreach (var ch in _chips)
                {
                    ch.IsSelected = (ch == chip); // <-- exclusively select the one chip only, thus all others must be deselected
                }
                if (!Mandatory)
                    chip.IsSelected = !wasSelected;
            }
            UpdateSelectedValues();
            return NotifySelectionAsync();
        }

        private void UpdateSelectedValues()
        {
            _selectedValues = new HashSet<object>(_chips.Where(x => x.IsSelected).Select(x => x.Value), _comparer);
        }

        private object[] _lastSelectedValues = null;

        private async Task NotifySelectionAsync()
        {
            if (_disposed)
                return;
            // to avoid endless notification loops we check if selection has really changed
            if (_selectedValues.IsEqualTo(_lastSelectedValues))
                return;
            _lastSelectedValues = _selectedValues.ToArray();
            await SelectedChipChanged.InvokeAsync(SelectedChip);
            await SelectedChipsChanged.InvokeAsync(SelectedChips);
            await SelectedValuesChanged.InvokeAsync(SelectedValues);
            StateHasChanged();
        }

        [Obsolete($"Use {nameof(OnChipDeletedAsync)} instead. This will be removed in v7.")]
        public void OnChipDeleted(MudChip chip)
        {
            RemoveAsync(chip).AndForget();
            OnClose.InvokeAsync(chip);
        }

        public async Task OnChipDeletedAsync(MudChip chip)
        {
            await RemoveAsync(chip);
            await OnClose.InvokeAsync(chip);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await SelectDefaultChipsAsync();
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task SelectDefaultChipsAsync()
        {
            if (!MultiSelection)
            {
                var anySelected = false;
                var defaultChip = _chips.LastOrDefault(chip => chip.Default == true);
                if (defaultChip != null)
                {
                    defaultChip.IsSelected = true;
                    anySelected = true;
                }
                if (anySelected)
                {
                    UpdateSelectedValues();
                    await NotifySelectionAsync();
                }
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
