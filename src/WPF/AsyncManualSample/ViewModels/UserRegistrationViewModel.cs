// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;

namespace AsyncManualSample.ViewModels;

public class UserRegistrationViewModel : ValidatableViewModelBase
{
    private readonly UserRegistration _registration = new()
    {
        Username = "admin",
        Email = "admin@example.com",
        Password = "SecureP@ss123"
    };

    private readonly SimpleServiceProvider _serviceProvider = new SimpleServiceProvider()
        .Register(new UserService());

    public string? Username
    {
        get => _registration.Username;
        set => SetAndValidateAsync(_registration.Username, value, username => _registration.Username = username);
    }

    public string? Email
    {
        get => _registration.Email;
        set => SetAndValidateAsync(_registration.Email, value, email => _registration.Email = email);
    }

    public string? Password
    {
        get => _registration.Password;
        set => SetAndValidateAsync(_registration.Password, value, password => _registration.Password = password);
    }

    protected override object GetValidationTarget() => _registration;

    protected override IServiceProvider? GetServiceProvider() => _serviceProvider;
}
