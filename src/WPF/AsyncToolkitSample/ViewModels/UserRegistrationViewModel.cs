// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using SharedModels.EntityClasses;
using SharedModels.ServiceClasses;

namespace AsyncToolkitSample.ViewModels;

public partial class UserRegistrationViewModel : ObservableValidator
{
    private readonly UserRegistration _registration = new()
    {
        Username = "admin",
        Email = "admin@example.com",
        Password = "SecureP@ss123"
    };

    private readonly SimpleServiceProvider _serviceProvider = new SimpleServiceProvider()
        .Register(new UserService());

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string? Username
    {
        get => _registration.Username;
        set => SetProperty(_registration.Username, value, _registration, static (registration, username) => registration.Username = username, true);
    }

    [Required]
    [EmailAddress]
    public string? Email
    {
        get => _registration.Email;
        set => SetProperty(_registration.Email, value, _registration, static (registration, email) => registration.Email = email, true);
    }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Password
    {
        get => _registration.Password;
        set => SetProperty(_registration.Password, value, _registration, static (registration, password) => registration.Password = password, true);
    }

    public async Task<bool> ValidateAllAsync()
    {
        ValidateAllProperties();

        var context = new ValidationContext(_registration, _serviceProvider, items: null);
        var results = new List<ValidationResult>();
        bool isValid = await Validator.TryValidateObjectAsync(_registration, context, results, validateAllProperties: true);

        return isValid && !HasErrors;
    }
}
