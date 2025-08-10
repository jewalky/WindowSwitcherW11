# WindowSwitchW11

A quick and dirty Alt+Tab replacement that works like the classic one from WinNT, as 24H2 removed the ability to reg-patch this.

Note: might not be good for reuse, since parts of the code are written by AI (specifically the parts that interact with WinAPI). MIT license for the same reason.

## Preview:

https://github.com/user-attachments/assets/73b6c7a6-08f2-4ba5-9e32-0e91c15784d6

In the preview the switching is a bit clunky, this is simply because the recorder consistently sets itself as the first window — this was broken with regular NT switcher too :)

Also, latest version is closer to original in appearance:

<img width="376" height="110" alt="image" src="https://github.com/user-attachments/assets/fdf475a9-51c0-472f-895e-58b3eeaf62ba" />

## Usage

Just build and add the release binary to autoload. 

Note that this code requires escalated prilileges because otherwise it crashes when trying to get the information from e.g. Task Manager, and it will revert to the original Alt+Tab on the admin windows. For this reason, you might want to schedule it to run on logon via Task Scheduler or so, as other methods are less reliable and will present you with UAC prompt on every login.

Note that default Release build for whatever reason does not drop the required files and the app won't start.

These two files are expected to be next to WindowSwitchW11.exe:

- WindowSwitchW11.deps.json:

```
{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v6.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v6.0": {
      "WindowSwitchW11/1.0.0": {
        "runtime": {
          "WindowSwitchW11.dll": {}
        }
      }
    }
  },
  "libraries": {
    "WindowSwitchW11/1.0.0": {
      "type": "project",
      "serviceable": false,
      "sha512": ""
    }
  }
}
```

- WindowSwitchW11.runtimeconfig.json:

```
{
  "runtimeOptions": {
    "tfm": "net6.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "6.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "6.0.0"
      }
    ]
  }
}
```

