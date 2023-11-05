# PowerShell.Testing
[![.NET](https://github.com/dorokhin-bohdan/PowerShell.Testing/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/dorokhin-bohdan/PowerShell.Testing/actions/workflows/dotnet.yml) [![#](https://img.shields.io/nuget/v/Tool.PowerShell.Testing.svg)](https://www.nuget.org/packages/Tool.PowerShell.Testing/)
---

## Goal
As a developer of CLI tools, I want to have the ability to write E2E tests on those tools. To do it I need to run cmdlet commands in PowerShell and read output data. 

## Usage
``` dotnet
// Creating an instance of testing tool
using var tool = PSTestingTool.Create();

// Subscribing on event 
var subscription = tool.OnDataAdded.Subscribe(msg => { 
    /* Do somethig with message */ 
});

// Executes cmdlet commad
await tool.ExecuteScriptAsync(command);
```

## License

This project is open-sourced software licensed under the [MIT](LICENSE).