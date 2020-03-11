# Installation:

- Install Nuget package Sharpness.Logging.Aspnet
- Add the following config to appsetings.json

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

- In ```CreateHostBuilder``` add call:

```
.ConfigureLogging(c => {
    // c.ClearProviders(); // uncomment if other logs are not needed
    c.AddWebLogger(); // add this line
})
```

- Use ILogger DI mechanism as before
