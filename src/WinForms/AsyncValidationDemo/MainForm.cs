// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using SharedModels.EntityClasses;

namespace AsyncValidationDemo;

/// <summary>
/// WinForms async validation demo organized around the four API proposal scenarios.
/// </summary>
public partial class MainForm : Form
{
    public MainForm()
    {
        Text = "WinForms AsyncValidationDemo — 4 API Proposal Scenarios";
        Size = new System.Drawing.Size(700, 550);

        var tabControl = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabControl);

        tabControl.TabPages.Add(CreateUserTab());
        tabControl.TabPages.Add(CreateOrderTab());
        tabControl.TabPages.Add(CreateMoneyTransferTab());
        tabControl.TabPages.Add(CreateEventTab());
    }

    private TabPage CreateUserTab()
    {
        var tab = new TabPage("Scenario 1 (User)");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "No interface — sync [IsValidName] + async [UsernameAvailableAsync].", AutoSize = true });

        var txtName = CreateField(panel, "Name:", "Bob");
        var txtUsername = CreateField(panel, "Username:", "admin");

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            var user = new User
            {
                Name = txtName.Text,
                Username = txtUsername.Text
            };

            await ValidateAndShowAsync(user, new ValidationContext(user), lblResult);
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateOrderTab()
    {
        var tab = new TabPage("Scenario 2 (Order)");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "IValidatableObject — async attributes + sync Validate() business rule.", AutoSize = true });

        var txtProduct = CreateField(panel, "Product Name:", "Gadget");
        var txtQuantity = CreateField(panel, "Quantity:", "250");
        var txtPrice = CreateField(panel, "Unit Price:", "250");
        var txtDelay = CreateField(panel, "Delay (ms):", "100");

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            var order = new Order
            {
                ProductName = txtProduct.Text,
                Quantity = int.TryParse(txtQuantity.Text, out var quantity) ? quantity : 0,
                UnitPrice = decimal.TryParse(txtPrice.Text, out var unitPrice) ? unitPrice : 0m,
                Delay = int.TryParse(txtDelay.Text, out var delay) ? delay : null
            };

            await ValidateAndShowAsync(order, new ValidationContext(order), lblResult);
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateMoneyTransferTab()
    {
        var tab = new TabPage("Scenario 3 (MoneyTransfer)");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "IAsyncValidatableObject — async cross-property validation via ValidateAsync().", AutoSize = true });

        var txtFrom = CreateField(panel, "From Account:", "checking");
        var txtTo = CreateField(panel, "To Account:", "checking");
        var txtAmount = CreateField(panel, "Amount:", "1000");

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            var transfer = new MoneyTransfer
            {
                FromAccount = txtFrom.Text,
                ToAccount = txtTo.Text,
                Amount = decimal.TryParse(txtAmount.Text, out var amount) ? amount : 0m
            };

            await ValidateAndShowAsync(transfer, new ValidationContext(transfer), lblResult);
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CreateEventTab()
    {
        var tab = new TabPage("Scenario 4 (Event)");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "Async [AsyncDateRangeValid] + sync IValidatableObject validation.", AutoSize = true });

        var txtTitle = CreateField(panel, "Title:", "TBD Kickoff");
        var dtpStart = CreateDateField(panel, "Start Date:", new DateTime(2026, 6, 1));
        var dtpEnd = CreateDateField(panel, "End Date:", new DateTime(2026, 6, 2));

        var btnValidate = new Button { Text = "Validate (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            var eventModel = new Event
            {
                Title = txtTitle.Text,
                StartDate = dtpStart.Value,
                EndDate = dtpEnd.Value
            };

            await ValidateAndShowAsync(eventModel, new ValidationContext(eventModel), lblResult);
        };
        panel.Controls.Add(btnValidate);
        panel.Controls.Add(lblResult);

        tab.Controls.Add(panel);
        return tab;
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

    private static TextBox CreateField(FlowLayoutPanel panel, string label, string defaultValue)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        var txt = new TextBox { Text = defaultValue, Width = 400 };
        panel.Controls.Add(txt);
        return txt;
    }

    private static DateTimePicker CreateDateField(FlowLayoutPanel panel, string label, DateTime value)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true });
        var picker = new DateTimePicker
        {
            Width = 200,
            Format = DateTimePickerFormat.Short,
            Value = value
        };
        panel.Controls.Add(picker);
        return picker;
    }
}
