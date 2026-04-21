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

### Amber
A Warm amber terminal

### White
Greys and whites


### Nostromo
Alien/Aliens inspired — aged amber + xenomorph acid
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
