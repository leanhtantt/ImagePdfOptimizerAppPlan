using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileUtilityHub_WinUI.Shared.Controls;

/// <summary>
/// Shared quality stepper-slider control.
/// Composes Slider + NumberBox + stepper buttons for CRF/q values.
/// Generic: Feature 01 uses for AVIF CRF, Feature 03 will use for PDF q.
/// </summary>
public sealed partial class QualityStepperSlider : UserControl, INotifyPropertyChanged
{
    public QualityStepperSlider()
    {
        this.InitializeComponent();
    }

    // Value
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(QualityStepperSlider),
            new PropertyMetadata(24.0, OnValueChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QualityStepperSlider control)
            control.OnPropertyChanged(nameof(DisplayValue));
    }

    public string DisplayValue => Value.ToString("F0");

    // Minimum
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(QualityStepperSlider),
            new PropertyMetadata(0.0));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    // Maximum
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(QualityStepperSlider),
            new PropertyMetadata(40.0));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    // StepFrequency
    public static readonly DependencyProperty StepFrequencyProperty =
        DependencyProperty.Register(nameof(StepFrequency), typeof(double), typeof(QualityStepperSlider),
            new PropertyMetadata(2.0));

    public double StepFrequency
    {
        get => (double)GetValue(StepFrequencyProperty);
        set => SetValue(StepFrequencyProperty, value);
    }

    // LowLabel
    public static readonly DependencyProperty LowLabelProperty =
        DependencyProperty.Register(nameof(LowLabel), typeof(string), typeof(QualityStepperSlider),
            new PropertyMetadata("Đẹp hơn"));

    public string LowLabel
    {
        get => (string)GetValue(LowLabelProperty);
        set => SetValue(LowLabelProperty, value);
    }

    // HighLabel
    public static readonly DependencyProperty HighLabelProperty =
        DependencyProperty.Register(nameof(HighLabel), typeof(string), typeof(QualityStepperSlider),
            new PropertyMetadata("Nhẹ hơn"));

    public string HighLabel
    {
        get => (string)GetValue(HighLabelProperty);
        set => SetValue(HighLabelProperty, value);
    }

    private void OnDecrementClick(object sender, RoutedEventArgs e)
    {
        var newValue = Value - StepFrequency;
        if (newValue >= Minimum)
            Value = newValue;
    }

    private void OnIncrementClick(object sender, RoutedEventArgs e)
    {
        var newValue = Value + StepFrequency;
        if (newValue <= Maximum)
            Value = newValue;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
