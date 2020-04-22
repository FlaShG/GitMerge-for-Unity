using GitMerge;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GitMerge
{
    public class MergeFilter
    {
        public event Action OnChanged;

        public enum FilterMode
        {
            Inclusion,
            Exclusion
        }

        [System.Flags]
        public enum FilterState
        {
            Conflict = 0x01,
            Done = 0x02
        }

        private bool _useFilter = false;
        public bool useFilter
        {
            get => _useFilter;
            set
            {
                if (_useFilter != value)
                {
                    _useFilter = value;
                    OnChanged?.Invoke();
                }
            }
        }

        private bool _isRegex = false;
        public bool isRegex
        {
            get => _isRegex;
            set
            {
                if (_isRegex != value)
                {
                    _isRegex = value;
                    OnChanged?.Invoke();
                }
            }
        }

        private bool _isCaseSensitive = false;
        public bool isCaseSensitive
        {
            get => _isCaseSensitive;
            set
            {
                if (_isCaseSensitive != value)
                {
                    _isCaseSensitive = value;
                    OnChanged?.Invoke();
                }
            }
        }

        private string _expression = string.Empty;
        public string expression
        {
            get => _expression;
            set
            {
                if (_expression != value)
                {
                    _expression = value;
                    regex = new Regex(_expression);
                    OnChanged?.Invoke();
                }
            }
        }

        private FilterMode _filterMode = FilterMode.Inclusion;
        public FilterMode filterMode
        {
            get => _filterMode;
            set
            {
                if (_filterMode != value)
                {
                    _filterMode = value;
                    OnChanged?.Invoke();
                }
            }
        }

        private FilterState _filterState = (FilterState)(-1);
        public FilterState filterState
        {
            get => _filterState;
            set
            {
                if (_filterState != value)
                {
                    _filterState = value;
                    OnChanged?.Invoke();
                }
            }
        }

        private Regex regex = new Regex(string.Empty);

        public bool IsPassingFilter(GameObjectMergeActions action)
        {
            if (!useFilter)
            {
                return true;
            }

            bool isPassingFilter = false;

            string name = action.name;

            if (isRegex)
            {
                isPassingFilter = regex.IsMatch(name);
            }
            else
            {
                if (!isCaseSensitive)
                {
                    name = name.ToLowerInvariant();
                    isPassingFilter = name.Contains(_expression.ToLowerInvariant());
                }
                else
                {
                    isPassingFilter = name.Contains(_expression);
                }
            }

            if (filterMode == FilterMode.Exclusion)
            {
                isPassingFilter = !isPassingFilter;
            }

            bool isPassingFilterState = false;
            if ((_filterState & FilterState.Conflict) != 0)
            {
                isPassingFilterState |= !action.merged;
            }

            if ((_filterState & FilterState.Done) != 0)
            {
                isPassingFilterState |= action.merged;
            }
            isPassingFilter &= isPassingFilterState;

            return isPassingFilter;
        }
    }
}