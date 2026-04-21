# Themes

SCSS theme files for Meshfrantic. Each theme defines a complete color palette via SCSS maps and CSS custom properties.

## Available Themes

- **Terminal Green** (`_terminal-green.scss`) - Classic phosphor green on black CRT aesthetic
- **Terminal Amber** (`_terminal-amber.scss`) - Warm amber phosphor on black CRT aesthetic
- **Terminal White** (`_terminal-white.scss`) - Crisp white phosphor on black CRT aesthetic
- **Nostromo** (`_nostromo.scss`) - Alien/Aliens inspired: aged amber + xenomorph acid
- **Terminator** (`_terminator.scss`) - T-800 HUD aesthetic: red phosphor on black
- **Cyberdyne** (`_cyberdyne.scss`) - Corporate tech aesthetic: electric blue on cold steel

## Structure

Each theme file defines CSS custom properties that can be used throughout the application.

### Color Keys

All themes define the following colors (via CSS custom properties):
- `--mesh-primary` - Main theme color
- `--mesh-bright` - Bright variant for highlights
- `--mesh-dim` - Dimmed variant
- `--mesh-very-dim` - Very dimmed variant
- `--mesh-border` - Border color
- `--mesh-accent` - Accent color
- `--mesh-bg` - Background color
- `--mesh-text` - Text color
- `--mesh-danger` - Danger/error state
- `--mesh-warning` - Warning state
- `--mesh-black` - Pure black

## Usage

Import a theme directly in your SCSS:
```scss
@import 'themes/terminal-green';
```

Or use the CSS custom properties in CSS:
```css
color: var(--mesh-text);
background: var(--mesh-bg);
```
