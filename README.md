# Meshfrantic

A .NET 10 Blazor Server application for interfacing with a [Meshtastic](https://meshtastic.org/) LoRa mesh radio node over USB/serial.

## Features

- **Dashboard** — live stats: nodes seen, messages, channels, and connected device info (name, ID, position, hardware, firmware, battery, telemetry)
- **Messages** — channel-aware chat with a sidebar listing active channels and direct-message threads per node; message delivery ACK tracking (sent / delivered / failed)
- **Nodes** — sortable table with expandable rows showing full node detail: position, telemetry (device metrics + environment sensors), and one-click traceroute request with hop-by-hop path display
- **Channels** — manage all 8 channels: edit name, PSK, role, uplink/downlink; changes written to device immediately
- **Map** — Leaflet map with node position markers, waypoint markers, and layer toggles (nodes / waypoints)
- **Config** — tabbed device config editor: LoRa (region, modem preset, hop limit, TX power), Device (role, rebroadcast mode, timezone), Position (GPS mode, broadcast interval), Bluetooth, Power
- **Module Config** — tabbed module config editor: Telemetry, MQTT, Store & Forward, Neighbor Info, External Notifications, Serial
- **Commands** — device admin panel: request config, reboot, factory reset, node DB reset, rename device
- **Logs** — live packet log
- **Themes** — six built-in terminal themes with localStorage persistence

> Meshfrantic covers everything in the official Meshtastic web client, plus device admin commands (reboot, factory reset, node DB reset) that the official client doesn't expose.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Meshtastic device connected via USB/serial (e.g. T-Beam, Heltec, RAK)
- Windows, Linux, or macOS

## Getting Started

```bash
git clone https://github.com/riddlemd/meshfrantic
cd meshfrantic/Meshfrantic
dotnet run
```

Open `https://localhost:5001` in your browser, select your COM port, and click **Connect**.

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

Blazor Server — single `MeshtasticService` singleton drives the serial connection and exposes state to all pages via `StateChanged` events.

| File | Role |
|---|---|
| `Services/MeshtasticService.cs` | Singleton: connection lifecycle, read loop, packet dispatch, send methods |
| `Services/ThemeService.cs` | Tracks active theme; switching is JS/localStorage-driven |
| `Components/Layout/MainLayout.razor` | Connection toolbar (port select, connect/disconnect, battery, theme) |
| `Components/Pages/Home.razor` | Dashboard + own-device telemetry card |
| `Components/Pages/Messages.razor` | Channel/DM sidebar, message list, ACK status icons |
| `Components/Pages/Nodes.razor` | Node table with expandable detail + traceroute |
| `Components/Pages/Channels.razor` | Channel management (name, PSK, role, uplink/downlink) |
| `Components/Pages/Map.razor` | Leaflet map with node + waypoint layers |
| `Components/Pages/DeviceConfig.razor` | Core radio config (LoRa, Device, Position, Bluetooth, Power) |
| `Components/Pages/ModuleSettings.razor` | Module config (Telemetry, MQTT, Store & Forward, etc.) |
| `Components/Pages/Commands.razor` | Admin commands |
| `Components/Pages/Logs.razor` | Packet log |
| `Models/ChatMessage.cs` | Per-message state including channel, ACK, and DM destination |
| `Models/NodeTelemetry.cs` | Per-node telemetry accumulator (device + environment metrics) |

## Dependencies

- [`Meshtastic`](https://www.nuget.org/packages/Meshtastic) 2.0.8 — C# client library for the Meshtastic protobuf protocol
- [`LeafletForBlazor`](https://www.nuget.org/packages/LeafletForBlazor) — interactive map component
- [`System.IO.Ports`](https://www.nuget.org/packages/System.IO.Ports) 10.0.6 — serial port access
