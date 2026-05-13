// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using SharedModels.EntityClasses;

namespace AsyncToolkitSample.ViewModels;

public partial class EventViewModel : ObservableValidator
{
    private readonly Event _event = new()
    {
        Title = "TBD Kickoff",
        StartDate = new DateTime(2026, 6, 1),
        EndDate = new DateTime(2026, 6, 2)
    };

    [Required]
    public string? Title
    {
        get => _event.Title;
        set => SetProperty(_event.Title, value, _event, static (item, title) => item.Title = title, true);
    }

    public DateTime? StartDate
    {
        get => _event.StartDate;
        set => SetProperty(_event.StartDate, value, _event, static (item, startDate) => item.StartDate = startDate);
    }

    public DateTime? EndDate
    {
        get => _event.EndDate;
        set => SetProperty(_event.EndDate, value, _event, static (item, endDate) => item.EndDate = endDate);
    }

    public async Task<bool> ValidateAllAsync()
    {
        ValidateAllProperties();

        var context = new ValidationContext(_event);
        var results = new List<ValidationResult>();
        bool isValid = await Validator.TryValidateObjectAsync(_event, context, results, validateAllProperties: true);

        return isValid && !HasErrors;
    }
}
