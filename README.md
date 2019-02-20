# File Based Monzo Provider

Library to help manage monzo secrets based on storing them in the local filesystem. Handles retries and the usual oauth setup.


## Setup

Create a JSON confile file in your project similar to the following:

```
{
  "oauthPath": "~/.config/monzo-exporter/monzo-oauth.json",
  "monzoClientId": "abc123",
  "monzoClientSecret": "abc123",
  "monzoRedirectUri": "https://localhost"
}
```

Then load it into A IConfigurationBuilder (see Microsoft.Configuration.Extensions) call "GetAccessToken" as needed.
