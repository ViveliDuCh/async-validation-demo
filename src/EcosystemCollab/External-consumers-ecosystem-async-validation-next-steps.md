# Ecosystem: External Projects with DataAnnotations Integration — Async Validation Next Steps

> **Date:** 2026-05-08
> **Context:** Assessment of next steps for addressing external ecosystem integration points based on the current state of async validation feasibility testing.
> **Sources:**
> - [Appendix A: Integration Points Catalog](https://github.com/jeffhandley/dataannotations-validation/blob/main/appendices/appendix-a-integration-points.md)
> - [Chapter 11: Integration History](https://github.com/jeffhandley/dataannotations-validation/blob/main/chapters/11-integration-history.md)
> - [Feasibility Demo Repo](https://github.com/ViveliDuCh/async-validation-demo/tree/main)
> - Internal feasibility test guides (prototyping-approaches, runtime/aspnetcore/openapi-efcore integration guides)

---

## 1. Current State of Feasibility Testing

### What Has Been Built and Proven

The feasibility testing is structured as a **hybrid of two prototyping approaches** (chosen after evaluating three options):

| Approach | Description | Status |
|----------|-------------|--------|
| **Option 1:** Standalone demo repo with local DLLs | Fast to set up; proves API surface | ✅ Chosen (combined) |
| **Option 2:** Two-repo branches (runtime + aspnetcore forks) | Real code — not simulation | ✅ Chosen (combined) |
| **Option 3:** VMR (dotnet/dotnet) | Full SDK build; production fidelity | ❌ Not planned — overkill for feasibility |

**The chosen approach** is a **standalone demo repository** ([ViveliDuCh/async-validation-demo](https://github.com/ViveliDuCh/async-validation-demo/tree/main)) that consumes **local NuGet package DLLs** built from real async-enabled forks of both `dotnet/runtime` and `dotnet/aspnetcore`. This provides genuine integration (not simulation) in a self-contained, easy-to-run package.

#### Rationale

- **Option 1 benefit:** Samples are self-contained and easy to clone-and-run — anyone can try the async APIs without building the full runtime or aspnetcore repos.
- **Option 2 benefit:** The packages come from real builds of the actual repos with async changes applied, so the integration is genuine. `ViveliDuCh/runtime@async-validation` provides core APIs; `ViveliDuCh/aspnetcore@async-validation` provides endpoint integration.
- **Why not VMR:** The VMR is ~20GB, requires 1–4 hour build times, and is designed for official builds — not iterative prototyping. It would only be needed to prove "does `dotnet new webapi` work with async validation?" — a final-stage question, not a feasibility one.

#### Core APIs (Phase 1 — Complete)

The runtime fork (`ViveliDuCh/runtime@async-validation`) provides:

- `AsyncValidationAttribute` — base class with `IsValidAsync()`, `GetValidationResultAsync()`
- `IAsyncValidatableObject` — interface with `ValidateAsync()`
- `Validator.TryValidateObjectAsync()` / `TryValidatePropertyAsync()` / `TryValidateValueAsync()` — 8 new async methods
- 103 new async tests, 976 total tests passing
- Two-phase validation strategy: sync attributes run first (fast fail), async attributes run only if sync passes

#### Tier Integration Status

| Tier | Integration | Repo | Feasibility Status | Demo Samples |
|------|-------------|------|--------------------|--------------|
| T1 | Core DataAnnotations | dotnet/runtime | ✅ Done — Phase 1 complete | Console (2), all samples use core APIs |
| T2 | Options.DataAnnotations | dotnet/runtime | ✅ Done — `IAsyncValidateOptions<T>`, bypass strategy | Options (3 samples) |
| T2b | Options Source Generator | dotnet/runtime | ✅ Done — `Emitter.cs` emits `ValidateAsync()` | Tier2b.OptionsGeneratorBlazor |
| T3 | MVC Model Validation | dotnet/aspnetcore | 🔶 Feasibility proven, full integration deferred | MVC (2 samples — manual async) |
| T4 | Blazor Forms | dotnet/aspnetcore | ✅ Done — `EditContext.ValidateAsync()`, sync guard removed | Blazor (2 samples) |
| T5 | OpenAPI Schema | dotnet/aspnetcore | ✅ Done — metadata awareness only | OpenAPI (2 simulations) |
| T6 | EF Core Conventions | dotnet/efcore | ✅ Done — convention-based attribute recognition | EfCore (2 samples) |
| T7 | Extensions.Validation | dotnet/aspnetcore | ✅ Done — async leaf calls in `ValidatablePropertyInfo` | MinimalApi (3 samples) |
| T8 | WPF | CommunityToolkit | ✅ Demo — app-level `INotifyDataErrorInfo` bridge | WPF (2 samples) |
| T9 | WinForms | dotnet/winforms | ✅ Demo — parallel async on `TextChanged` | WinForms (4 samples) |
| T10 | Aspire | dotnet/aspire | ✅ Follows T2 automatically | N/A |
| T11 | Minimal APIs | dotnet/aspnetcore | ✅ Done — inherits Tier 7 changes | MinimalApi (3 samples) |

#### Demo Repo Structure (23 Sample Projects across 9 Technologies)

The demo repo ships pre-built DLLs from both prototype branches, targeting .NET 11 Preview, with a shared models library (`UserRegistration`, `Event`, `Order`) exercising three validation shapes: attribute-only, sync `IValidatableObject`, and async `IAsyncValidatableObject` — plus a "Case 4" sync-over-async fallback pattern via `[ReservedTitleCheck]`.

---

## 2. Ecosystem Projects Requiring Attention

The following external projects are identified in [Appendix A](https://github.com/jeffhandley/dataannotations-validation/blob/main/appendices/appendix-a-integration-points.md) as ecosystem consumers that must be considered for async validation. They are **not owned by the .NET team** but have direct integration with DataAnnotations validation.

### 2.1 OpenRiaServices (.NET Foundation)

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Direct consumer of `Validator.TryValidateObject/Property()` with its own recursive pipeline. `Entity.ValidateProperty()` in setters, `ValidationUtilities.TryValidateObject()` wrapper, `DomainService.ValidateChangeSetAsync()` server-side changeset validation. |
| **Impact of async changes** | 🔴 **High** — OpenRiaServices wraps `Validator.TryValidateObject()` in its own recursive pipeline. Adding async requires updating `ValidationUtilities` to call `TryValidateObjectAsync()`, making `ValidateProperty()` async (which currently runs in property setters — sync by nature), and updating `DomainService.ValidateChangeSetAsync()` (already async in name, but calls synchronous `ValidateOperations()` internally). |
| **Key blocker** | Property setter validation (`Entity.ValidateProperty()`) is inherently synchronous. Same "sync wall" pattern as WPF's `INotifyDataErrorInfo.GetErrors()` and Options' `IOptions<T>.Value`. |
| **Recommended strategy** | **"Bypass, Don't Infect"** — the same strategy proven in Tier 2 (Options). Add a parallel async validation path for server-side changeset validation (where `DomainService.ValidateChangeSetAsync()` already has an async shape). Property setter validation remains sync; async results surface via `ValidationResultCollection` + `INotifyDataErrorInfo` (which OpenRIA already supports for async error injection). |
| **Next steps** | 1. Open a discussion on [OpenRIAServices/OpenRiaServices](https://github.com/OpenRIAServices/OpenRiaServices) about async validation awareness. 2. Share the feasibility demo repo as evidence that `Validator.TryValidateObjectAsync()` works. 3. Propose that `DomainService.ValidateChangeSetAsync()` call `Validator.TryValidateObjectAsync()` instead of sync `ValidateOperations()`. 4. Document the "bypass" pattern for property setter validation. |
| **Timeline** | Post-Phase 1 shipping. Can adopt incrementally — server-side first, client-side entity validation later. |
| **Feasibility confidence** | 🟡 Medium — server-side path is straightforward; client-side property setters need the bypass pattern. |

### 2.2 Blazor Component Vendors

#### MudBlazor

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Field-level: calls `ValidationAttribute.GetValidationResult()` directly. Form-level: aggregates via `MudForm`. |
| **Impact of async changes** | 🟡 **Medium** — MudBlazor calls `GetValidationResult()` directly (like MVC), not through `Validator.TryValidateObject()`. Must add `instanceof AsyncValidationAttribute` checks and call `GetValidationResultAsync()` for async attrs. |
| **Recommended strategy** | Adopt after Blazor's `EditContext.ValidateAsync()` ships (Tier 4). MudBlazor's custom validation path needs its own update to call `GetValidationResultAsync()` on `AsyncValidationAttribute` subclasses. |
| **Next steps** | 1. Open issue on [MudBlazor/MudBlazor](https://github.com/MudBlazor/MudBlazor) once Tier 4 ships in a preview SDK. 2. Provide a migration guide showing the `is AsyncValidationAttribute asyncAttr` pattern. 3. Highlight that `MudForm` aggregation logic may need async-aware collection. |
| **Feasibility confidence** | 🟢 High — the pattern is well-demonstrated in the feasibility samples (Blazor AsyncBasicSample). |

#### Radzen Blazor

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Custom `RadzenDataAnnotationValidator` calls `Validator.TryValidateProperty()` directly. |
| **Impact of async changes** | 🟢 **Low** — Radzen's validator calls `Validator.TryValidateProperty()`, which has a direct async counterpart (`TryValidatePropertyAsync()`). Swap is nearly mechanical. |
| **Recommended strategy** | Drop-in replacement of `TryValidateProperty()` → `TryValidatePropertyAsync()`. |
| **Next steps** | 1. Open issue on [radzenhq/radzen-blazor](https://github.com/radzenhq/radzen-blazor) once async APIs ship. 2. The migration is straightforward — share the demo's Blazor samples as reference. |
| **Feasibility confidence** | 🟢 High — direct API swap. |

#### Telerik UI for Blazor / Syncfusion Blazor / DevExpress Blazor

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | All three wrap standard `EditForm` + `DataAnnotationsValidator` or use `EditContext.Validate()`. |
| **Impact of async changes** | 🟢 **None (if backward compatible)** — these vendors inherit validation behavior from Blazor's built-in components. If `EditContext.ValidateAsync()` and the updated `DataAnnotationsValidator` maintain backward compatibility (which the feasibility testing confirms — sync `OnValidationRequested` still fires first, async `OnAsyncValidationRequested` fires second), these vendors benefit automatically. |
| **Recommended strategy** | No proactive changes needed from these vendors. Monitor for breaking changes in `EditContext`/`DataAnnotationsValidator` API surface. |
| **Next steps** | 1. Notify vendor developer relations when async Blazor validation ships in preview. 2. Provide the feasibility demo's Blazor samples as verification reference. |
| **Feasibility confidence** | 🟢 High — inherits from Blazor core. |

### 2.3 API Frameworks

#### FastEndpoints

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Optional `EnableDataAnnotationsSupport`; recursive request validation via `Validator.TryValidateObject()`. |
| **Impact of async changes** | 🟡 **Medium** — FastEndpoints already has an async pipeline (`ExecuteAsync`). The DataAnnotations integration path calls `Validator.TryValidateObject()` synchronously within an async context. Swapping to `TryValidateObjectAsync()` is architecturally feasible but requires updating the recursive validation walker. |
| **Recommended strategy** | Add `TryValidateObjectAsync()` path alongside existing sync validation. FastEndpoints' validator architecture already supports async validators (FluentValidation-style); DataAnnotations async is a natural extension. |
| **Next steps** | 1. Open issue/discussion on [FastEndpoints/FastEndpoints](https://github.com/FastEndpoints/FastEndpoints). 2. Share the MinimalApi feasibility samples as reference for how `AddValidation()` + async attrs work. 3. Note that FastEndpoints' recursive validation with cycle detection must be preserved in the async path. |
| **Feasibility confidence** | 🟢 High — async pipeline already exists; swap at the leaf level. |

#### MiniValidation

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Built atop DataAnnotations; recursive graph walk with cycle detection; `Validator.TryValidateValue()` per property; **already supports `IAsyncValidatableObject`**. |
| **Impact of async changes** | 🟢 **Low** — MiniValidation already has `TryValidateAsync()` and `IAsyncValidatableObject`. It is the closest ecosystem project to async-ready. When `Validator.TryValidateObjectAsync()` ships, MiniValidation can delegate to it or continue its own recursive approach. |
| **Recommended strategy** | Align on the canonical `IAsyncValidatableObject` interface from `System.ComponentModel.DataAnnotations` (replacing MiniValidation's own definition). MiniValidation's recursive graph walk with cycle detection informed the design documented in [Chapter 13](https://github.com/jeffhandley/dataannotations-validation/blob/main/chapters/13-object-graph-validation.md). |
| **Next steps** | 1. Coordinate with [Damian Edwards](https://github.com/DamianEdwards) on aligning `IAsyncValidatableObject` with the runtime definition. 2. MiniValidation can serve as a validation reference for the recursive async pattern. |
| **Feasibility confidence** | 🟢 Very High — MiniValidation already demonstrates the pattern works. |

### 2.4 Schema Generators (Metadata Consumers)

#### Swashbuckle.AspNetCore / NSwag

| Aspect | Assessment |
|--------|------------|
| **How they use validation** | Read `[Required]`, `[Range]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[DataType]` as metadata for OpenAPI schema generation. **No runtime validation is ever executed.** |
| **Impact of async changes** | 🟢 **Very Low** — Same category as Tier 5 (OpenAPI) and Tier 6 (EF Core): "metadata consumers read the menu, they don't cook the food." These tools need to **recognize** new `AsyncValidationAttribute` subclasses if they carry schema-relevant properties (e.g., an `[AsyncUniqueEmail]` attribute that also exposes `Format = "email"`). |
| **What the feasibility testing proved** | The OpenAPI feasibility samples demonstrate two approaches: (A) **zero-modification reflection** — discover `AsyncValidationAttribute` subclasses and extract metadata by convention to produce `x-async-validation` schema extensions; (B) **`ISchemaDescriptor` interface** — attributes self-describe their schema contributions. Both produce valid OpenAPI-compatible JSON Schema. |
| **Recommended strategy** | No proactive changes needed unless new async attributes carry schema-relevant metadata. If they do, Swashbuckle/NSwag need to add `is AsyncValidationAttribute` checks in their schema generation pipelines — same pattern as the OpenAPI feasibility samples. |
| **Next steps** | 1. Document the `ISchemaDescriptor` pattern as a recommended way for async attributes to expose schema metadata. 2. Open issues on [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) and [NSwag](https://github.com/RicoSuter/NSwag) when async attrs with schema-relevant properties are finalized. 3. Note that Swashbuckle is in maintenance mode — NSwag may be the more active target. |
| **Feasibility confidence** | 🟢 High — metadata-only concern; no runtime behavior change. |

### 2.5 Other Known Consumers

#### ServiceStack

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Blazor templates use `EditForm` + `DataAnnotationsValidator`; HTML helpers emit unobtrusive validation attributes. |
| **Impact** | 🟢 Low — inherits Blazor async support if backward compatible. HTML helpers read attributes as metadata. |
| **Next steps** | Monitor; no proactive action needed. |

#### FluentValidation

| Aspect | Assessment |
|--------|------------|
| **How it uses validation** | Independent framework; does not consume DataAnnotations attributes. Provides MVC integration that **replaces** the DataAnnotations pipeline. |
| **Impact** | ⚪ None — FluentValidation already has full async support (`ValidateAsync`). It does not depend on DataAnnotations attributes and its MVC integration replaces rather than extends the DataAnnotations model validator. |
| **Next steps** | No action required. FluentValidation's async patterns may serve as prior art for API design decisions. |

---

## 3. Priority Matrix for Ecosystem Outreach

| Priority | Project | Action Type | Effort | When |
|----------|---------|-------------|--------|------|
| 🔴 P1 | **MiniValidation** | Align `IAsyncValidatableObject` interface | Low | Before Phase 1 ships — coordinate with Damian Edwards |
| 🔴 P1 | **OpenRiaServices** | Discussion + bypass strategy proposal | Medium | Before Phase 1 ships — .NET Foundation coordination |
| 🟡 P2 | **MudBlazor** | Migration guide + `GetValidationResultAsync` pattern | Medium | After Tier 4 (Blazor) ships in preview |
| 🟡 P2 | **FastEndpoints** | `TryValidateObjectAsync` adoption path | Low | After Phase 1 ships |
| 🟡 P2 | **Radzen Blazor** | `TryValidatePropertyAsync` swap | Very Low | After Phase 1 ships |
| 🟢 P3 | **Telerik / Syncfusion / DevExpress** | Notification only — inherits from Blazor | None | When Blazor async validation ships |
| 🟢 P3 | **Swashbuckle / NSwag** | Schema awareness docs | Very Low | When async attrs with schema metadata are finalized |
| 🟢 P3 | **ServiceStack** | Monitor only | None | Ongoing |
| ⚪ N/A | **FluentValidation** | No action | None | N/A |

---

## 4. Key Patterns Proven by Feasibility Testing

The feasibility work has validated several patterns that directly apply to ecosystem adoption:

### Pattern 1: "Bypass, Don't Infect" (from Tier 2 Options)

**Applicable to:** OpenRiaServices (property setters), WPF (`INotifyDataErrorInfo.GetErrors()`), WinForms (`Validating` event)

When a synchronous API surface cannot be made async (property getters, sync event handlers), create a **parallel async path** that runs alongside the existing sync one. The sync path stays untouched; async results are injected after completion.

```
EXISTING SYNC PATH (untouched):
  Sync trigger → Validator.TryValidateObject() → sync results

NEW ASYNC PATH (additive):
  Async trigger → Validator.TryValidateObjectAsync() → async results → merge
```

### Pattern 2: `is AsyncValidationAttribute` Type Check (from Tier 7)

**Applicable to:** MudBlazor, FastEndpoints, any project calling `GetValidationResult()` directly

```csharp
ValidationResult? result;
if (attribute is AsyncValidationAttribute asyncAttr)
{
    result = await asyncAttr.GetValidationResultAsync(value, context, ct)
        .ConfigureAwait(false);
}
else
{
    result = attribute.GetValidationResult(value, context);
}
```

This pattern preserves full backward compatibility: sync attributes take the fast path; async attributes are properly awaited.

### Pattern 3: Two-Phase Validation (built into Phase 1 core)

**Applicable to:** All ecosystem consumers calling `Validator.TryValidateObjectAsync()`

Sync attributes run first. If any fail, async attributes are skipped entirely (fast fail). This ensures that expensive async operations (database lookups, API calls) only execute when the object is otherwise valid.

### Pattern 4: Metadata-Only Awareness (from Tiers 5 & 6)

**Applicable to:** Swashbuckle, NSwag, any schema generator

Metadata consumers never call `IsValid()` — they read attribute properties. They just need to recognize `AsyncValidationAttribute` subclasses and extract schema-relevant metadata (via reflection or `ISchemaDescriptor`). No async plumbing is required.

---

## 5. Risks and Considerations

| Risk | Mitigation |
|------|------------|
| **MudBlazor's direct `GetValidationResult()` calls break async attrs** | MudBlazor will silently get sync fallback behavior (via `AsyncValidationAttribute`'s sync `IsValid()` calling `IsValidAsync().GetAwaiter().GetResult()`). Not ideal but not broken. Migration guide should prioritize MudBlazor. |
| **OpenRiaServices' recursive pipeline conflicts with `Validator.TryValidateObjectAsync()`'s own recursion** | OpenRIA may need to choose: delegate recursion to the runtime (like MiniValidation could), or maintain its own walker and call per-attribute async methods. Coordination via .NET Foundation. |
| **MiniValidation's `IAsyncValidatableObject` differs from runtime's** | Must align interface signatures before Phase 1 ships. MiniValidation's version was the inspiration; runtime's is the canonical one going forward. |
| **Swashbuckle is in maintenance mode** | Focus NSwag outreach. Document the pattern generically so any schema generator can adopt it. |
| **Blazor component vendors may not test async validation paths** | Provide the feasibility demo repo as a turnkey test harness. The 23 sample projects cover all major scenarios. |

---

## 6. Recommended Outreach Plan

### Phase A: Pre-Shipping Coordination (Now → Phase 1 GA)

1. **MiniValidation** — Open PR or issue to align `IAsyncValidatableObject` with `System.ComponentModel.DataAnnotations` definition. Damian Edwards is on the .NET team; internal coordination is straightforward.
2. **OpenRiaServices** — Open a discussion with a link to the feasibility demo repo and the "bypass" strategy documentation. Propose `DomainService.ValidateChangeSetAsync()` → `Validator.TryValidateObjectAsync()` as the first adoption step.
3. **Publish the demo repo** — Ensure [ViveliDuCh/async-validation-demo](https://github.com/ViveliDuCh/async-validation-demo/tree/main) is documented well enough for external contributors to clone and run.

### Phase B: Preview SDK Available

4. **Blazor vendor notification** — When async Blazor validation ships in a preview SDK, notify MudBlazor, Radzen, Telerik, Syncfusion, and DevExpress. Provide the `is AsyncValidationAttribute` pattern and point to the Blazor feasibility samples.
5. **FastEndpoints** — Open issue proposing `TryValidateObjectAsync()` adoption in the `EnableDataAnnotationsSupport` path.

### Phase C: Post-GA

6. **Schema generators** — If finalized async attributes carry schema-relevant metadata, open issues on NSwag and Swashbuckle with the `ISchemaDescriptor` pattern.
7. **Community content** — Blog post or docs page showing the migration patterns for each ecosystem category (runtime validator, Blazor component, metadata consumer).

---

## 7. Summary

The feasibility testing has **comprehensively proven** that async validation works across all .NET technologies — 23 sample projects across 9 technology stacks (Console, WinForms, WPF, Blazor, Minimal API, MVC, EF Core, OpenAPI, Options). The ecosystem impact is manageable:

- **3 projects need proactive coordination** (MiniValidation, OpenRiaServices, MudBlazor)
- **2 projects need notification** (FastEndpoints, Radzen)
- **5+ projects inherit support automatically** (Telerik, Syncfusion, DevExpress, ServiceStack, and other Blazor-wrapping vendors)
- **2 schema generators need optional metadata awareness** (Swashbuckle, NSwag)
- **1 project is unaffected** (FluentValidation)

The key enabler is backward compatibility: sync attributes continue working unchanged on the fast path, and the `is AsyncValidationAttribute` type check pattern ensures zero behavioral change for existing sync-only consumers. Ecosystem projects can adopt at their own pace.
