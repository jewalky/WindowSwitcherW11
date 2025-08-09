# WindowSwitchW11

A quick and dirty Alt+Tab replacement that works like the classic one from WinNT, as 24H2 removed the ability to reg-patch this.

Note: might not be good for reuse, since parts of the code are written by AI (specifically the parts that interact with WinAPI). MIT license for the same reason.

Preview:

https://github.com/user-attachments/assets/73b6c7a6-08f2-4ba5-9e32-0e91c15784d6

(Note, in the preview the switching is a bit clunky, this is simply because the recorder consistently sets itself as the first window — this was broken with regular NT switcher too :))

Usage: just build and add to autoload. Note that this code requires escalated prilileges because otherwise it crashes when trying to get the information from e.g. Task Manager, and it will revert to the original Alt+Tab on the admin windows.