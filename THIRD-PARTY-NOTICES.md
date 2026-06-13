# Third-Party Notices

Meshfrantic is licensed under the GNU General Public License v3.0 (see `LICENSE.txt`).
The project's choice of GPL-3.0 is **required** by its dependency on the `Meshtastic`
library, which is itself GPL-3.0 (strong copyleft).

This file lists the third-party components distributed with or linked by Meshfrantic,
along with their licenses and attribution.

---

## NuGet Packages

### Meshtastic 2.0.8
- **License:** GPL-3.0
- **Project:** https://github.com/meshtastic/c-sharp
- C# client library for the Meshtastic protobuf protocol. Because this library is
  GPL-3.0 and Meshfrantic links against it directly, the combined work is GPL-3.0.

### LeafletForBlazor 2.4.6.28
- **License:** Not declared
- **Project:** https://github.com/ichim/LeafletForBlazor-NuGet
- The upstream repository and NuGet package do not declare an explicit license.
  Publication on NuGet.org implies a grant of use, but no formal license terms are
  published. Included here for transparency.

### System.IO.Ports 10.0.6
- **License:** MIT
- **Project:** https://github.com/dotnet/runtime
- Copyright (c) .NET Foundation and Contributors.

### Tmds.DBus 0.92.0
- **License:** MIT
- **Project:** https://github.com/tmds/Tmds.DBus
- Copyright (c) Tom Deseyn and contributors.

### Serilog.AspNetCore 9.0.0
- **License:** Apache-2.0
- **Project:** https://github.com/serilog/serilog-aspnetcore
- Copyright Serilog Contributors. Licensed under the Apache License, Version 2.0.

### Serilog.Sinks.File 6.0.0
- **License:** Apache-2.0
- **Project:** https://github.com/serilog/serilog-sinks-file
- Copyright Serilog Contributors. Licensed under the Apache License, Version 2.0.

### Serilog.Sinks.Console 6.0.0
- **License:** Apache-2.0
- **Project:** https://github.com/serilog/serilog-sinks-console
- Copyright Serilog Contributors. Licensed under the Apache License, Version 2.0.

---

## Bundled Front-End Assets

### Bootstrap (`wwwroot/lib/bootstrap`)
- **License:** MIT
- **Project:** https://github.com/twbs/bootstrap
- Copyright (c) 2011-2024 The Bootstrap Authors. The MIT license header is retained
  in the distributed CSS/JS files.

### Leaflet (loaded via LeafletForBlazor)
- **License:** BSD-2-Clause
- **Project:** https://github.com/Leaflet/Leaflet
- Copyright (c) 2010-2024 Volodymyr Agafonkin, CloudMade. Map tiles and their
  attribution (e.g. OpenStreetMap) are governed separately by the tile provider.

---

## Notes

- The Apache-2.0 license requires preservation of copyright and NOTICE attribution;
  the Serilog entries above satisfy that requirement.
- The BSD-2-Clause license requires retention of the copyright notice; the Leaflet
  entry above satisfies that requirement.
- This file should be kept in sync with `Meshfrantic/Meshfrantic.csproj`
  `<PackageReference>` entries.
