// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.EntityClasses;

namespace AsyncDesignerValidationDemo;

/// <summary>
/// WinForms designer-based async validation demo organized around the four API proposal scenarios.
/// </summary>
public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private async void BtnValidateUser_Click(object? sender, EventArgs e)
    {
        var user = new User
        {
            Name = txtUserName.Text,
            Username = txtUserUsername.Text
        };

        await ValidateAndShowAsync(user, new ValidationContext(user), lblUserResult);
    }

    private async void BtnValidateOrder_Click(object? sender, EventArgs e)
    {
        var order = new Order
        {
            ProductName = txtOrderProduct.Text,
            Quantity = int.TryParse(txtOrderQuantity.Text, out var quantity) ? quantity : 0,
            UnitPrice = decimal.TryParse(txtOrderPrice.Text, out var unitPrice) ? unitPrice : 0m,
            Delay = int.TryParse(txtOrderDelay.Text, out var delay) ? delay : null
        };

        await ValidateAndShowAsync(order, new ValidationContext(order), lblOrderResult);
    }

    private async void BtnValidateMoneyTransfer_Click(object? sender, EventArgs e)
    {
        var transfer = new MoneyTransfer
        {
            FromAccount = txtTransferFromAccount.Text,
            ToAccount = txtTransferToAccount.Text,
            Amount = decimal.TryParse(txtTransferAmount.Text, out var amount) ? amount : 0m
        };

        await ValidateAndShowAsync(transfer, new ValidationContext(transfer), lblTransferResult);
    }

    private async void BtnValidateEvent_Click(object? sender, EventArgs e)
    {
        var eventModel = new Event
        {
            Title = txtEventTitle.Text,
            StartDate = dtpEventStart.Value,
            EndDate = dtpEventEnd.Value
        };

        await ValidateAndShowAsync(eventModel, new ValidationContext(eventModel), lblEventResult);
    }

    private static async Task ValidateAndShowAsync(object model, ValidationContext context, Label resultLabel)
    {
        var results = new List<ValidationResult>();
        try
        {
            bool isValid = await Validator.TryValidateObjectAsync(model, context, results, true);
            resultLabel.Text = isValid
                ? "✅ Valid!"
                : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
        }
        catch (OperationCanceledException)
        {
            resultLabel.Text = "⏹️ Validation was cancelled.";
        }
        catch (Exception ex)
        {
            resultLabel.Text = $"⚠️ Validation error: {ex.Message}";
        }
    }
}
