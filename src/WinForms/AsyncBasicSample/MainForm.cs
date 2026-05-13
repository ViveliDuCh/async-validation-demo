// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

namespace AsyncBasicSample;

/// <summary>
/// WinForms async DataAnnotations validation with ErrorProvider.
/// Demonstrates the four API proposal scenarios.
/// </summary>
public partial class MainForm : Form
{
    private readonly ErrorProvider _errorProvider = new();
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

    private TabControl _tabControl = null!;

    public MainForm()
    {
        Text = "WinForms AsyncBasicSample — 4 API Proposal Scenarios";
        Size = new System.Drawing.Size(700, 520);
        _errorProvider.ContainerControl = this;

        _tabControl = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(_tabControl);

        _tabControl.TabPages.Add(CreateUserTab());
        _tabControl.TabPages.Add(CreateOrderTab());
        _tabControl.TabPages.Add(CreateMoneyTransferTab());
        _tabControl.TabPages.Add(CreateEventTab());
    }

    private TabPage CreateUserTab()
    {
        var tab = new TabPage("Scenario 1 (User)");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };

        panel.Controls.Add(new Label { Text = "No interface — sync [IsValidName] + async [UsernameAvailableAsync].", AutoSize = true });

        var txtName = CreateField(panel, "Name:", _user.Name ?? string.Empty);
        txtName.Validating += async (_, _) =>
        {
            _user.Name = txtName.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtName,
                _user,
                nameof(User.Name),
                txtName.Text);
        };

        var txtUsername = CreateField(panel, "Username:", _user.Username ?? string.Empty);
        txtUsername.Validating += async (_, _) =>
        {
            _user.Username = txtUsername.Text;
            await ValidationHelper.ValidatePropertyAsync(
                _errorProvider,
                txtUsername,
                _user,
                nameof(User.Username),
                txtUsername.Text);
        };

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            _user.Name = txtName.Text;
            _user.Username = txtUsername.Text;

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_user);
            lblResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
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

        panel.Controls.Add(new Label { Text = "IValidatableObject — async [AsyncProductExists]/[AsyncInventoryCheck] + sync Validate().", AutoSize = true });

        var txtProduct = CreateField(panel, "Product Name:", _order.ProductName ?? string.Empty);
        var txtQuantity = CreateField(panel, "Quantity:", _order.Quantity.ToString());
        var txtPrice = CreateField(panel, "Unit Price:", _order.UnitPrice.ToString());
        var txtDelay = CreateField(panel, "Delay (ms):", _order.Delay?.ToString() ?? string.Empty);

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            _order.ProductName = txtProduct.Text;
            _order.Quantity = int.TryParse(txtQuantity.Text, out var quantity) ? quantity : 0;
            _order.UnitPrice = decimal.TryParse(txtPrice.Text, out var price) ? price : 0m;
            _order.Delay = int.TryParse(txtDelay.Text, out var delay) ? delay : null;

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_order);
            lblResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
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

        var txtFrom = CreateField(panel, "From Account:", _moneyTransfer.FromAccount ?? string.Empty);
        var txtTo = CreateField(panel, "To Account:", _moneyTransfer.ToAccount ?? string.Empty);
        var txtAmount = CreateField(panel, "Amount:", _moneyTransfer.Amount.ToString());

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            _moneyTransfer.FromAccount = txtFrom.Text;
            _moneyTransfer.ToAccount = txtTo.Text;
            _moneyTransfer.Amount = decimal.TryParse(txtAmount.Text, out var amount) ? amount : 0m;

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_moneyTransfer);
            lblResult.Text = valid ? "✅ Valid!" : "❌ " + string.Join("; ", results.Select(r => r.ErrorMessage));
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

        panel.Controls.Add(new Label { Text = "Async [AsyncDateRangeValid] + sync IValidatableObject rules.", AutoSize = true });

        var txtTitle = CreateField(panel, "Title:", _event.Title ?? string.Empty);
        var dtpStart = CreateDateField(panel, "Start Date:", _event.StartDate ?? DateTime.Today);
        var dtpEnd = CreateDateField(panel, "End Date:", _event.EndDate ?? DateTime.Today);

        var btnValidate = new Button { Text = "Validate All (Async)", AutoSize = true };
        var lblResult = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DarkRed, MaximumSize = new System.Drawing.Size(600, 0) };
        btnValidate.Click += async (_, _) =>
        {
            _event.Title = txtTitle.Text;
            _event.StartDate = dtpStart.Value;
            _event.EndDate = dtpEnd.Value;

            var (valid, results) = await ValidationHelper.ValidateObjectAsync(_event);
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
