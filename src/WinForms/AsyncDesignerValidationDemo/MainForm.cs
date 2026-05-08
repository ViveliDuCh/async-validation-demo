// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;

namespace AsyncDesignerValidationDemo;

/// <summary>
/// WinForms designer-based async validation demo with DI,
/// event/order validation, two-phase validation, and error handling.
/// </summary>
public partial class MainForm : Form
{
    private readonly SimpleServiceProvider _serviceProvider;

    public MainForm()
    {
        InitializeComponent();
        _serviceProvider = new SimpleServiceProvider()
            .Register(new UserService());
        txtRegPassword.UseSystemPasswordChar = true;
        txtErrorPassword.UseSystemPasswordChar = true;
        txtTwoPhasePassword.UseSystemPasswordChar = true;
    }

    private async void BtnValidateReg_Click(object? sender, EventArgs e)
    {
        var registration = new UserRegistration
        {
            Username = txtRegUsername.Text,
            Email = txtRegEmail.Text,
            Password = txtRegPassword.Text
        };

        var context = new ValidationContext(registration, _serviceProvider, null);
        var results = new List<ValidationResult>();
        try
        {
            bool isValid = await Validator.TryValidateObjectAsync(registration, context, results, true);
            lblRegResult.Text = isValid
                ? "✅ Valid!"
                : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
        }
        catch (OperationCanceledException)
        {
            lblRegResult.Text = "⏹️ Validation was cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            lblRegResult.Text = $"⚠️ Infrastructure error: {ex.Message}";
        }
        catch (Exception ex)
        {
            lblRegResult.Text = $"⚠️ Unexpected validation error: {ex.Message}";
        }
    }

    private async void BtnValidateOrder_Click(object? sender, EventArgs e)
    {
        var order = new Order
        {
            ProductName = txtOrderProduct.Text,
            Quantity = int.TryParse(txtOrderQuantity.Text, out int quantity) ? quantity : 0,
            UnitPrice = decimal.TryParse(txtOrderPrice.Text, out decimal unitPrice) ? unitPrice : 0m,
            Delay = int.TryParse(txtOrderDelay.Text, out int delay) ? delay : null
        };

        var context = new ValidationContext(order);
        var results = new List<ValidationResult>();
        try
        {
            bool isValid = await Validator.TryValidateObjectAsync(order, context, results, true);
            lblOrderResult.Text = isValid
                ? "✅ Valid!"
                : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
        }
        catch (OperationCanceledException)
        {
            lblOrderResult.Text = "⏹️ Validation was cancelled.";
        }
        catch (Exception ex)
        {
            lblOrderResult.Text = $"⚠️ Validation error: {ex.Message}";
        }
    }

    private async void BtnValidateEvent_Click(object? sender, EventArgs e)
    {
        var eventModel = new Event
        {
            Title = txtEventTitle.Text,
            StartDate = dtpEventStart.Value,
            EndDate = dtpEventEnd.Value,
            Delay = int.TryParse(txtEventDelay.Text, out int delay) ? delay : null
        };

        var context = new ValidationContext(eventModel);
        var results = new List<ValidationResult>();
        try
        {
            bool isValid = await Validator.TryValidateObjectAsync(eventModel, context, results, true);
            lblEventResult.Text = isValid
                ? "✅ Valid!"
                : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
        }
        catch (OperationCanceledException)
        {
            lblEventResult.Text = "⏹️ Validation was cancelled.";
        }
        catch (Exception ex)
        {
            lblEventResult.Text = $"⚠️ Validation error: {ex.Message}";
        }
    }

    private async void BtnValidateError_Click(object? sender, EventArgs e)
    {
        var registration = new UserRegistration
        {
            Username = txtErrorUsername.Text,
            Email = txtErrorEmail.Text,
            Password = txtErrorPassword.Text
        };

        var context = new ValidationContext(registration, _serviceProvider, null);
        var results = new List<ValidationResult>();
        try
        {
            bool isValid = await Validator.TryValidateObjectAsync(registration, context, results, true);
            lblErrorResult.Text = isValid
                ? "✅ Valid!"
                : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
        }
        catch (InvalidOperationException ex)
        {
            lblErrorResult.Text = $"⚠️ Infrastructure error caught: {ex.Message}";
        }
        catch (OperationCanceledException)
        {
            lblErrorResult.Text = "⏹️ Validation was cancelled.";
        }
        catch (Exception ex)
        {
            lblErrorResult.Text = $"⚠️ Unexpected validation error: {ex.Message}";
        }
    }

    private async void BtnValidateTwoPhase_Click(object? sender, EventArgs e)
    {
        var registration = new UserRegistration
        {
            Username = txtTwoPhaseUsername.Text,
            Email = txtTwoPhaseEmail.Text,
            Password = txtTwoPhasePassword.Text
        };

        var context = new ValidationContext(registration, _serviceProvider, null);
        var results = new List<ValidationResult>();
        try
        {
            bool isValid = await Validator.TryValidateObjectAsync(registration, context, results, true);

            string message = isValid
                ? "✅ Valid!"
                : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
            message += "\n(Note: async UniqueEmail check was skipped because sync EmailAddress failed first)";
            lblTwoPhaseResult.Text = message;
        }
        catch (OperationCanceledException)
        {
            lblTwoPhaseResult.Text = "⏹️ Validation was cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            lblTwoPhaseResult.Text = $"⚠️ Infrastructure error: {ex.Message}";
        }
        catch (Exception ex)
        {
            lblTwoPhaseResult.Text = $"⚠️ Unexpected validation error: {ex.Message}";
        }
    }
}
