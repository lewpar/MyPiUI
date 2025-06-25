![Logo](.github/assets/logo.png)

A primitive Kiosk UI framework for the Raspberry Pi using the Linux Frame Buffer (`/dev/fb0`) for rendering.

- 16-bit and 32-bit color support.
- Touch screen support.

## Usage
To get started, check out the sample project [here](https://github.com/lewpar/MyKUIPi/tree/master/MyKUIPi.Sample).

NuGet package coming soon..

## Dependencies
- `fbset` - Used to get the frame buffer information (size, color depth, etc..)
  - `apt install fbset`

## FAQ
### I get permission denied when trying to draw
- Linux user must be in the `video` and `input` user groups (requires relog).
  - `usermod -aG video $USER`
  - `usermod -aG input $USER`