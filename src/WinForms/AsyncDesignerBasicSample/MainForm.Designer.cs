// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AsyncDesignerBasicSample;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _errorProvider = new ErrorProvider(components);
        _tabControl = new TabControl();
        _tabUserPage = new TabPage();
        _tabOrderPage = new TabPage();
        _tabMoneyTransferPage = new TabPage();
        _tabEventPage = new TabPage();

        lblUserHeader = new Label();
        lblUserName = new Label();
        txtUserName = new TextBox();
        lblUserUsername = new Label();
        txtUserUsername = new TextBox();
        btnValidateUser = new Button();
        lblUserResult = new Label();

        lblOrderHeader = new Label();
        lblOrderProduct = new Label();
        txtOrderProduct = new TextBox();
        lblOrderQuantity = new Label();
        txtOrderQuantity = new TextBox();
        lblOrderPrice = new Label();
        txtOrderPrice = new TextBox();
        lblOrderDelay = new Label();
        txtOrderDelay = new TextBox();
        btnValidateOrder = new Button();
        lblOrderResult = new Label();

        lblTransferHeader = new Label();
        lblTransferFromAccount = new Label();
        txtTransferFromAccount = new TextBox();
        lblTransferToAccount = new Label();
        txtTransferToAccount = new TextBox();
        lblTransferAmount = new Label();
        txtTransferAmount = new TextBox();
        btnValidateMoneyTransfer = new Button();
        lblTransferResult = new Label();

        lblEventHeader = new Label();
        lblEventTitle = new Label();
        txtEventTitle = new TextBox();
        lblEventStart = new Label();
        dtpEventStart = new DateTimePicker();
        lblEventEnd = new Label();
        dtpEventEnd = new DateTimePicker();
        btnValidateEvent = new Button();
        lblEventResult = new Label();

        ((System.ComponentModel.ISupportInitialize)_errorProvider).BeginInit();
        SuspendLayout();

        _errorProvider.ContainerControl = this;

        _tabControl.Dock = DockStyle.Fill;
        _tabControl.TabPages.AddRange(new TabPage[] { _tabUserPage, _tabOrderPage, _tabMoneyTransferPage, _tabEventPage });

        _tabUserPage.Text = "Scenario 1 (User)";
        _tabUserPage.Padding = new Padding(10);

        lblUserHeader.Text = "No interface — sync [IsValidName] + async [UsernameAvailableAsync].";
        lblUserHeader.AutoSize = true;
        lblUserHeader.Location = new System.Drawing.Point(15, 15);
        lblUserHeader.Font = new System.Drawing.Font(lblUserHeader.Font, System.Drawing.FontStyle.Bold);

        lblUserName.Text = "Name:";
        lblUserName.AutoSize = true;
        lblUserName.Location = new System.Drawing.Point(15, 45);
        txtUserName.Text = "Bob";
        txtUserName.Width = 400;
        txtUserName.Location = new System.Drawing.Point(15, 65);

        lblUserUsername.Text = "Username:";
        lblUserUsername.AutoSize = true;
        lblUserUsername.Location = new System.Drawing.Point(15, 95);
        txtUserUsername.Text = "admin";
        txtUserUsername.Width = 400;
        txtUserUsername.Location = new System.Drawing.Point(15, 115);

        btnValidateUser.Text = "Validate All (Async)";
        btnValidateUser.AutoSize = true;
        btnValidateUser.Location = new System.Drawing.Point(15, 150);
        btnValidateUser.Click += BtnValidateUser_Click;

        lblUserResult.AutoSize = true;
        lblUserResult.ForeColor = System.Drawing.Color.DarkRed;
        lblUserResult.Location = new System.Drawing.Point(15, 185);
        lblUserResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabUserPage.Controls.AddRange(new Control[]
        {
            lblUserHeader, lblUserName, txtUserName, lblUserUsername, txtUserUsername, btnValidateUser, lblUserResult
        });

        _tabOrderPage.Text = "Scenario 2 (Order)";
        _tabOrderPage.Padding = new Padding(10);

        lblOrderHeader.Text = "IValidatableObject — async attributes + sync Validate() business rule.";
        lblOrderHeader.AutoSize = true;
        lblOrderHeader.Location = new System.Drawing.Point(15, 15);
        lblOrderHeader.Font = new System.Drawing.Font(lblOrderHeader.Font, System.Drawing.FontStyle.Bold);

        lblOrderProduct.Text = "Product Name:";
        lblOrderProduct.AutoSize = true;
        lblOrderProduct.Location = new System.Drawing.Point(15, 45);
        txtOrderProduct.Text = "Gadget";
        txtOrderProduct.Width = 400;
        txtOrderProduct.Location = new System.Drawing.Point(15, 65);

        lblOrderQuantity.Text = "Quantity:";
        lblOrderQuantity.AutoSize = true;
        lblOrderQuantity.Location = new System.Drawing.Point(15, 95);
        txtOrderQuantity.Text = "250";
        txtOrderQuantity.Width = 100;
        txtOrderQuantity.Location = new System.Drawing.Point(15, 115);

        lblOrderPrice.Text = "Unit Price:";
        lblOrderPrice.AutoSize = true;
        lblOrderPrice.Location = new System.Drawing.Point(15, 145);
        txtOrderPrice.Text = "250";
        txtOrderPrice.Width = 100;
        txtOrderPrice.Location = new System.Drawing.Point(15, 165);

        lblOrderDelay.Text = "Delay (ms):";
        lblOrderDelay.AutoSize = true;
        lblOrderDelay.Location = new System.Drawing.Point(15, 195);
        txtOrderDelay.Text = "100";
        txtOrderDelay.Width = 100;
        txtOrderDelay.Location = new System.Drawing.Point(15, 215);

        btnValidateOrder.Text = "Validate All (Async)";
        btnValidateOrder.AutoSize = true;
        btnValidateOrder.Location = new System.Drawing.Point(15, 250);
        btnValidateOrder.Click += BtnValidateOrder_Click;

        lblOrderResult.AutoSize = true;
        lblOrderResult.ForeColor = System.Drawing.Color.DarkRed;
        lblOrderResult.Location = new System.Drawing.Point(15, 285);
        lblOrderResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabOrderPage.Controls.AddRange(new Control[]
        {
            lblOrderHeader, lblOrderProduct, txtOrderProduct, lblOrderQuantity, txtOrderQuantity,
            lblOrderPrice, txtOrderPrice, lblOrderDelay, txtOrderDelay, btnValidateOrder, lblOrderResult
        });

        _tabMoneyTransferPage.Text = "Scenario 3 (MoneyTransfer)";
        _tabMoneyTransferPage.Padding = new Padding(10);

        lblTransferHeader.Text = "IAsyncValidatableObject — async cross-property validation via ValidateAsync().";
        lblTransferHeader.AutoSize = true;
        lblTransferHeader.Location = new System.Drawing.Point(15, 15);
        lblTransferHeader.Font = new System.Drawing.Font(lblTransferHeader.Font, System.Drawing.FontStyle.Bold);

        lblTransferFromAccount.Text = "From Account:";
        lblTransferFromAccount.AutoSize = true;
        lblTransferFromAccount.Location = new System.Drawing.Point(15, 45);
        txtTransferFromAccount.Text = "checking";
        txtTransferFromAccount.Width = 400;
        txtTransferFromAccount.Location = new System.Drawing.Point(15, 65);

        lblTransferToAccount.Text = "To Account:";
        lblTransferToAccount.AutoSize = true;
        lblTransferToAccount.Location = new System.Drawing.Point(15, 95);
        txtTransferToAccount.Text = "checking";
        txtTransferToAccount.Width = 400;
        txtTransferToAccount.Location = new System.Drawing.Point(15, 115);

        lblTransferAmount.Text = "Amount:";
        lblTransferAmount.AutoSize = true;
        lblTransferAmount.Location = new System.Drawing.Point(15, 145);
        txtTransferAmount.Text = "1000";
        txtTransferAmount.Width = 150;
        txtTransferAmount.Location = new System.Drawing.Point(15, 165);

        btnValidateMoneyTransfer.Text = "Validate All (Async)";
        btnValidateMoneyTransfer.AutoSize = true;
        btnValidateMoneyTransfer.Location = new System.Drawing.Point(15, 200);
        btnValidateMoneyTransfer.Click += BtnValidateMoneyTransfer_Click;

        lblTransferResult.AutoSize = true;
        lblTransferResult.ForeColor = System.Drawing.Color.DarkRed;
        lblTransferResult.Location = new System.Drawing.Point(15, 235);
        lblTransferResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabMoneyTransferPage.Controls.AddRange(new Control[]
        {
            lblTransferHeader, lblTransferFromAccount, txtTransferFromAccount, lblTransferToAccount, txtTransferToAccount,
            lblTransferAmount, txtTransferAmount, btnValidateMoneyTransfer, lblTransferResult
        });

        _tabEventPage.Text = "Scenario 4 (Event)";
        _tabEventPage.Padding = new Padding(10);

        lblEventHeader.Text = "Async [AsyncDateRangeValid] + sync IValidatableObject rules.";
        lblEventHeader.AutoSize = true;
        lblEventHeader.Location = new System.Drawing.Point(15, 15);
        lblEventHeader.Font = new System.Drawing.Font(lblEventHeader.Font, System.Drawing.FontStyle.Bold);

        lblEventTitle.Text = "Title:";
        lblEventTitle.AutoSize = true;
        lblEventTitle.Location = new System.Drawing.Point(15, 45);
        txtEventTitle.Text = "TBD Kickoff";
        txtEventTitle.Width = 400;
        txtEventTitle.Location = new System.Drawing.Point(15, 65);

        lblEventStart.Text = "Start Date:";
        lblEventStart.AutoSize = true;
        lblEventStart.Location = new System.Drawing.Point(15, 95);
        dtpEventStart.Format = DateTimePickerFormat.Short;
        dtpEventStart.Value = new DateTime(2026, 6, 1);
        dtpEventStart.Width = 200;
        dtpEventStart.Location = new System.Drawing.Point(15, 115);

        lblEventEnd.Text = "End Date:";
        lblEventEnd.AutoSize = true;
        lblEventEnd.Location = new System.Drawing.Point(15, 145);
        dtpEventEnd.Format = DateTimePickerFormat.Short;
        dtpEventEnd.Value = new DateTime(2026, 6, 2);
        dtpEventEnd.Width = 200;
        dtpEventEnd.Location = new System.Drawing.Point(15, 165);

        btnValidateEvent.Text = "Validate All (Async)";
        btnValidateEvent.AutoSize = true;
        btnValidateEvent.Location = new System.Drawing.Point(15, 200);
        btnValidateEvent.Click += BtnValidateEvent_Click;

        lblEventResult.AutoSize = true;
        lblEventResult.ForeColor = System.Drawing.Color.DarkRed;
        lblEventResult.Location = new System.Drawing.Point(15, 235);
        lblEventResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabEventPage.Controls.AddRange(new Control[]
        {
            lblEventHeader, lblEventTitle, txtEventTitle, lblEventStart, dtpEventStart,
            lblEventEnd, dtpEventEnd, btnValidateEvent, lblEventResult
        });

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(700, 520);
        Controls.Add(_tabControl);
        Text = "WinForms AsyncDesignerBasicSample — 4 API Proposal Scenarios";

        ((System.ComponentModel.ISupportInitialize)_errorProvider).EndInit();
        ResumeLayout(false);
    }

    private ErrorProvider _errorProvider;
    private TabControl _tabControl;
    private TabPage _tabUserPage;
    private TabPage _tabOrderPage;
    private TabPage _tabMoneyTransferPage;
    private TabPage _tabEventPage;

    private Label lblUserHeader;
    private Label lblUserName;
    private TextBox txtUserName;
    private Label lblUserUsername;
    private TextBox txtUserUsername;
    private Button btnValidateUser;
    private Label lblUserResult;

    private Label lblOrderHeader;
    private Label lblOrderProduct;
    private TextBox txtOrderProduct;
    private Label lblOrderQuantity;
    private TextBox txtOrderQuantity;
    private Label lblOrderPrice;
    private TextBox txtOrderPrice;
    private Label lblOrderDelay;
    private TextBox txtOrderDelay;
    private Button btnValidateOrder;
    private Label lblOrderResult;

    private Label lblTransferHeader;
    private Label lblTransferFromAccount;
    private TextBox txtTransferFromAccount;
    private Label lblTransferToAccount;
    private TextBox txtTransferToAccount;
    private Label lblTransferAmount;
    private TextBox txtTransferAmount;
    private Button btnValidateMoneyTransfer;
    private Label lblTransferResult;

    private Label lblEventHeader;
    private Label lblEventTitle;
    private TextBox txtEventTitle;
    private Label lblEventStart;
    private DateTimePicker dtpEventStart;
    private Label lblEventEnd;
    private DateTimePicker dtpEventEnd;
    private Button btnValidateEvent;
    private Label lblEventResult;
}
