// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AsyncDesignerValidationDemo;

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
        _tabControl = new TabControl();
        _tabRegistration = new TabPage();
        _tabOrder = new TabPage();
        _tabEvent = new TabPage();
        _tabErrorHandling = new TabPage();
        _tabTwoPhase = new TabPage();

        lblRegHeader = new Label();
        lblRegUsername = new Label();
        txtRegUsername = new TextBox();
        lblRegEmail = new Label();
        txtRegEmail = new TextBox();
        lblRegPassword = new Label();
        txtRegPassword = new TextBox();
        btnValidateReg = new Button();
        lblRegResult = new Label();

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

        lblEventHeader = new Label();
        lblEventTitle = new Label();
        txtEventTitle = new TextBox();
        lblEventStart = new Label();
        dtpEventStart = new DateTimePicker();
        lblEventEnd = new Label();
        dtpEventEnd = new DateTimePicker();
        lblEventDelay = new Label();
        txtEventDelay = new TextBox();
        btnValidateEvent = new Button();
        lblEventResult = new Label();

        lblErrorHeader = new Label();
        lblErrorUsername = new Label();
        txtErrorUsername = new TextBox();
        lblErrorEmail = new Label();
        txtErrorEmail = new TextBox();
        lblErrorPassword = new Label();
        txtErrorPassword = new TextBox();
        btnValidateError = new Button();
        lblErrorResult = new Label();

        lblTwoPhaseHeader = new Label();
        lblTwoPhaseUsername = new Label();
        txtTwoPhaseUsername = new TextBox();
        lblTwoPhaseEmail = new Label();
        txtTwoPhaseEmail = new TextBox();
        lblTwoPhasePassword = new Label();
        txtTwoPhasePassword = new TextBox();
        btnValidateTwoPhase = new Button();
        lblTwoPhaseResult = new Label();

        SuspendLayout();

        _tabControl.Dock = DockStyle.Fill;
        _tabControl.TabPages.AddRange(new TabPage[] { _tabRegistration, _tabOrder, _tabEvent, _tabErrorHandling, _tabTwoPhase });

        _tabRegistration.Text = "Registration";
        _tabRegistration.Padding = new Padding(10);

        lblRegHeader.Text = "DI + Async Duplicate Detection (UniqueUsername + UniqueEmail)";
        lblRegHeader.AutoSize = true;
        lblRegHeader.Location = new System.Drawing.Point(15, 15);
        lblRegHeader.Font = new System.Drawing.Font(lblRegHeader.Font, System.Drawing.FontStyle.Bold);

        lblRegUsername.Text = "Username:";
        lblRegUsername.AutoSize = true;
        lblRegUsername.Location = new System.Drawing.Point(15, 45);
        txtRegUsername.Text = "admin";
        txtRegUsername.Width = 400;
        txtRegUsername.Location = new System.Drawing.Point(15, 65);

        lblRegEmail.Text = "Email:";
        lblRegEmail.AutoSize = true;
        lblRegEmail.Location = new System.Drawing.Point(15, 95);
        txtRegEmail.Text = "admin@example.com";
        txtRegEmail.Width = 400;
        txtRegEmail.Location = new System.Drawing.Point(15, 115);

        lblRegPassword.Text = "Password:";
        lblRegPassword.AutoSize = true;
        lblRegPassword.Location = new System.Drawing.Point(15, 145);
        txtRegPassword.Text = "SecureP@ss123";
        txtRegPassword.Width = 400;
        txtRegPassword.Location = new System.Drawing.Point(15, 165);

        btnValidateReg.Text = "Validate (Async)";
        btnValidateReg.AutoSize = true;
        btnValidateReg.Location = new System.Drawing.Point(15, 200);
        btnValidateReg.Click += BtnValidateReg_Click;

        lblRegResult.AutoSize = true;
        lblRegResult.ForeColor = System.Drawing.Color.DarkRed;
        lblRegResult.Location = new System.Drawing.Point(15, 235);
        lblRegResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabRegistration.Controls.AddRange(new Control[]
        {
            lblRegHeader, lblRegUsername, txtRegUsername, lblRegEmail, txtRegEmail,
            lblRegPassword, txtRegPassword, btnValidateReg, lblRegResult
        });

        _tabOrder.Text = "Order";
        _tabOrder.Padding = new Padding(10);

        lblOrderHeader.Text = "IAsyncValidatableObject — Order ([AsyncProductExists], [MaxOrderValue], [AsyncInventoryCheck])";
        lblOrderHeader.AutoSize = true;
        lblOrderHeader.Location = new System.Drawing.Point(15, 15);
        lblOrderHeader.Font = new System.Drawing.Font(lblOrderHeader.Font, System.Drawing.FontStyle.Bold);

        lblOrderProduct.Text = "Product:";
        lblOrderProduct.AutoSize = true;
        lblOrderProduct.Location = new System.Drawing.Point(15, 45);
        txtOrderProduct.Text = "Widget";
        txtOrderProduct.Width = 300;
        txtOrderProduct.Location = new System.Drawing.Point(15, 65);

        lblOrderQuantity.Text = "Quantity:";
        lblOrderQuantity.AutoSize = true;
        lblOrderQuantity.Location = new System.Drawing.Point(15, 95);
        txtOrderQuantity.Text = "10000";
        txtOrderQuantity.Width = 150;
        txtOrderQuantity.Location = new System.Drawing.Point(15, 115);

        lblOrderPrice.Text = "Unit Price:";
        lblOrderPrice.AutoSize = true;
        lblOrderPrice.Location = new System.Drawing.Point(15, 145);
        txtOrderPrice.Text = "10";
        txtOrderPrice.Width = 150;
        txtOrderPrice.Location = new System.Drawing.Point(15, 165);

        lblOrderDelay.Text = "Delay (ms):";
        lblOrderDelay.AutoSize = true;
        lblOrderDelay.Location = new System.Drawing.Point(15, 195);
        txtOrderDelay.Text = "3000";
        txtOrderDelay.Width = 150;
        txtOrderDelay.Location = new System.Drawing.Point(15, 215);

        btnValidateOrder.Text = "Validate (Async)";
        btnValidateOrder.AutoSize = true;
        btnValidateOrder.Location = new System.Drawing.Point(15, 250);
        btnValidateOrder.Click += BtnValidateOrder_Click;

        lblOrderResult.AutoSize = true;
        lblOrderResult.ForeColor = System.Drawing.Color.DarkRed;
        lblOrderResult.Location = new System.Drawing.Point(15, 285);
        lblOrderResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabOrder.Controls.AddRange(new Control[]
        {
            lblOrderHeader, lblOrderProduct, txtOrderProduct, lblOrderQuantity, txtOrderQuantity,
            lblOrderPrice, txtOrderPrice, lblOrderDelay, txtOrderDelay, btnValidateOrder, lblOrderResult
        });

        _tabEvent.Text = "Event";
        _tabEvent.Padding = new Padding(10);

        lblEventHeader.Text = "IValidatableObject — Event ([ReservedTitleCheck], [DateRange], [AsyncScheduleCheck])";
        lblEventHeader.AutoSize = true;
        lblEventHeader.Location = new System.Drawing.Point(15, 15);
        lblEventHeader.Font = new System.Drawing.Font(lblEventHeader.Font, System.Drawing.FontStyle.Bold);

        lblEventTitle.Text = "Title:";
        lblEventTitle.AutoSize = true;
        lblEventTitle.Location = new System.Drawing.Point(15, 45);
        txtEventTitle.Text = "Launch Party";
        txtEventTitle.Width = 400;
        txtEventTitle.Location = new System.Drawing.Point(15, 65);

        lblEventStart.Text = "Start Date:";
        lblEventStart.AutoSize = true;
        lblEventStart.Location = new System.Drawing.Point(15, 95);
        dtpEventStart.Value = new DateTime(2026, 12, 25);
        dtpEventStart.Width = 200;
        dtpEventStart.Location = new System.Drawing.Point(15, 115);

        lblEventEnd.Text = "End Date:";
        lblEventEnd.AutoSize = true;
        lblEventEnd.Location = new System.Drawing.Point(15, 145);
        dtpEventEnd.Value = new DateTime(2026, 12, 20);
        dtpEventEnd.Width = 200;
        dtpEventEnd.Location = new System.Drawing.Point(15, 165);

        lblEventDelay.Text = "Delay (ms):";
        lblEventDelay.AutoSize = true;
        lblEventDelay.Location = new System.Drawing.Point(15, 195);
        txtEventDelay.Text = "3000";
        txtEventDelay.Width = 150;
        txtEventDelay.Location = new System.Drawing.Point(15, 215);

        btnValidateEvent.Text = "Validate (Async)";
        btnValidateEvent.AutoSize = true;
        btnValidateEvent.Location = new System.Drawing.Point(15, 250);
        btnValidateEvent.Click += BtnValidateEvent_Click;

        lblEventResult.AutoSize = true;
        lblEventResult.ForeColor = System.Drawing.Color.DarkRed;
        lblEventResult.Location = new System.Drawing.Point(15, 285);
        lblEventResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabEvent.Controls.AddRange(new Control[]
        {
            lblEventHeader, lblEventTitle, txtEventTitle, lblEventStart, dtpEventStart,
            lblEventEnd, dtpEventEnd, lblEventDelay, txtEventDelay, btnValidateEvent, lblEventResult
        });

        _tabErrorHandling.Text = "Error Handling";
        _tabErrorHandling.Padding = new Padding(10);

        lblErrorHeader.Text = "Infrastructure Failure — 'error-trigger' throws exception";
        lblErrorHeader.AutoSize = true;
        lblErrorHeader.Location = new System.Drawing.Point(15, 15);
        lblErrorHeader.Font = new System.Drawing.Font(lblErrorHeader.Font, System.Drawing.FontStyle.Bold);

        lblErrorUsername.Text = "Username:";
        lblErrorUsername.AutoSize = true;
        lblErrorUsername.Location = new System.Drawing.Point(15, 45);
        txtErrorUsername.Text = "error-trigger";
        txtErrorUsername.Width = 400;
        txtErrorUsername.Location = new System.Drawing.Point(15, 65);

        lblErrorEmail.Text = "Email:";
        lblErrorEmail.AutoSize = true;
        lblErrorEmail.Location = new System.Drawing.Point(15, 95);
        txtErrorEmail.Text = "new@example.com";
        txtErrorEmail.Width = 400;
        txtErrorEmail.Location = new System.Drawing.Point(15, 115);

        lblErrorPassword.Text = "Password:";
        lblErrorPassword.AutoSize = true;
        lblErrorPassword.Location = new System.Drawing.Point(15, 145);
        txtErrorPassword.Text = "SecureP@ss123";
        txtErrorPassword.Width = 400;
        txtErrorPassword.Location = new System.Drawing.Point(15, 165);

        btnValidateError.Text = "Validate (Async)";
        btnValidateError.AutoSize = true;
        btnValidateError.Location = new System.Drawing.Point(15, 200);
        btnValidateError.Click += BtnValidateError_Click;

        lblErrorResult.AutoSize = true;
        lblErrorResult.ForeColor = System.Drawing.Color.DarkRed;
        lblErrorResult.Location = new System.Drawing.Point(15, 235);
        lblErrorResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabErrorHandling.Controls.AddRange(new Control[]
        {
            lblErrorHeader, lblErrorUsername, txtErrorUsername, lblErrorEmail, txtErrorEmail,
            lblErrorPassword, txtErrorPassword, btnValidateError, lblErrorResult
        });

        _tabTwoPhase.Text = "Two-Phase";
        _tabTwoPhase.Padding = new Padding(10);

        lblTwoPhaseHeader.Text = "Two-Phase: sync [EmailAddress] fails → async [UniqueEmail] skipped";
        lblTwoPhaseHeader.AutoSize = true;
        lblTwoPhaseHeader.Location = new System.Drawing.Point(15, 15);
        lblTwoPhaseHeader.Font = new System.Drawing.Font(lblTwoPhaseHeader.Font, System.Drawing.FontStyle.Bold);

        lblTwoPhaseUsername.Text = "Username:";
        lblTwoPhaseUsername.AutoSize = true;
        lblTwoPhaseUsername.Location = new System.Drawing.Point(15, 45);
        txtTwoPhaseUsername.Text = "newuser";
        txtTwoPhaseUsername.Width = 400;
        txtTwoPhaseUsername.Location = new System.Drawing.Point(15, 65);

        lblTwoPhaseEmail.Text = "Email:";
        lblTwoPhaseEmail.AutoSize = true;
        lblTwoPhaseEmail.Location = new System.Drawing.Point(15, 95);
        txtTwoPhaseEmail.Text = "not-an-email";
        txtTwoPhaseEmail.Width = 400;
        txtTwoPhaseEmail.Location = new System.Drawing.Point(15, 115);

        lblTwoPhasePassword.Text = "Password:";
        lblTwoPhasePassword.AutoSize = true;
        lblTwoPhasePassword.Location = new System.Drawing.Point(15, 145);
        txtTwoPhasePassword.Text = "SecureP@ss123";
        txtTwoPhasePassword.Width = 400;
        txtTwoPhasePassword.Location = new System.Drawing.Point(15, 165);

        btnValidateTwoPhase.Text = "Validate (Async)";
        btnValidateTwoPhase.AutoSize = true;
        btnValidateTwoPhase.Location = new System.Drawing.Point(15, 200);
        btnValidateTwoPhase.Click += BtnValidateTwoPhase_Click;

        lblTwoPhaseResult.AutoSize = true;
        lblTwoPhaseResult.ForeColor = System.Drawing.Color.DarkRed;
        lblTwoPhaseResult.Location = new System.Drawing.Point(15, 235);
        lblTwoPhaseResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabTwoPhase.Controls.AddRange(new Control[]
        {
            lblTwoPhaseHeader, lblTwoPhaseUsername, txtTwoPhaseUsername, lblTwoPhaseEmail, txtTwoPhaseEmail,
            lblTwoPhasePassword, txtTwoPhasePassword, btnValidateTwoPhase, lblTwoPhaseResult
        });

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(700, 550);
        Controls.Add(_tabControl);
        Text = "WinForms AsyncDesignerValidationDemo — Designer + DI + Async";

        ResumeLayout(false);
    }

    private TabControl _tabControl;
    private TabPage _tabRegistration;
    private TabPage _tabOrder;
    private TabPage _tabEvent;
    private TabPage _tabErrorHandling;
    private TabPage _tabTwoPhase;

    private Label lblRegHeader;
    private Label lblRegUsername;
    private TextBox txtRegUsername;
    private Label lblRegEmail;
    private TextBox txtRegEmail;
    private Label lblRegPassword;
    private TextBox txtRegPassword;
    private Button btnValidateReg;
    private Label lblRegResult;

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

    private Label lblEventHeader;
    private Label lblEventTitle;
    private TextBox txtEventTitle;
    private Label lblEventStart;
    private DateTimePicker dtpEventStart;
    private Label lblEventEnd;
    private DateTimePicker dtpEventEnd;
    private Label lblEventDelay;
    private TextBox txtEventDelay;
    private Button btnValidateEvent;
    private Label lblEventResult;

    private Label lblErrorHeader;
    private Label lblErrorUsername;
    private TextBox txtErrorUsername;
    private Label lblErrorEmail;
    private TextBox txtErrorEmail;
    private Label lblErrorPassword;
    private TextBox txtErrorPassword;
    private Button btnValidateError;
    private Label lblErrorResult;

    private Label lblTwoPhaseHeader;
    private Label lblTwoPhaseUsername;
    private TextBox txtTwoPhaseUsername;
    private Label lblTwoPhaseEmail;
    private TextBox txtTwoPhaseEmail;
    private Label lblTwoPhasePassword;
    private TextBox txtTwoPhasePassword;
    private Button btnValidateTwoPhase;
    private Label lblTwoPhaseResult;
}
