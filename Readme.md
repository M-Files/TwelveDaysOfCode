# 12 Days of Code M-Files Development Challenge

Have you been looking to start building M-Files customizations, or to hone your skills?  Have some free time over the festive period?  If so then why not take a look at our 12 Days of Code M-Files Development Challenge!

The purpose of this is to have a little fun and to learn some new skills.  All levels are encouraged to participate.

Each day we will release a small task that you will be asked to implement using M-Files.  Most of these tasks are aimed at the Vault Application Framework, and all require you to write some C#.  If you've not done some of these things before then each task should take you around an hour.  If you've got lots of experience then you will obviously do them quickly, but we have some ideas for how you might want to extend the task a little.

We will be running the challenge between the 13th and 24th December.  This allows you to "complete" the challenge before the typical holiday season starts.  Remember: there's no obligation to get the challenges done on the day that they're released, so pop back whenever you have time to do another one.

## Rules

* You can use any appropriate technology, including external libraries.  It's your code; build it as you would normally (or would like to normally!).
* If you would like to make your code available to everyone then feel free to host it on GitHub or similar.  If you would like to keep your code to yourself then that is fine as well.
* If you don't have any development experience with M-Files then start with the Developer Portal.  We'll give you guidance to get started on each day.
* If you do have M-Files development experience then take this opportunity to try out new things.  Want to unit test approaches?  Go for it.  Want to try out the VAF Extensions library? Awesome!
* Keep things positive and constructive.  Everyone's code style and concerns are different; acknowledge and accept those.
* Some of the M-Files team will be posting example solutions each day.  If you'd like us to see what you've built then we'll monitor <a href="https://www.linkedin.com/feed/hashtag/?keywords=MFiles12DaysOfCode">LinkedIn</a> and <a href="https://twitter.com/hashtag/MFiles12DaysOfCode">Twitter</a> for the hashtag #MFiles12DaysOfCode, or post on the M-Files Developer Community!
* *There are no points or prizes, unfortunately.

## Getting Started

To take part in the challenge you will need a few things set up:

1. You will need the latest version of M-Files installed on your computer.  You can download the <a href="https://www.m-files.com/try-m-files/">30-day trial from our website</a> if you need it.
1. You will need Visual Studio 2019 or 2022 installed (the free community edition is fine, if you qualify).  You will also need the <a href="https://marketplace.visualstudio.com/items?itemName=M-Files.MFilesVisualStudioExtensions">M-Files Visual Studio Template Package</a> installed.
	1. Note: If you are using Visual Studio 2022 then you may also need to <a href="https://dotnet.microsoft.com/download/dotnet-framework/net452">install the .NET 4.5.2 developer pack</a> so that you can create .NET 4.5 applications.
1. You will need to have the <a href="vault-backup">12 Days of Code vault</a> configured.  We will use this for the challenges.

Challenges will be released once per day, in the evening.  You can choose to undertake each challenge the day it's released, or at any time over the festive period.

## Tasks

*Make sure that you have all your prerequisites set up: set up your M-Files server, restore the challenge vault, and install/configure Visual Studio.  Do this before the challenge begins!*

1. **13th December:** Start off simple: Use the Vault Application Framework 2.3 Visual Studio template to create a new VAF 2.3 application.
	1. Open the PowerShell file and change the vault name to install to.  Build the application in debug mode and check the Visual Studio "Output" window to check that your application was installed to the vault with no errors.
	1.	Optional: Use nuget to add a reference to the VAF Extensions library and change the VaultApplication base class to `MFiles.VAF.Extensions.ConfigurableVaultApplicationBase<Configuration>`. 
	1.	Open the `appdef.xml` file and update the name, version, publisher, and other information that you would like to set.  Also set the Multi-Server-Mode compatible flag to true.  Rebuild and check that the changes are shown in the M-Files Admin software.
1. **14th December:** *Not yet published*
1. **15th December:** *Not yet published*
1. **16th December:** *Not yet published*
1. **17th December:** *Not yet published*
1. **18th December:** *Not yet published*
1. **19th December:** *Not yet published*
1. **20th December:** *Not yet published*
1. **21st December:** *Not yet published*
1. **22nd December:** *Not yet published*
1. **23rd December:** *Not yet published*
1. **24th December:** *Not yet published*
