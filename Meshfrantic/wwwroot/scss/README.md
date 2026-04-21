# SCSS Structure

This directory contains the SCSS source files for Meshfrantic. All files are compiled into a single `main.css` file in the parent `/css/` directory.

## Organization

- **main.scss** — Entry point that imports all partials
- **_variables.scss** — CSS variables and color definitions
- **_base.scss** — Base element styles, typography, forms, alerts, buttons
- **_components.scss** — Component-specific styles (cards, tables, chat, pagination, etc.)
- **layout/** — Layout-related styles
  - _header.scss — Main layout structure, header, logo, connection bar
  - _nav.scss — Navigation menu
  - _reconnect-modal.scss — Reconnect modal dialog
- **pages/** — Page-specific styles
  - _logs.scss — Logs page layout and styling
- **themes/** — Theme CSS files (compiled from separate sources)

## Development

To watch SCSS files and auto-compile during development:

```bash
npm run sass
```

This will watch the `/wwwroot/scss/` directory and automatically compile to `/wwwroot/css/main.css` whenever SCSS files change.

## Production Build

To compile SCSS with compression for production:

```bash
npm run sass:build
```

This creates minified CSS in `/wwwroot/css/main.css`.

## Adding New Styles

1. **Component styles** — Add to `_components.scss` or create a new file and import it in `main.scss`
2. **Page styles** — Create a new partial in `pages/` and import in `main.scss`
3. **Layout styles** — Create a new partial in `layout/` and import in `main.scss`
4. **Utilities/Variables** — Add to `_variables.scss`

Remember to import new partials in `main.scss`:

```scss
@import 'path/to/new-partial';
```
