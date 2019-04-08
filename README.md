# DockerDeveloperTools

This tool is primarily intended to be used when developing Docker images using Docker for Windows.
It tries to combine as many different work processes as possible into one tool.
Some of the things that would normally require one or more commands in Powershell/Command prompt ca be done with a single click in the application.

[Downloads](https://drive.google.com/drive/folders/1RwvxWN8UHJgQEetQTajKH6uMor2scI7p?usp=sharing)

### Intro
My background for starting this app is that at my workplace we recently started working with Docker.
As work progressed I found myself in need of a tool that could do many of the things that i had to use Powershell/Command Prompt for.
I also needed a better and always up to date overview of images and containers.
So I spent a bit of time and came up with this little thing.

I'll appreciate any input or suggestions you may have.

__Just to be perfectly clear this app is in no way affiliated with the official Docker system.__

### Known Bugs
1. Sometimes the selection in lists don't update correctly and displayed details therefore incorrect.
2. The image list resets selection everytime the list is updated.

### Planned Features/Reworks/Upgrades
The following are a list of things I'm planning to add.

1. __Networks__: A list of networks that exists in Docker along with their details. The current list was just my initial go at it, the more detailed view came after.
2. Show logs directly in the app instead of opening the Command Prompt.
3. Attach to container should be a multi document dockable tab in the app much like opening code files in Visual Studio.
The idea is that you can attach to multiple containers a once and dock the windows side by side to more easily monitor activity when requests chain through multiple containers.
4. Start container
5. Delete all images should retry over and over until all images are gone or until images no longer gets removed.
This is mostly because images are dependent upon each other so one iteration doesn't remove all images.
6. The ability to show/hide images that are not "primary" images.
These are typically images that primary images are dependent upon but not really interresting for the developer.
7. Start/stop/restart the Docker Desktop app. Useful for the times when Docker seems to not work properly and all you need to do to fix it is restart Docker.
8. Start/stop/restart the Docker service. Also useful for when Docker misbehave.
9. "Upgrade" to Visual Studio 2019 and DevExpress 19.1 once released.

### Development
For anyone wanting to take a look at and build the application you will need a license for [DevExpress WinForms](https://www.devexpress.com/products/net/controls/winforms/)

Also if you intent to create any pull requests please make sure you add comments in your code.
I realize I havn't been too good at it myself mainly because of time issues but it is my intent to add them were appropriete when i have the time.

Right now there are no automation because of the DevExpress controls.
Although DevExpress do offer a NuGet feed there is the small problem with the license.
If i were to add the NuGet feed to the nuget.config file it would include my license which effectively opens up my license to anyone working on the project also anyone could copy the feed to their own projects which I'm sure DevExpress wont like.

Svg images used on buttons and the like are either from the DevExpress library (embedded in DevExpress libraries) or created using the DevExpress SVG Icon Builder which can be found on the [Microsoft Store](https://www.microsoft.com/en-us/p/svg-icon-builder/9mxbbwvknrvr).
To make it easier for others to create similar icons please try to use one of those solutions.

### License
- [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
- Copyright 2019
