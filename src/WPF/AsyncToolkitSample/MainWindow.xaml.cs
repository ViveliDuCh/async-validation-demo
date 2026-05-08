// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows;
using AsyncToolkitSample.ViewModels;

namespace AsyncToolkitSample;

public partial class MainWindow : Window
{
    public UserRegistrationViewModel UserRegistrationVM { get; } = new();
    public EventViewModel EventVM { get; } = new();
    public OrderViewModel OrderVM { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void ValidateUserRegistration_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool valid = await UserRegistrationVM.ValidateAllAsync();
            UserRegistrationResult.Text = valid ? "✅ Valid!" : "❌ Validation failed (async, UI stayed responsive).";
        }
        catch (OperationCanceledException)
        {
            UserRegistrationResult.Text = "⏹️ Validation cancelled.";
        }
        catch (Exception ex)
        {
            UserRegistrationResult.Text = $"⚠️ Validation error: {ex.Message}";
        }
    }

    private async void ValidateEvent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool valid = await EventVM.ValidateAllAsync();
            EventResult.Text = valid ? "✅ Valid!" : "❌ Validation failed (async, UI stayed responsive).";
        }
        catch (OperationCanceledException)
        {
            EventResult.Text = "⏹️ Validation cancelled.";
        }
        catch (Exception ex)
        {
            EventResult.Text = $"⚠️ Validation error: {ex.Message}";
        }
    }

    private async void ValidateOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool valid = await OrderVM.ValidateAllAsync();
            OrderResult.Text = valid ? "✅ Valid!" : "❌ Validation failed (async, UI stayed responsive).";
        }
        catch (OperationCanceledException)
        {
            OrderResult.Text = "⏹️ Validation cancelled.";
        }
        catch (Exception ex)
        {
            OrderResult.Text = $"⚠️ Validation error: {ex.Message}";
        }
    }

}
