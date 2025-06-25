![Logo](.github/assets/logo.png)

A primitive Kiosk UI framework for the Raspberry Pi using the Linux Frame Buffer (`/dev/fb0`) for rendering.

Supports both 16bit and 32bit colors.

## Usage
Example repository coming soon..

## Dependencies
- `fbset` - Used to get the frame buffer information (size, color depth, etc..)
  - `apt install fbset`

## FAQ
### I get permission denied when trying to draw
- Linux user must be in the `video` and `input` user groups (requires relog).
  - `usermod -aG video $USER`
  - `usermod -aG input $USER`