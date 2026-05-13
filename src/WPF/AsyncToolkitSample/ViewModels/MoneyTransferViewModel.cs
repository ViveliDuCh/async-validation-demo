// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using SharedModels.EntityClasses;

namespace AsyncToolkitSample.ViewModels;

public partial class MoneyTransferViewModel : ObservableValidator
{
    private readonly MoneyTransfer _moneyTransfer = new()
    {
        FromAccount = "checking",
        ToAccount = "checking",
        Amount = 1000m
    };

    [Required]
    public string? FromAccount
    {
        get => _moneyTransfer.FromAccount;
        set => SetProperty(_moneyTransfer.FromAccount, value, _moneyTransfer, static (item, fromAccount) => item.FromAccount = fromAccount, true);
    }

    [Required]
    public string? ToAccount
    {
        get => _moneyTransfer.ToAccount;
        set => SetProperty(_moneyTransfer.ToAccount, value, _moneyTransfer, static (item, toAccount) => item.ToAccount = toAccount, true);
    }

    [Range(0.01, double.MaxValue)]
    public decimal Amount
    {
        get => _moneyTransfer.Amount;
        set => SetProperty(_moneyTransfer.Amount, value, _moneyTransfer, static (item, amount) => item.Amount = amount, true);
    }

    public async Task<bool> ValidateAllAsync()
    {
        ValidateAllProperties();

        var context = new ValidationContext(_moneyTransfer);
        var results = new List<ValidationResult>();
        bool isValid = await Validator.TryValidateObjectAsync(_moneyTransfer, context, results, validateAllProperties: true);

        return isValid && !HasErrors;
    }
}
