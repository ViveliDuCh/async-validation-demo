// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharedModels.EntityClasses;

namespace AsyncManualSample.ViewModels;

public class EventViewModel : ValidatableViewModelBase
{
    private readonly Event _event = new()
    {
        Title = "TBD Kickoff",
        StartDate = new DateTime(2026, 6, 1),
        EndDate = new DateTime(2026, 6, 2)
    };

    public string? Title
    {
        get => _event.Title;
        set => SetAndValidateAsync(_event.Title, value, title => _event.Title = title);
    }

    public DateTime? StartDate
    {
        get => _event.StartDate;
        set { _event.StartDate = value; OnPropertyChanged(); }
    }

    public DateTime? EndDate
    {
        get => _event.EndDate;
        set { _event.EndDate = value; OnPropertyChanged(); }
    }

    protected override object GetValidationTarget() => _event;
}
