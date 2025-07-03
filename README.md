![Logo](.github/assets/logo.png)

A Kiosk UI framework for the Raspberry Pi using the Linux Frame Buffer device (`/dev/fb0`) for rendering.

## Usage
To get started, check out the sample project [here](https://github.com/lewpar/MyKUIPi/tree/master/MyKUIPi.Sample).

NuGet package coming soon..

## Features
- Touch screen support.
  - Automatic detection of touch screen/pad device.
  - Touch calibration.
- 16-bit and 32-bit color support.
- XML powered UI
  - Automatic wiring of button handlers
  - Planned: Data binding

## Dependencies
- `fbset` - Used to get the frame buffer information (size, color depth, etc..)
  - `apt install fbset`
- `ImageSharp` - Used to support loading of all image file types.

## FAQ
### I get permission denied when trying to draw
- Linux user must be in the `video` and `input` user groups (requires relog).
  - `usermod -aG video $USER`
  - `usermod -aG input $USER`