// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;

namespace AsyncBasicSample;

/// <summary>
/// WinForms async DataAnnotations validation with ErrorProvider.
/// Demonstrates async validation for UserRegistration, Event, and Order.
/// </summary>
public partial class MainForm : Form
{
    private readonly ErrorProvider _errorProvider = new();
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

    private TabControl _tabControl = null!;

    public MainForm()
    {
        Text = "WinForms AsyncBasicSample — Async ErrorProvider + Validator";
        Size = new System.Drawing.Size(650, 500);
        _errorProvider.ContainerControl = this;

        _tabControl = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(_tabControl);

        _tabControl.TabPages.Add(CreateRegistrationTab());
        _tabControl.TabPages.Add(CreateEventTab());
        _tabControl.TabPages.Add(CreateOrderTab());
    }

    private TabPage CreateRegistrationTab()
    {
        var tab = new TabPage("UserRegistration");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "DI + Async Duplicate Detection (UniqueUsername + UniqueEmail)", AutoSize = true });

        var txtUsername = CreateField(panel, "Username:", _registration.Username ?? "");
        txtUsername.Validating += async (s, e) =>
        {
            _registration.Username = txtUsername.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtUsername,
                _registration,
                nameof(UserRegistration.Username),
                txtUsername.Text,
                _serviceProvider);
        };

        var txtEmail = CreateField(panel, "Email:", _registration.Email ?? "");
        txtEmail.Validating += async (s, e) =>
        {
            _registration.Email = txtEmail.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtEmail,
                _registration,
                nameof(UserRegistration.Email),
                txtEmail.Text,
                _serviceProvider);
        };

        var txtPassword = CreateField(panel, "Password:", _registration.Password ?? "");
        txtPassword.UseSystemPasswordChar = true;
        txtPassword.Validating += async (s, e) =>
        {
            _registration.Password = txtPassword.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtPassword,
                _registration,
                nameof(UserRegistration.Password),
                txtPassword.Text,
                _serviceProvider);
        };

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed };
        btnValidate.Click += async (s, e) =>
        {
            _registration.Username = txtUsername.Text;
            _registration.Email = txtEmail.Text;
            _registration.Password = txtPassword.Text;

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_registration, _serviceProvider);
            lblResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateEventTab()
    {
        var tab = new TabPage("Event");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "IValidatableObject + [ReservedTitleCheck], [DateRange], [AsyncScheduleCheck]", AutoSize = true });

        var txtTitle = CreateField(panel, "Title:", _event.Title ?? "");
        var txtStart = CreateField(panel, "Start Date:", _event.StartDate?.ToString("yyyy-MM-dd") ?? "");
        var txtEnd = CreateField(panel, "End Date:", _event.EndDate?.ToString("yyyy-MM-dd") ?? "");
        var txtDelay = CreateField(panel, "Delay (ms):", _event.Delay?.ToString() ?? "3000");

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed };
        btnValidate.Click += async (s, e) =>
        {
            _event.Title = txtTitle.Text;
            if (DateTime.TryParse(txtStart.Text, out var startDate))
            {
                _event.StartDate = startDate;
            }

            if (DateTime.TryParse(txtEnd.Text, out var endDate))
            {
                _event.EndDate = endDate;
            }

            if (int.TryParse(txtDelay.Text, out int delay))
            {
                _event.Delay = delay;
            }

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_event);
            lblResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateOrderTab()
    {
        var tab = new TabPage("Order");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "IAsyncValidatableObject + [AsyncProductExists], [MaxOrderValue], [AsyncInventoryCheck]", AutoSize = true });

        var txtProduct = CreateField(panel, "Product:", _order.ProductName ?? "");
        var txtQuantity = CreateField(panel, "Quantity:", _order.Quantity.ToString());
        var txtPrice = CreateField(panel, "Unit Price:", _order.UnitPrice.ToString());
        var txtDelay = CreateField(panel, "Delay (ms):", _order.Delay?.ToString() ?? "3000");

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed };
        btnValidate.Click += async (s, e) =>
        {
            _order.ProductName = txtProduct.Text;
            if (int.TryParse(txtQuantity.Text, out int quantity))
            {
                _order.Quantity = quantity;
            }

            if (decimal.TryParse(txtPrice.Text, out decimal price))
            {
                _order.UnitPrice = price;
            }

            if (int.TryParse(txtDelay.Text, out int delay))
            {
                _order.Delay = delay;
            }

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_order);
            lblResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private static TextBox CreateField(FlowLayoutPanel panel, string label, string defaultValue)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        var txt = new TextBox { Text = defaultValue, Width = 400 };
        panel.Controls.Add(txt);
        return txt;
    }
}
