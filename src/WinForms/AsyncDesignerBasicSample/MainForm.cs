// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;

namespace AsyncDesignerBasicSample;

/// <summary>
/// WinForms designer-based async validation with ErrorProvider.
/// Demonstrates UserRegistration, Event, and Order using designer-generated controls.
/// </summary>
public partial class MainForm : Form
{
    private readonly SimpleServiceProvider _serviceProvider = new SimpleServiceProvider()
        .Register(new UserService());
    private readonly UserRegistration _registration = new()
    {
        Username = "admin",
        Email = "admin@example.com",
        Password = "SecureP@ss123"
    };
    private readonly Event _event = new()
    {
        Title = "Launch Party",
        StartDate = new DateTime(2026, 12, 25),
        EndDate = new DateTime(2026, 12, 20),
        Delay = 3000
    };
    private readonly Order _order = new()
    {
        ProductName = "Widget",
        Quantity = 10_000,
        UnitPrice = 10m,
        Delay = 3000
    };

    public MainForm()
    {
        InitializeComponent();

        txtRegistrationPassword.UseSystemPasswordChar = true;
        txtRegistrationUsername.Validating += async (sender, args) =>
        {
            _registration.Username = txtRegistrationUsername.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtRegistrationUsername,
                _registration,
                nameof(UserRegistration.Username),
                txtRegistrationUsername.Text,
                _serviceProvider);
        };
        txtRegistrationEmail.Validating += async (sender, args) =>
        {
            _registration.Email = txtRegistrationEmail.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtRegistrationEmail,
                _registration,
                nameof(UserRegistration.Email),
                txtRegistrationEmail.Text,
                _serviceProvider);
        };
        txtRegistrationPassword.Validating += async (sender, args) =>
        {
            _registration.Password = txtRegistrationPassword.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtRegistrationPassword,
                _registration,
                nameof(UserRegistration.Password),
                txtRegistrationPassword.Text,
                _serviceProvider);
        };
    }

    private async void BtnValidateRegistration_Click(object? sender, EventArgs e)
    {
        _registration.Username = txtRegistrationUsername.Text;
        _registration.Email = txtRegistrationEmail.Text;
        _registration.Password = txtRegistrationPassword.Text;

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_registration, _serviceProvider);
        lblRegistrationResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }

    private async void BtnValidateEvent_Click(object? sender, EventArgs e)
    {
        _event.Title = txtEventTitle.Text;
        _event.StartDate = dtpEventStart.Value;
        _event.EndDate = dtpEventEnd.Value;
        if (int.TryParse(txtEventDelay.Text, out int delay))
        {
            _event.Delay = delay;
        }

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_event);
        lblEventResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }

    private async void BtnValidateOrder_Click(object? sender, EventArgs e)
    {
        _order.ProductName = txtOrderProduct.Text;
        if (int.TryParse(txtOrderQuantity.Text, out int quantity))
        {
            _order.Quantity = quantity;
        }

        if (decimal.TryParse(txtOrderPrice.Text, out decimal price))
        {
            _order.UnitPrice = price;
        }

        if (int.TryParse(txtOrderDelay.Text, out int delay))
        {
            _order.Delay = delay;
        }

        var (valid, results) = await ValidationHelper.ValidateObjectAsync(_order);
        lblOrderResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
    }
}
