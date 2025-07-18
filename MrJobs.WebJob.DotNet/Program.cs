﻿using Azure.Identity;
using Microsoft.Extensions.Configuration;

//   __      __      ___.         ____.     ___.    
//  /  \    /  \ ____\_ |__      |    | ____\_ |__  
//  \   \/\/   // __ \| __ \     |    |/  _ \| __ \ 
//   \        /\  ___/| \_\ \/\__|    (  <_> ) \_\ \
//    \__/\  /  \___  >___  /\________|\____/|___  /
//         \/       \/    \/                     \/ 
Console.WriteLine("Starting Job...");

try
{
  // 1. Read appsettings
  var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
  var scope = configuration["Api:Scope"];
  var route = configuration["Api:Route"];

  if (string.IsNullOrWhiteSpace(scope))
    throw new ArgumentException("Bad API Scope");

  if (string.IsNullOrWhiteSpace(route))
    throw new ArgumentException("Bad API Route");

  Console.WriteLine($"Successfully read appsettings");

  // 2. Get the access token

  // Use `DefaultAzureCredential` >>> `ManagedIdentityCredential` as
  // this object will attempt multiple auth methods in this order:
  //  1. FIRST Managed Identity (when running in Azure)
  //  2. THEN az cli (login when running locally)

  var credential = new DefaultAzureCredential(); // new ManagedIdentityCredential();
  var tokenRequestCtx = new Azure.Core.TokenRequestContext([$"{scope}/.default"]);
  var accessToken = await credential.GetTokenAsync(tokenRequestCtx);
  var jwt = accessToken.Token;

  Console.WriteLine($"Successfully obtained access token '{jwt[0..Math.Min(5, jwt.Length)]}...'");

  // 3. Make the HTTP request
  var httpClient = new HttpClient()
  {
    DefaultRequestHeaders =
    {
      { "Authorization", $"Bearer {accessToken.Token}" },
    }
  };
  var uri = new Uri(route);
  var response = await httpClient.GetAsync(uri);
  var content = await response.Content.ReadAsStringAsync();

  Console.WriteLine($"Successfully completed Job with status code '{response.StatusCode}' and content '{content}'");
}
catch (Exception ex)
{
  Console.WriteLine($"Failed to complete Job, reason: {ex.Message}");
}
