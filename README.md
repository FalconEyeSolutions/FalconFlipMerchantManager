# Falcon Flip Merchant Manager

The Falcon Flip Merchant Manager is a specialized tool designed for introducers to swiftly onboard and link merchants with the FlipPay API. This application streamlines the process of managing merchant accounts, enhancing efficiency and providing a seamless experience for both introducers and merchants.

## Key Features

- Quick Merchant Onboarding: Simplifies and accelerates the onboarding process for new merchants.
- Efficient Link Management: Enables introducers to easily manage link operations for merchant accounts.
- User-Friendly Interface: Intuitive command-line interface tailored for introducer operations.
- Custom Settings: Configurable options including Introducer Token and Demo Mode.
- Integrated Logging: Comprehensive logging system for tracking application activities.

## System Requirements

- .NET 7.0
- Access to the FlipPay API

## Dependencies

- FlipPayWebClient: Utilizes the `FlipPayWebClient` NuGet package for seamless interaction with the FlipPay API. It is available on [NuGet](https://www.nuget.org/packages/FlipPayWebClient/) and its source code can be found on [GitHub](https://github.com/FalconEyeSolutions/FlipPayWebClient).

## Installation

1. Clone the repository to your local machine.
2. Ensure you have .NET 7.0 installed.
3. Navigate to the project directory.
4. Use `dotnet build` to build the project.

## Usage

Run the application with dotnet run. Follow the on-screen prompts to quickly onboard merchants or manage their links.

## Configuring the Application

- Introducer Token: Essential for API authentication. Follow the on-screen prompts to enter it at runtime.
- Demo Mode: Toggle this mode to enable demo or production API calls.

## Merchant Onboarding and Link Management

- Select the appropriate option from the main menu to manage onboarding requests or link operations.

## Logging

Features a custom console logger to monitor operations, visible during the application's runtime.

## Contributions

We welcome contributions to enhance this tool. Please fork the repository and submit pull requests with your improvements.