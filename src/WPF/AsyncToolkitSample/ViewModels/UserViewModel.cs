// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using SharedModels.EntityClasses;
using SharedModels.ValidationClasses;

namespace AsyncToolkitSample.ViewModels;

public partial class UserViewModel : ObservableValidator
{
    private readonly User _user = new()
    {
        Name = "Bob",
        Username = "admin"
    };

    [Required]
    [IsValidName]
    public string? Name
    {
        get => _user.Name;
        set => SetProperty(_user.Name, value, _user, static (user, name) => user.Name = name, true);
    }

    [Required]
    public string? Username
    {
        get => _user.Username;
        set => SetProperty(_user.Username, value, _user, static (user, username) => user.Username = username, true);
    }

    public async Task<bool> ValidateAllAsync()
    {
        ValidateAllProperties();

        var context = new ValidationContext(_user);
        var results = new List<ValidationResult>();
        bool isValid = await Validator.TryValidateObjectAsync(_user, context, results, validateAllProperties: true);

        return isValid && !HasErrors;
    }
}
