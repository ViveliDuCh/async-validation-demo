// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;

namespace AsyncValidationDemo;

/// <summary>
/// WinForms async validation demo with DI, event/order validation,
/// two-phase validation, and error handling.
/// </summary>
public partial class MainForm : Form
{
    private readonly SimpleServiceProvider _serviceProvider;
    private readonly ErrorProvider _errorProvider = new();

    public MainForm()
    {
        Text = "WinForms AsyncValidationDemo — DI + Async Validation";
        Size = new System.Drawing.Size(700, 550);
        _errorProvider.ContainerControl = this;

        _serviceProvider = new SimpleServiceProvider()
            .Register(new UserService());

        var tabControl = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabControl);

        tabControl.TabPages.Add(CreateRegistrationTab());
        tabControl.TabPages.Add(CreateOrderTab());
        tabControl.TabPages.Add(CreateEventTab());
        tabControl.TabPages.Add(CreateErrorHandlingTab());
        tabControl.TabPages.Add(CreateTwoPhaseTab());
    }

    private TabPage CreateRegistrationTab()
    {
        var tab = new TabPage("Registration");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "DI + Async Duplicate Detection (UniqueUsername + UniqueEmail)", AutoSize = true });

        var txtUsername = CreateField(panel, "Username:", "admin");
        var txtEmail = CreateField(panel, "Email:", "admin@example.com");
        var txtPassword = CreateField(panel, "Password:", "SecureP@ss123");
        txtPassword.UseSystemPasswordChar = true;

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (s, e) =>
        {
            var registration = new UserRegistration
            {
                Username = txtUsername.Text,
                Email = txtEmail.Text,
                Password = txtPassword.Text
            };

            var context = new ValidationContext(registration, _serviceProvider, null);
            var results = new List<ValidationResult>();
            try
            {
                bool isValid = await Validator.TryValidateObjectAsync(registration, context, results, true);
                lblResult.Text = isValid
                    ? "✅ Valid!"
                    : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
            }
            catch (OperationCanceledException)
            {
                lblResult.Text = "⏹️ Validation was cancelled.";
            }
            catch (InvalidOperationException ex)
            {
                lblResult.Text = $"⚠️ Infrastructure error: {ex.Message}";
            }
            catch (Exception ex)
            {
                lblResult.Text = $"⚠️ Unexpected validation error: {ex.Message}";
            }
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

        panel.Controls.Add(new Label { Text = "IAsyncValidatableObject — Order ([AsyncProductExists], [MaxOrderValue], [AsyncInventoryCheck])", AutoSize = true });

        var txtProduct = CreateField(panel, "Product:", "Widget");
        var txtQuantity = CreateField(panel, "Quantity:", "10000");
        var txtPrice = CreateField(panel, "Unit Price:", "10");
        var txtDelay = CreateField(panel, "Delay (ms):", "3000");

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (s, e) =>
        {
            var order = new Order
            {
                ProductName = txtProduct.Text,
                Quantity = int.TryParse(txtQuantity.Text, out int quantity) ? quantity : 0,
                UnitPrice = decimal.TryParse(txtPrice.Text, out decimal unitPrice) ? unitPrice : 0m,
                Delay = int.TryParse(txtDelay.Text, out int delay) ? delay : null
            };

            var context = new ValidationContext(order);
            var results = new List<ValidationResult>();
            try
            {
                bool isValid = await Validator.TryValidateObjectAsync(order, context, results, true);
                lblResult.Text = isValid
                    ? "✅ Valid!"
                    : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
            }
            catch (OperationCanceledException)
            {
                lblResult.Text = "⏹️ Validation was cancelled.";
            }
            catch (Exception ex)
            {
                lblResult.Text = $"⚠️ Validation error: {ex.Message}";
            }
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

        panel.Controls.Add(new Label { Text = "IValidatableObject — Event ([ReservedTitleCheck], [DateRange], [AsyncScheduleCheck])", AutoSize = true });

        var txtTitle = CreateField(panel, "Title:", "Launch Party");
        var txtStart = CreateField(panel, "Start Date:", "2026-12-25");
        var txtEnd = CreateField(panel, "End Date:", "2026-12-20");
        var txtDelay = CreateField(panel, "Delay (ms):", "3000");

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (s, e) =>
        {
            var eventModel = new Event
            {
                Title = txtTitle.Text,
                StartDate = DateTime.TryParse(txtStart.Text, out var startDate) ? startDate : null,
                EndDate = DateTime.TryParse(txtEnd.Text, out var endDate) ? endDate : null,
                Delay = int.TryParse(txtDelay.Text, out int delay) ? delay : null
            };

            var context = new ValidationContext(eventModel);
            var results = new List<ValidationResult>();
            try
            {
                bool isValid = await Validator.TryValidateObjectAsync(eventModel, context, results, true);
                lblResult.Text = isValid
                    ? "✅ Valid!"
                    : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
            }
            catch (OperationCanceledException)
            {
                lblResult.Text = "⏹️ Validation was cancelled.";
            }
            catch (Exception ex)
            {
                lblResult.Text = $"⚠️ Validation error: {ex.Message}";
            }
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateErrorHandlingTab()
    {
        var tab = new TabPage("Error Handling");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "Infrastructure Failure — 'error-trigger' username throws exception", AutoSize = true });

        var txtUsername = CreateField(panel, "Username:", "error-trigger");
        var txtEmail = CreateField(panel, "Email:", "new@example.com");
        var txtPassword = CreateField(panel, "Password:", "SecureP@ss123");
        txtPassword.UseSystemPasswordChar = true;

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (s, e) =>
        {
            var registration = new UserRegistration
            {
                Username = txtUsername.Text,
                Email = txtEmail.Text,
                Password = txtPassword.Text
            };

            var context = new ValidationContext(registration, _serviceProvider, null);
            var results = new List<ValidationResult>();
            try
            {
                bool isValid = await Validator.TryValidateObjectAsync(registration, context, results, true);
                lblResult.Text = isValid
                    ? "✅ Valid!"
                    : "❌ " + string.Join("\n", results.Select(r => r.ErrorMessage));
            }
            catch (InvalidOperationException ex)
            {
                lblResult.Text = $"⚠️ Infrastructure error caught: {ex.Message}";
            }
            catch (OperationCanceledException)
            {
                lblResult.Text = "⏹️ Validation was cancelled.";
            }
            catch (Exception ex)
            {
                lblResult.Text = $"⚠️ Unexpected validation error: {ex.Message}";
            }
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateTwoPhaseTab()
    {
        var tab = new TabPage("Two-Phase");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "Two-Phase: sync [EmailAddress] fails → async [UniqueEmail] skipped", AutoSize = true });

        var txtUsername = CreateField(panel, "Username:", "newuser");
        var txtEmail = CreateField(panel, "Email:", "not-an-email");
        var txtPassword = CreateField(panel, "Password:", "SecureP@ss123");
        txtPassword.UseSystemPasswordChar = true;

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (s, e) =>
        {
            var registration = new UserRegistration
            {
                Username = txtUsername.Text,
                Email = txtEmail.Text,
                Password = txtPassword.Text
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
                lblResult.Text = message;
            }
            catch (OperationCanceledException)
            {
                lblResult.Text = "⏹️ Validation was cancelled.";
            }
            catch (InvalidOperationException ex)
            {
                lblResult.Text = $"⚠️ Infrastructure error: {ex.Message}";
            }
            catch (Exception ex)
            {
                lblResult.Text = $"⚠️ Unexpected validation error: {ex.Message}";
            }
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
