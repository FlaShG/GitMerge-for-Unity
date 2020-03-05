using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class MergeFilter
{
    public enum FilterMode
    {
        Inclusion,
        Exclusion
    }

    public bool useFilter { get; set; } = false;
    public bool isRegex { get; set; } = false;
    public bool isCaseSensitive { get; set; } = false;

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
            }
        }
    }

    public FilterMode filterMode { get; set; } = FilterMode.Inclusion;

    private Regex regex = new Regex(string.Empty);

    public bool IsPassingFilter(string input)
    {
        if (!useFilter)
        {
            return true;
        }

        bool isPassingFilter = false;

        if (isRegex)
        {
            isPassingFilter = regex.IsMatch(input);
        }
        else
        {
            if (!isCaseSensitive)
            {
                input = input.ToLowerInvariant();
                isPassingFilter = input.Contains(_expression.ToLowerInvariant());
            }
            else
            {
                isPassingFilter = input.Contains(_expression);
            }
        }

        if (filterMode == FilterMode.Exclusion)
        {
            isPassingFilter = !isPassingFilter;
        }

        return isPassingFilter;
    }
}
