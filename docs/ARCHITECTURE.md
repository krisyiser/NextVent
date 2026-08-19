# Ticketfy - Valcore Enterprise Architecture

## 1. Structural Overview
Ticketfy is an industrial-grade Desktop POS/ERP application strictly adhering to **Protocol Valcore v4.0**.
It is built as a 100% Native Desktop executable using `.NET 9`, `Avalonia UI`, and `C# 13`. Web wrappers (Electron, Next.js, WebViews) are **strictly prohibited** in this project.

### Core Technologies
- **Runtime:** .NET 9 Native Runtime
- **UI Framework:** Avalonia UI (XAML-based, Cross-Platform Native GPU Rendering via Skia / Direct3D)
- **Architecture Pattern:** Strict MVVM (Model-View-ViewModel) via `CommunityToolkit.Mvvm`
- **Data Layer:** Local SQLite via Entity Framework Core 9 + SQLCipher Encryption
- **Validation:** FluentValidation + INotifyDataErrorInfo

## 2. Directory Structure Matrix

### `Ticketfy.Desktop/Core/`
Contains domain models, enums, constants, globally accessible helpers, and validation schemas.
- **Models:** Pure C# record DTOs.
- **Validators:** FluentValidation rules that enforce absolute data integrity before EF Core execution.

### `Ticketfy.Desktop/Data/`
The Entity Framework Core data access layer.
- **Entities:** Database-mapped entities.
- **Interceptors:** Audit and security EF Core interceptors.
- **Migrations:** SQLite DB schemas.
All database queries MUST use asynchronous pagination and strict SELECT projections. Unbounded `ToList()` calls are banned.

### `Ticketfy.Desktop/Services/`
The backbone of offline execution and hardware integration.
- **Hardware:** Direct `System.IO.Ports` / Native Win32/POSIX raw sockets for ESC/POS thermal printers and barcode scanners.
- **Security:** Local token generation and encryption wrappers.
- All services are strictly asynchronous to prevent UI freezes.

### `Ticketfy.Desktop/ViewModels/`
The orchestrator layer. Maps `Core` models and `Services` output into properties observable by `Views`.
- All ViewModels inherit from `ObservableObject`.
- Must contain **ZERO** direct UI coupling (No references to `Avalonia.Controls`).

### `Ticketfy.Desktop/Views/`
The presentation layer.
- **Code-behind (.axaml.cs):** Must be strictly empty of business logic. Only `InitializeComponent()` or pure view-lifecycle events are allowed.
- **Density:** UI components must follow the Industrial Flow principles: high information density, 60-30-10 color rule (Zinc/Slate palettes), and full keyboard tab-index support.

## 3. Data Sovereignty & Offline Execution
Ticketfy is designed for zero-network local resilience. 
- All transactional records commit instantly to the local encrypted SQLite DB.
- If cloud synchronization drops, the UI will fall back to "Isolated Local Legacy Mode" seamlessly. No UI threads will lock.

## 4. Hardware I/O
- Direct POS peripheral integration is managed on dedicated background threads.
- Fallback caching is implemented for scenarios where hardware is momentarily disconnected.
