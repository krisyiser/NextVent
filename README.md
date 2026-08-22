# Ticketfy Desktop POS

Ticketfy is a high-performance, industrial-grade Point of Sale (POS) and ERP system built on **Protocol Valcore v4.0**.

![Valcore Badge](https://img.shields.io/badge/Architecture-Valcore_v4.0-blue?style=flat-square)
![Avalonia UI](https://img.shields.io/badge/UI-Avalonia-purple?style=flat-square)
![.NET 9](https://img.shields.io/badge/Runtime-.NET_9-512BD4?style=flat-square)

## Overview
Ticketfy is designed to provide zero-network local resilience, utilizing an offline-first encrypted SQLite database and direct hardware integration for high-speed retail checkout operations. It strictly enforces the MVVM pattern via `CommunityToolkit.Mvvm` and features an ultra-dense, keyboard-first industrial user interface.

## Documentation
Please refer to the `docs/` folder for comprehensive architectural guidelines:
- [Architecture & Structure](docs/ARCHITECTURE.md)
- [Development Guide & Mandates](docs/DEVELOPMENT_GUIDE.md)

## Requirements
- .NET 9.0 SDK
- Windows 10/11 (for full hardware POS compatibility)

## Build Instructions
For development:
```bash
dotnet build Ticketfy.Desktop
```

For production (Release):
Use the automated PowerShell script to produce an optimized, single-file native executable.
```powershell
./Ticketfy.Desktop/build_release.ps1
```

## Installation & Windows Compatibility Troubleshooting

If you encounter the Windows error:
> **"No se puede ejecutar esta aplicación en el equipo"** / *"This app can't run on your PC"*

Follow these steps:
1. **Automatic Detection via valcore.cloud**:
   - The official download portal at [valcore.cloud](file:///C:/Users/YERSI/.gemini/antigravity-ide/scratch/NextVent/web/download.html) uses `web/valcore-download.js` to automatically detect if your PC runs 64-bit or 32-bit Windows and serves the matching installer (`Ticketfy-Setup-v3.0.18-x64.exe` vs `Ticketfy-Setup-v3.0.18-x86.exe`).
2. **Unblock Downloaded File (Mark of the Web)**:
   - Right-click the downloaded `.exe` installer > select **Properties**.
   - At the bottom of the *General* tab, check the **Unblock** (*Desbloquear*) checkbox.
   - Click **Apply** > **OK** and launch the installer again.

## Licensing & Security
This software utilizes `Ticketfy.Keygen` for ECDSA-based cryptographic offline license verification. Hardcoded secrets are explicitly prohibited.
