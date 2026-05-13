// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

namespace AsyncDesignerBasicSample;

/// <summary>
/// WinForms designer-based async validation with ErrorProvider.
/// Demonstrates the four API proposal scenarios using designer-generated controls.
/// </summary>
public partial class MainForm : Form
{
    private readonly User _user = new()
    {
        Name = "Bob",
        Username = "admin"
    };
    private readonly Order _order = new()
    {
        ProductName = "Gadget",
        Quantity = 250,
        UnitPrice = 250m,
        Delay = 100
    };
    private readonly MoneyTransfer _moneyTransfer = new()
    {
        FromAccount = "checking",
        ToAccount = "checking",
        Amount = 1000m
    };
    private readonly Event _event = new()
    {
        Title = "TBD Kickoff",
        StartDate = new DateTime(2026, 6, 1),
        EndDate = new DateTime(2026, 6, 2)
    };

    public MainForm()
    {
        InitializeComponent();

        txtUserName.Validating += async (_, _) =>
        {
            _user.Name = txtUserName.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtUserName,
                _user,
                nameof(User.Name),
                txtUserName.Text);
        };

        txtUserUsername.Validating += async (_, _) =>
        {
            _user.Username = txtUserUsername.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtUserUsername,
                _user,
                nameof(User.Username),
                txtUserUsername.Text);
        };
    }

    private async void BtnValidateUser_Click(object? sender, EventArgs e)
    {
        _user.Name = txtUserName.Text;
        _user.Username = txtUserUsername.Text;

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_user);
        lblUserResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }

    private async void BtnValidateOrder_Click(object? sender, EventArgs e)
    {
        _order.ProductName = txtOrderProduct.Text;
        _order.Quantity = int.TryParse(txtOrderQuantity.Text, out var quantity) ? quantity : 0;
        _order.UnitPrice = decimal.TryParse(txtOrderPrice.Text, out var price) ? price : 0m;
        _order.Delay = int.TryParse(txtOrderDelay.Text, out var delay) ? delay : null;

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_order);
        lblOrderResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }

    private async void BtnValidateMoneyTransfer_Click(object? sender, EventArgs e)
    {
        _moneyTransfer.FromAccount = txtTransferFromAccount.Text;
        _moneyTransfer.ToAccount = txtTransferToAccount.Text;
        _moneyTransfer.Amount = decimal.TryParse(txtTransferAmount.Text, out var amount) ? amount : 0m;

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_moneyTransfer);
        lblTransferResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }

    private async void BtnValidateEvent_Click(object? sender, EventArgs e)
    {
        _event.Title = txtEventTitle.Text;
        _event.StartDate = dtpEventStart.Value;
        _event.EndDate = dtpEventEnd.Value;

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_event);
        lblEventResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }
}
