# async-validation-demo

Standalone demo repo for proving the async `DataAnnotations` validation API surface works across .NET endpoints (MVC, Blazor, Options, etc.) — without requiring multi-repo builds.

## Goal

Demonstrate feasibility of the `AsyncValidationAttribute`, `Validator.*Async`, and `IAsyncValidatableObject` APIs by simulating how each endpoint (MVC model binding, Blazor `EditContext`, Options validation) would call the new async paths. This is **not** a real integration — it's a self-contained proof that the API surface is correct and usable for each consumer pattern.

## Folder Structure

```
async-validation-demo/
├── local-packages/   ← Pre-built DLLs from the runtime fork (async-validation branch)
├── src/              ← Prototype/simulation projects per endpoint tier
│   ├── OptionsPrototype/    (Tier 2 – Options.DataAnnotations)
│   ├── MvcSimulation/       (Tier 3 – MVC model validation pattern)
│   └── BlazorSimulation/    (Tier 4 – EditContext.ValidateAsync pattern)
└── README.md
```

- **`local-packages/`** — Contains `System.ComponentModel.Annotations.dll` (and any other assemblies) built from the prototype branches. Demo projects reference these directly so anyone can clone and run without building the runtime.
- **`src/`** — One project per endpoint tier. Each simulates the real call pattern the endpoint uses, proving the async API fits without requiring the actual aspnetcore codebase.

## Prototype Branches

| Repo | Branch | Description |
|------|--------|-------------|
| [ViveliDuCh/runtime](https://github.com/ViveliDuCh/runtime/tree/async-validation) | `async-validation` | Core async validation APIs (`AsyncValidationAttribute`, `Validator.*Async`, `IAsyncValidatableObject`) |
| [ViveliDuCh/aspnetcore](https://github.com/ViveliDuCh/aspnetcore/tree/async-validation) | `async-validation` | Endpoint integration (MVC, Blazor, Minimal APIs) |
