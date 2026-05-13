// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

namespace AsyncManualSample.ViewModels;

public class MoneyTransferViewModel : ValidatableViewModelBase
{
    private readonly MoneyTransfer _moneyTransfer = new()
    {
        FromAccount = "checking",
        ToAccount = "checking",
        Amount = 1000m
    };

    public string? FromAccount
    {
        get => _moneyTransfer.FromAccount;
        set => SetAndValidateAsync(_moneyTransfer.FromAccount, value, fromAccount => _moneyTransfer.FromAccount = fromAccount);
    }

    public string? ToAccount
    {
        get => _moneyTransfer.ToAccount;
        set => SetAndValidateAsync(_moneyTransfer.ToAccount, value, toAccount => _moneyTransfer.ToAccount = toAccount);
    }

    public decimal Amount
    {
        get => _moneyTransfer.Amount;
        set { _moneyTransfer.Amount = value; OnPropertyChanged(); }
    }

    protected override object GetValidationTarget() => _moneyTransfer;
}
