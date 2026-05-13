// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

namespace AsyncManualSample.ViewModels;

public class UserViewModel : ValidatableViewModelBase
{
    private readonly User _user = new()
    {
        Name = "Bob",
        Username = "admin"
    };

    public string? Name
    {
        get => _user.Name;
        set => SetAndValidateAsync(_user.Name, value, name => _user.Name = name);
    }

    public string? Username
    {
        get => _user.Username;
        set => SetAndValidateAsync(_user.Username, value, username => _user.Username = username);
    }

    protected override object GetValidationTarget() => _user;
}
