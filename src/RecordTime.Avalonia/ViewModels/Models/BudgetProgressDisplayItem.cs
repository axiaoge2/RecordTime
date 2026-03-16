using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using RecordTime.Core.Models;

namespace RecordTime.Avalonia.ViewModels;

public partial class BudgetProgressDisplayItem : ObservableObject
{
    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _typeLabel = string.Empty;

    [ObservableProperty]
    private BudgetType _budgetType;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isOverBudget;

    [ObservableProperty]
    private bool _isGoalMet;

    [ObservableProperty]
    private IBrush _progressColor = new SolidColorBrush(Color.Parse("#4ECDC4"));

    public void UpdateProgressColor()
    {
        string colorHex;
        if (BudgetType == BudgetType.Maximum)
        {
            if (IsOverBudget) colorHex = "#FF6B6B";
            else if (ProgressPercentage >= 80) colorHex = "#FFB347";
            else colorHex = "#4ECDC4";
        }
        else
        {
            if (IsGoalMet) colorHex = "#4ECDC4";
            else if (ProgressPercentage >= 50) colorHex = "#FFB347";
            else colorHex = "#8E8E93";
        }
        ProgressColor = new SolidColorBrush(Color.Parse(colorHex));
    }
}
