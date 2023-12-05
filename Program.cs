﻿using FalconFlipMerchantManager;
using FlipPayApiLibrary;
using FlipPayApiLibrary.Models.Common;
using FlipPayApiLibrary.Models.General;
using FlipPayApiLibrary.Models.Link;
using FlipPayApiLibrary.Models.Onboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Text.RegularExpressions;

// Initialize Logger
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole(options =>
    {
        options.FormatterName = "custom"; // Set a custom formatter
    })
    .AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
});

ILogger<Program> programLogger = loggerFactory.CreateLogger<Program>();
ILogger<FlipPayWebClient> flipPayWebClientLogger = loggerFactory.CreateLogger<FlipPayWebClient>();

// Welcome User
Console.WriteLine("Welcome to the Falcon Flip Merchant Manager!");

// Load Configuration
string companyName = LoadCompanyName();
string token = LoadToken();
bool demo = LoadDemoMode();

#region Configuration Methods

string LoadCompanyName()
{
    string loadedCompanyName = File.Exists("companyName.txt") ? File.ReadAllText("companyName.txt") : string.Empty;

    if (string.IsNullOrEmpty(loadedCompanyName))
    {
        Console.WriteLine("Company Name not found.");
        return PromptForStringSetting("Enter Company Name: ", "Company Name cannot be empty.", "companyName.txt");
    }

    return loadedCompanyName;
}

string LoadToken()
{
    string loadedToken = File.Exists("token.txt") ? File.ReadAllText("token.txt") : string.Empty;

    if (string.IsNullOrEmpty(loadedToken))
    {
        Console.WriteLine("Introducer Token not found.");
        return PromptForStringSetting("Enter Introducer Token: ", "Introducer Token cannot be empty.", "token.txt");
    }

    return loadedToken;
}

bool LoadDemoMode()
{
    if (File.Exists("demo.txt"))
    {
        return File.ReadAllText("demo.txt") == "True";
    }
    else
    {
        Console.WriteLine("Demo Mode setting not found.");
        return PromptForBoolSetting("Enter Demo Mode (true/false): ", "Invalid input. Please enter true or false.", "demo.txt");
    }
}

#endregion

#region Main Loop

var merchantId = string.Empty;
var onboardingId = string.Empty;

var flipPayConfig = new FlipPayConfig
{
    Token = LoadToken(),
    IsDemo = LoadDemoMode()
};

var flipPayWebClient = new FlipPayWebClient(flipPayConfig, flipPayWebClientLogger);

while (true)
{
    var choice = GetUserChoice("\nMain Menu:\n1. General Operations\n2. Link Operations\n3. Onboarding Operations\n4. Settings\n5. Exit\nEnter your choice: ");
    switch (choice)
    {
        case "1": await ShowGeneralSubMenu(); break;
        case "2": await ShowLinkSubMenu(); break;
        case "3": await ShowOnboardingSubMenu(); break;
        case "4": ShowSettingsSubMenu(); break;
        case "5": ExitProgram(); return;
        default: LogError("Invalid choice. Please try again."); break;
    }
}

#endregion

#region General Operations

async Task ShowGeneralSubMenu()
{
    merchantId = GetValidInput("Enter Merchant ID (M-xxxx-xxxx): ", IsValidMerchantId);
    if (string.IsNullOrEmpty(merchantId)) return;

    while (true)
    {
        var generalChoice = GetUserChoice("\nGeneral Operations:\n1. Retrieve Bank Accounts\n2. Retrieve Products Available\n3. Return to Main Menu\nEnter your choice: ");
        switch (generalChoice)
        {
            case "1": await DisplayBankAccounts(); break;
            case "2": await DisplayAvailableProducts(); break;
            case "3": return;
            default: LogError("Invalid choice. Please try again."); break;
        }
    }
}

async Task DisplayBankAccounts()
{
    var response = await flipPayWebClient.RetrieveBankAccounts(merchantId);
    if (response == null)
    {
        LogAndDisplayInfo("Bank accounts not found.");
        return;
    }

    LogAndDisplayInfo($"Bank accounts found: {response.Accounts.Count}");
    LogAndDisplayInfo("Bank accounts:");

    // Display bank accounts
    foreach (var bankAccount in response.Accounts)
    {
        LogAndDisplayInfo($"Account ID: {bankAccount.AccountId}, Account Name: {bankAccount.AccountName}, Account Number: {bankAccount.AccountNumber}, Bsb: {bankAccount.Bsb}");
    }
}

async Task DisplayAvailableProducts()
{
    var response = await flipPayWebClient.RetrieveProductsOnAMerchantAccount(merchantId);

    if (response == null || response.Products == null)
    {
        LogAndDisplayInfo("Products not found.");
        return;
    }

    LogAndDisplayInfo($"Products found: {response.Products.Count}");
    LogAndDisplayInfo("Products:");

    // Display products
    foreach (var productInfo in response.Products)
    {
        LogAndDisplayInfo($"Product ID: {productInfo.ProductId}, Min Amount: {productInfo.MinAmount}, Max Amount: {productInfo.MaxAmount}");

        if(productInfo.MerchantFacility != null)
        LogAndDisplayInfo($"Merchant Facility: {productInfo.MerchantFacility}");
    }
}

#endregion

#region Link Operations

async Task ShowLinkSubMenu()
{
    merchantId = GetValidInput("Enter Merchant ID (M-xxxx-xxxx): ", IsValidMerchantId);
    if (string.IsNullOrEmpty(merchantId)) return;

    while (true)
    {
        var linkChoice = GetUserChoice("\nLink Operations:\n1. Request Link\n2. Retrieve Status of Link\n3. Remove Link\n4. Return to Main Menu\nEnter your choice: ");
        switch (linkChoice)
        {
            case "1": await flipPayWebClient.RequestAnAccountLink(new LinkPostRequest(merchantId, null)); break;
            case "2": await DisplayLinkStatus(); break;
            case "3": await flipPayWebClient.RemoveAnAccountLink(merchantId); break;
            case "4": merchantId = string.Empty; return;
            default: Console.WriteLine("Invalid choice. Please try again."); break;
        }
    }
}

async Task DisplayLinkStatus()
{
    var response = await flipPayWebClient.RetrieveTheStatusOfAnAccountLink(merchantId);
    LogAndDisplayInfo(response == null ? "Link not found." : $"Link status for Merchant ID {response.MerchantId}: {response.Status}");
}

bool IsValidMerchantId(string merchantId)
{
    return MerchantIdRegex().IsMatch(merchantId);
}

#endregion

#region Onboarding Operations

async Task ShowOnboardingSubMenu()
{
    while (true)
    {
        var onboardChoice = GetUserChoice("\nOnboarding Operations:\n1. Create Onboarding Request\n2. Retrieve Onboarding Status\n3. Cancel Onboarding Request\n4. Return to Main Menu\nEnter your choice: ");
        switch (onboardChoice)
        {
            case "1": await CreateOnboardingRequest(); break;
            case "2": await DisplayOnboardingStatus(); break;
            case "3": await CancelOnboardingRequest(); break;
            case "4": return;
            default: programLogger.LogError("Invalid choice. Please try again."); break;
        }
    }
}

async Task CreateOnboardingRequest()
{
    Console.Write("Send Communications (true/false): ");
    bool sendComms;
    while (!bool.TryParse(Console.ReadLine(), out sendComms))
    {
        LogError("Invalid input. Please enter true or false.");
        Console.Write("Send Communications (true/false): ");
    }

    Console.Write("Enter Receiver's Name: ");
    var receiverName = Console.ReadLine();

    if (string.IsNullOrEmpty(receiverName))
    {
        LogError("Receiver's Name cannot be empty.");
        return;
    }

    Console.Write("Enter Receiver's Email: ");
    var receiverEmail = Console.ReadLine();

    if (string.IsNullOrEmpty(receiverEmail))
    {
        LogError("Receiver's Email cannot be empty.");
        return;
    }

    OnboardPostRequest onboardPostRequest = new(
        sendComms,
        new(companyName, string.Empty, string.Empty, string.Empty),
        new(receiverName, receiverEmail),
        null);

    var response = await flipPayWebClient.CreateAnOnboardingRequest(onboardPostRequest);
    LogAndDisplayInfo(response == null ? "Onboarding request not created." : $"Onboarding Request Successful. \nOnboarding ID: {response.OnboardingId} \nURL: {response.OnboardingUrl}");
}

async Task DisplayOnboardingStatus()
{
    onboardingId = GetValidInput("Enter Onboarding ID (OB-xxxx-xxxx): ", IsValidOnboardingId);
    if (string.IsNullOrEmpty(onboardingId)) return;
    var response = await flipPayWebClient.RetrieveAnOnboardingRequest(onboardingId);
    LogAndDisplayInfo(response == null ? "Onboarding request not found." : $"Onboarding Status for {onboardingId}: {response.Status} \nURL: {response.OnboardingUrl} \nMerchant ID: {response.MerchantId ?? "Not yet created"}");
}

async Task CancelOnboardingRequest()
{
    onboardingId = GetValidInput("Enter Onboarding ID (OB-xxxx-xxxx): ", IsValidOnboardingId);
    if (string.IsNullOrEmpty(onboardingId)) return;
    await flipPayWebClient.CancelAnOnboardingRequest(onboardingId);
}

bool IsValidOnboardingId(string onboardingId)
{
    return OnboardingIdRegex().IsMatch(onboardingId);
}

#endregion

#region Settings

void ShowSettingsSubMenu()
{
    while (true)
    {
        var settingsChoice = GetUserChoice($"\nCompany Name: {companyName}\nIntroducer Token: {token}\nDemo Mode: {demo}\n\nSettings:\n1. Set Introducer Token\n2. Set Demo Mode\n3. Set Company Name\n4. Return to Main Menu\nEnter your choice: ");

        switch (settingsChoice)
        {
            case "1":
                token = PromptForStringSetting("Enter Introducer Token: ", "Introducer Token cannot be empty.", "token.txt");
                UpdateFlipPayWebClient();
                break;
            case "2":
                demo = PromptForBoolSetting("Enter Demo Mode (true/false): ", "Invalid input. Please enter true or false.", "demo.txt");
                UpdateFlipPayWebClient();
                break;
            case "3":
                companyName = PromptForStringSetting("Enter Company Name: ", "Company Name cannot be empty.", "companyName.txt");
                break;
            case "4":
                return;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }
}

void UpdateFlipPayWebClient()
{
    // Update the FlipPayConfig with new settings
    flipPayConfig.Token = token;
    flipPayConfig.IsDemo = demo;

    // Update the FlipPayWebClient instance
    flipPayWebClient = new FlipPayWebClient(flipPayConfig, flipPayWebClientLogger);

    // Make sure to get the garbage collector to clean up the old instance
    GC.Collect();
}

string PromptForStringSetting(string prompt, string errorMessage, string fileName)
{
    return PromptForSetting(
        prompt,
        input => !string.IsNullOrEmpty(input),
        errorMessage,
        input => input.Trim(),
        fileName);
}

bool PromptForBoolSetting(string prompt, string errorMessage, string fileName)
{
    return PromptForSetting(
        prompt,
        input => TryParseBool(input, out bool result),
        errorMessage,
        bool.Parse,
        fileName);
}

bool TryParseBool(string input, out bool result)
{
    return bool.TryParse(input, out result);
}

T PromptForSetting<T>(string prompt, Func<string, bool> validationFunc, string errorMessage, Func<string, T> parseFunc, string fileName)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        if (input != null && validationFunc(input))
        {
            var setting = parseFunc(input);
            SaveSettingToFile(fileName, setting);
            return setting;
        }
        else
        {
            programLogger.LogError(errorMessage);
        }
    }
}

void SaveSettingToFile<T>(string fileName, T setting)
{
    if (string.IsNullOrEmpty(fileName))
        return;

    if (setting == null)
        return;

    try
    {
        File.WriteAllText(fileName, setting.ToString());
        Console.WriteLine($"{Path.GetFileNameWithoutExtension(fileName)} successfully set.");
    }
    catch (IOException ex)
    {
        programLogger.LogError($"Error writing to file: {ex.Message}");
    }
}

#endregion

#region Helper Methods

string GetUserChoice(string message)
{
    Console.Write(message);
    return Console.ReadLine() ?? string.Empty;
}

void LogAndDisplayInfo(string message)
{
    programLogger.LogInformation("\n" + message);
    Thread.Sleep(1000);
}

void LogError(string message)
{
    programLogger.LogError("\n" + message);
    Thread.Sleep(1000);
}

void ExitProgram()
{
    Console.WriteLine("Exiting program...");
    Environment.Exit(0);
}

string GetValidInput(string prompt, Func<string, bool> validationFunc)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (validationFunc(input))
            return input;

        Console.WriteLine("Invalid input. Please try again.");
    }
}

#endregion

#region Regex Patterns

partial class Program
{
    [GeneratedRegex("^OB-\\d{4}-\\d{4}$")]
    private static partial Regex OnboardingIdRegex();
}

partial class Program
{
    [GeneratedRegex("^M-\\d{4}-\\d{4}$")]
    private static partial Regex MerchantIdRegex();
}

#endregion