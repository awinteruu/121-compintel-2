# How to compile and run

## Without an IDE
Make sure you have the dotnet runtime installed.

```shell
dotnet run
```

If you're benchmarking times, you'll want to use a release version:

```shell
dotnet publish; cd bin/Release/net8.0/publish/; ./solver2-1
```
## With an IDE
Import the project and run it.
