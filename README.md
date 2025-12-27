# Easy Markers for Chessbase/Fritz

## What
A simple utility to help me record chess videos using ChessBase or Fritz.

**The goal:** avoid holding Alt + Shift keys to draw arrows or mark squares.

**The idea:** intercept the mouse right-click and simulate holding Alt+Shift to draw red markers (using Fritz).

![ChessProgram19_YS9VIPtUyQ](https://github.com/user-attachments/assets/630651a7-2151-4e45-88ab-0bddfb7b2bab)

## Tech stack

Making a .NET app seems the best option to run on Windows (Chessbase/Fritz are Win-only).
I could've made with Windows Forms as well (probably would be even simpler), but chose WPF.

Other reasons:
- Excellent performance for low-level hooks.
- Native integration with Windows API.
- Easy creation of system tray applications.
- Single executable with no external dependencies.
- Precise control over mouse/keyboard events.

## Disclaimer
The program requires admin rights to work properly.

## Known issues
- Alt+Shift keys hard-coded: this is what serves me right now, didn't feel need to customize.
  - This makes recent Chessbase versions (17+) to draw cyan markers.

## Possible improvements
- [ ] Add a menu in the system tray to choose which color user wants to draw.
