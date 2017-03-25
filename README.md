# PEBakery
WinBuilder drop-in replacement. Stil in development.

## What is PEBakery?
PEBakery is new, improved implementation of WinBuilder 082's script engine.

PEBakery aims to resolve WinBuilder 082's abandoned bugs, provide improved support for Unicode, more plugin developer friendly builder.

## Disclaimer
All implementation is only backed by documentation and blackbox testing, without violating WinBuilder 082's EULA.

# Current State
## Working
- PEBakery.WPF (GUI)
- Plugin UI Rendering

## TODO
- PEBakery's engine need full refactoring;


# License
Core of PEBakery is licensed under GPL.
Portions of PEBakery is licensed under MIT License and Apache License 2.0.

# State of PEBakery-Legacy
## Implemented
- Plugin Code Parser
- Project Recognition
- Logger
- Variables

## Working
- Commands
- TestSuite
- Macro (known as API in WinBuilder 082)


## Command Status
|   Class  | Implemented | All |
|----------|-------------|-----|
| Registry | 0  | 10  |
| Text     | 3  | 12  |
| Plugin   | 0  | 6   |
| UI       | 0  | 11  |
| String   | 0  | 24  |
| System   | 14 | 16  |
| Branch   | 27 | 32  |
| Control  | 2  | 8   |
| All      | 60 | 132 |
