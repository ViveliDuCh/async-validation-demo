// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

namespace AsyncManualSample.ViewModels;

public class OrderViewModel : ValidatableViewModelBase
{
    private readonly Order _order = new()
    {
        ProductName = "Gadget",
        Quantity = 250,
        UnitPrice = 250m,
        Delay = 100
    };

    public string? ProductName
    {
        get => _order.ProductName;
        set => SetAndValidateAsync(_order.ProductName, value, productName => _order.ProductName = productName);
    }

    public int Quantity
    {
        get => _order.Quantity;
        set { _order.Quantity = value; OnPropertyChanged(); }
    }

    public decimal UnitPrice
    {
        get => _order.UnitPrice;
        set { _order.UnitPrice = value; OnPropertyChanged(); }
    }

    public int? Delay
    {
        get => _order.Delay;
        set { _order.Delay = value; OnPropertyChanged(); }
    }

    protected override object GetValidationTarget() => _order;
}
