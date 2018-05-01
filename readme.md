# Preamble

One should call this project _"AutoIt++ **Interpiler**"_, as it is an interpreter from a C#'s point of view - but works more like a compiler when viewing it from AutoIt's side....

It kind of interprets everything and transforms it into C# code, which then gets compiled. My component is therefore an interpreter - but the whole project is technically a compiler.

So we call it an **Interpiler** for now...

# The AutoIt++ ~Interpreter~ Interpiler
_or 'Autoit--' - depends on how you see it_


This project is an ~Interpreter~ Interpiler written to target a custom flavour of the _AutoIt3_ scripting language called _"AutoIt++"_ (even though it has nothing todo with C++).

The most important aspect of this project is the fact, that the ~Interpreter~ Interpiler is not only platform-independent, but can also cross-platform and cross-architecture target applications.

The ~Interpreter~ Interpiler can currently target the following systems (can be specified via the flag `-t`):
 - `win7`, `win8`, `win81`, `win10`
 - `centos`, `fedora`, `gentoo`, `opensuse`
 - `debian`, `ubuntu`, `linuxmint`
 - `osx`
 - `android` _comming soon_
 - and much more...
 
The following architectures are currently supported:
 - `x86`, `x64` (The 32Bit- and 64Bit-compatible systems from Intel or AMD)
 - `arm`, `arm64` (ARM's equivalent)

For more information about the usage of the ~Interpreter~ Interpiler, refer to the [usage page](doc/usage.md).

## Links

 - [Usage page](doc/usage.md)
 - [AutoIt++ Language reference](doc/language.md)
 - [AutoIt++ Syntax reference](doc/syntax.md)
 - [Official AutoIt3 documentation](https://www.autoitscript.com/autoit3/docs/)

## Credits

This AutoIt-~Interpreter~ Interpiler is written in C# and F# targeting the .NET-Core Framework in order to provide full platform independency.

A big shoutout to the [Roslyn Dev Team](https://github.com/dotnet/roslyn) and the [.NET Core Dev Team](https://github.com/dotnet/coreclr) for making such an awesome framework possible!

It uses a modified version of the [_Piglet_-Library](https://github.com/Dervall/Piglet) written by [Dervall](https://github.com/Dervall) in order to improve expression parsing.
All credits go to him for the wonderful LR-Parser-Library!!

-----------------

_An historic image:_<br/>
![Exception Screenshot](doc/wtf.png)
