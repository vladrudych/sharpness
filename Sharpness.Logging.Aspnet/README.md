# Installation:

- Install Nuget package ```Sharpness.Logging.Aspnet```
- Add the following config to appsetings.json, appsecrets.json etc.

```
"Logging": {
    "Web": {
        "ApiUrl": "https://{{HOST}}/api/hubs/write",
        "ClientId": "{{CLIENT_ID}}",
        "ClientSecret": "{{CLIENT_SECRET}}",
        "LogLevel": "Information" // or whatever you want
    }
}
```

- In ```Program.cs``` add:

```
using Sharpness.Logging.Aspnet;

CreateWebHostBuilder(args)
    .ConfigureLogging(c => {
        // c.ClearProviders(); // uncomment if other logs are not needed
        #if !DEBUG // there is no need to put live logs while debugging
        c.AddWebLogger(); // add this line
        #endif
    })
```

- Use ILogger DI mechanism as before
