# Valcore Desktop Development Guide

## 1. Development Mandates

### Separation of Concerns
Monolithic code is strictly prohibited. You must decouple domain business logic, presentation ViewModels, EF Core data layers, and AXAML UI view components into strict directory boundaries.

### MVVM Pattern
- Code-behind files (`.axaml.cs`) must contain **zero business logic**.
- Use `CommunityToolkit.Mvvm` for all data bindings and observable properties.
- Use explicit bindings (`{Binding Path}`) instead of code-behind manipulation.

### UI Design (Industrial Flow)
- **Zero-Mouse First:** Complex views must be 100% operable via keyboard shortcuts (F1-F12, Enter, Esc, Navigation Keys).
- **Density:** Maximize screen space. High-density data grids, compact padding, and tight typography scales.
- **Colors:** Deep background tones (Slate/Zinc). Single-tone muted functional accents. NO neon gradients.

## 2. Compilation & Deployment
Deployment bypasses standard self-contained runtimes in favor of AOT or single-file optimization.
Use the included `build_release.ps1` to trigger Native publish configurations.

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## 3. Hardware Interfacing
Always offload hardware calls (ESC/POS Printing, COM Port scanning) to `Task.Run()` or dedicated asynchronous background threads. NEVER execute raw hardware I/O on the Avalonia UI Thread.

## 4. Git Protocol
- The `master` branch is production-ready only.
- Single-line imperative commit messages under 50 chars. (e.g., `feat: establish local sqlite audit log engine`).
