# Meshfrantic

A .NET 10 Blazor Server application for interfacing with a [Meshtastic](https://meshtastic.org/) LoRa mesh radio node over USB/serial.

## Features

- **Dashboard** — live stats: nodes seen, messages, channels, and connected device info (name, ID, position, hardware, firmware, battery)
- **Messages** — real-time mesh chat with send support
- **Nodes** — table of all nodes in the network with signal, position, and device info
- **Commands** — device admin panel: request config, reboot, factory reset, node DB reset, rename device
- **Themes** — four built-in terminal themes with localStorage persistence

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Meshtastic device connected via USB/serial (e.g. T-Beam, Heltec, RAK)
- Windows, Linux, or macOS

## Getting Started

```bash
git clone https://github.com/yourname/meshfrantic
cd meshfrantic/Meshfrantic
dotnet run
```

Open `https://localhost:5001` in your browser.

## Themes

### Green
A Classic phosphor green
<img width="1910" height="482" alt="image" src="https://github.com/user-attachments/assets/e4e62a55-2291-4cb9-a2da-d8998b1cfd36" />


### Amber
A Warm amber terminal
<img width="1909" height="476" alt="image" src="https://github.com/user-attachments/assets/6964684e-1cec-43f7-8f5c-7abc39c54b79" />


### White
Greys and whites
<img width="1909" height="481" alt="image" src="https://github.com/user-attachments/assets/4105e94d-4cb0-458c-9884-3c54892e3cbc" />

### T-800
Red phosphor on pure black
<img width="1910" height="476" alt="image" src="https://github.com/user-attachments/assets/f946a33b-61f0-45b3-8f91-56090b3cdeea" />

### Nostromo
Alien/Aliens inspired — aged amber + xenomorph acid
<img width="1915" height="478" alt="image" src="https://github.com/user-attachments/assets/815cf45e-911b-4e0e-9cde-0bf4bb73f20e" />

### Cyberdyne
Electric blue on cold steel
<img width="1914" height="468" alt="image" src="https://github.com/user-attachments/assets/b5d5184f-271f-4fcf-93eb-830b5139f2aa" />


## Architecture

Blazor Server

| File | Role |
|---|---|
| `Services/MeshtasticService.cs` | Singleton: connection lifecycle, send commands, state events |
| `Services/MeshtasticReaderService.cs` | `BackgroundService` that drives the read loop |
| `Services/ThemeService.cs` | Tracks active theme; switching is JS/localStorage-driven |
| `Components/Layout/MainLayout.razor` | Connection toolbar (port select, connect/disconnect, theme) |
| `Components/Pages/Home.razor` | Dashboard |
| `Components/Pages/Messages.razor` | Chat UI |
| `Components/Pages/Nodes.razor` | Node list |
| `Components/Pages/Commands.razor` | Admin commands |

## Dependencies

- [`Meshtastic`](https://www.nuget.org/packages/Meshtastic) 2.0.8 — C# client library for the Meshtastic protobuf protocol
- [`System.IO.Ports`](https://www.nuget.org/packages/System.IO.Ports) 10.0.6 — serial port access
