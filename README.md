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

## Licensing & Security
This software utilizes `Ticketfy.Keygen` for ECDSA-based cryptographic offline license verification. Hardcoded secrets are explicitly prohibited.
