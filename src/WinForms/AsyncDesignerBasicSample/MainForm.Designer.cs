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
        _tabRegistrationPage = new TabPage();
        _tabEventPage = new TabPage();
        _tabOrderPage = new TabPage();

        lblRegistrationHeader = new Label();
        lblRegistrationUsername = new Label();
        txtRegistrationUsername = new TextBox();
        lblRegistrationEmail = new Label();
        txtRegistrationEmail = new TextBox();
        lblRegistrationPassword = new Label();
        txtRegistrationPassword = new TextBox();
        btnValidateRegistration = new Button();
        lblRegistrationResult = new Label();

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

        ((System.ComponentModel.ISupportInitialize)_errorProvider).BeginInit();
        SuspendLayout();

        _errorProvider.ContainerControl = this;

        _tabControl.Dock = DockStyle.Fill;
        _tabControl.TabPages.AddRange(new TabPage[] { _tabRegistrationPage, _tabEventPage, _tabOrderPage });

        _tabRegistrationPage.Text = "UserRegistration";
        _tabRegistrationPage.Padding = new Padding(10);

        lblRegistrationHeader.Text = "DI + Async Duplicate Detection (UniqueUsername + UniqueEmail)";
        lblRegistrationHeader.AutoSize = true;
        lblRegistrationHeader.Location = new System.Drawing.Point(15, 15);
        lblRegistrationHeader.Font = new System.Drawing.Font(lblRegistrationHeader.Font, System.Drawing.FontStyle.Bold);

        lblRegistrationUsername.Text = "Username:";
        lblRegistrationUsername.AutoSize = true;
        lblRegistrationUsername.Location = new System.Drawing.Point(15, 45);
        txtRegistrationUsername.Text = "admin";
        txtRegistrationUsername.Width = 400;
        txtRegistrationUsername.Location = new System.Drawing.Point(15, 65);

        lblRegistrationEmail.Text = "Email:";
        lblRegistrationEmail.AutoSize = true;
        lblRegistrationEmail.Location = new System.Drawing.Point(15, 95);
        txtRegistrationEmail.Text = "admin@example.com";
        txtRegistrationEmail.Width = 400;
        txtRegistrationEmail.Location = new System.Drawing.Point(15, 115);

        lblRegistrationPassword.Text = "Password:";
        lblRegistrationPassword.AutoSize = true;
        lblRegistrationPassword.Location = new System.Drawing.Point(15, 145);
        txtRegistrationPassword.Text = "SecureP@ss123";
        txtRegistrationPassword.Width = 400;
        txtRegistrationPassword.Location = new System.Drawing.Point(15, 165);

        btnValidateRegistration.Text = "Validate All (Async)";
        btnValidateRegistration.AutoSize = true;
        btnValidateRegistration.Location = new System.Drawing.Point(15, 200);
        btnValidateRegistration.Click += BtnValidateRegistration_Click;

        lblRegistrationResult.AutoSize = true;
        lblRegistrationResult.ForeColor = System.Drawing.Color.DarkRed;
        lblRegistrationResult.Location = new System.Drawing.Point(15, 235);
        lblRegistrationResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabRegistrationPage.Controls.AddRange(new Control[]
        {
            lblRegistrationHeader, lblRegistrationUsername, txtRegistrationUsername, lblRegistrationEmail, txtRegistrationEmail,
            lblRegistrationPassword, txtRegistrationPassword, btnValidateRegistration, lblRegistrationResult
        });

        _tabEventPage.Text = "Event";
        _tabEventPage.Padding = new Padding(10);

        lblEventHeader.Text = "IValidatableObject + [ReservedTitleCheck], [DateRange], [AsyncScheduleCheck]";
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
        txtEventDelay.Width = 100;
        txtEventDelay.Location = new System.Drawing.Point(15, 215);

        btnValidateEvent.Text = "Validate All (Async)";
        btnValidateEvent.AutoSize = true;
        btnValidateEvent.Location = new System.Drawing.Point(15, 250);
        btnValidateEvent.Click += BtnValidateEvent_Click;

        lblEventResult.AutoSize = true;
        lblEventResult.ForeColor = System.Drawing.Color.DarkRed;
        lblEventResult.Location = new System.Drawing.Point(15, 285);
        lblEventResult.MaximumSize = new System.Drawing.Size(550, 0);

        _tabEventPage.Controls.AddRange(new Control[]
        {
            lblEventHeader, lblEventTitle, txtEventTitle, lblEventStart, dtpEventStart,
            lblEventEnd, dtpEventEnd, lblEventDelay, txtEventDelay, btnValidateEvent, lblEventResult
        });

        _tabOrderPage.Text = "Order";
        _tabOrderPage.Padding = new Padding(10);

        lblOrderHeader.Text = "IAsyncValidatableObject + [AsyncProductExists], [MaxOrderValue], [AsyncInventoryCheck]";
        lblOrderHeader.AutoSize = true;
        lblOrderHeader.Location = new System.Drawing.Point(15, 15);
        lblOrderHeader.Font = new System.Drawing.Font(lblOrderHeader.Font, System.Drawing.FontStyle.Bold);

        lblOrderProduct.Text = "Product:";
        lblOrderProduct.AutoSize = true;
        lblOrderProduct.Location = new System.Drawing.Point(15, 45);
        txtOrderProduct.Text = "Widget";
        txtOrderProduct.Width = 400;
        txtOrderProduct.Location = new System.Drawing.Point(15, 65);

        lblOrderQuantity.Text = "Quantity:";
        lblOrderQuantity.AutoSize = true;
        lblOrderQuantity.Location = new System.Drawing.Point(15, 95);
        txtOrderQuantity.Text = "10000";
        txtOrderQuantity.Width = 100;
        txtOrderQuantity.Location = new System.Drawing.Point(15, 115);

        lblOrderPrice.Text = "Unit Price:";
        lblOrderPrice.AutoSize = true;
        lblOrderPrice.Location = new System.Drawing.Point(15, 145);
        txtOrderPrice.Text = "10";
        txtOrderPrice.Width = 100;
        txtOrderPrice.Location = new System.Drawing.Point(15, 165);

        lblOrderDelay.Text = "Delay (ms):";
        lblOrderDelay.AutoSize = true;
        lblOrderDelay.Location = new System.Drawing.Point(15, 195);
        txtOrderDelay.Text = "3000";
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

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(650, 500);
        Controls.Add(_tabControl);
        Text = "WinForms AsyncDesignerBasicSample — Designer + Async Validator";

        ((System.ComponentModel.ISupportInitialize)_errorProvider).EndInit();
        ResumeLayout(false);
    }

    private ErrorProvider _errorProvider;
    private TabControl _tabControl;
    private TabPage _tabRegistrationPage;
    private TabPage _tabEventPage;
    private TabPage _tabOrderPage;

    private Label lblRegistrationHeader;
    private Label lblRegistrationUsername;
    private TextBox txtRegistrationUsername;
    private Label lblRegistrationEmail;
    private TextBox txtRegistrationEmail;
    private Label lblRegistrationPassword;
    private TextBox txtRegistrationPassword;
    private Button btnValidateRegistration;
    private Label lblRegistrationResult;

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
}
