# Rip Van BluRay

## Overview

Rip Van BluRay is a service that runs on a Computer with One or Many Optical Disk Drives. When running, when a Disc is inserted into one of the drives it will identify the type of Disc (Music CD, DVD, BluRay) and begin to rip it. Afterwards it will eject the Disc and will be ready to rip again.

**Currently Automated Ripping of TV Shows is not supported because of no reliable ways of determining whether a DVD/BluRay is a Movie or TV Show/Series. If I someday find a way to do it, I will implement it.**

## Dependencies

1. **Linux** - I wrote this using Ubuntu as the main distrobution but in theory any Linux Distribution could work.

    Currently I recommend running the system with a GUI interface as configuring MakeMKV, at this point in time, is easier.

2. **MakeMKV** - The brains behind ripping Movies/TV on Disc (DVD, BluRay, 4K UHD)

    MakeMKV is free in its current Beta state but requires a beta key available on the MakeMKV forums. I do recommend buying a Registration Key for prolonged use.

    - ccextractor - Required for Ripping Closed Captions (MakeMKV will complain if not installed)

3. **ABCDE** - Probably the best Music ripping software around

    - flac - This automation rips Music CDs to flac for Quality and Portability

## Installation

I have included a SystemD unit file in this repository. You may customize to your liking but the one included should get to you started. Once configured to start with the system, it will begin when the system is booted. I do recommend, before running it as a service, to first do a manual rip of a DVD, BluRay, or UHD Disc and a Music CD to ensure MakeMKV and ABCDE are working properly. **DO NOT COME TO ME FOR SUPPORT ON MAKEMKV OR ABCDE. I CANNOT HELP.**

## Configuration

In the repository there is a file named settings.json. It has all the settings that you can change/manipulate. I will try to include a settings file will all available settings listed. More will likely be added as time goes on. This settings file should be located in the Rip Van BluRay default directory ($HOME/.RipVanBluRay) which is created on first launch. If no file is found it will use the defaults.

## Building

This application is written in .Net 6 as a Hosted Service. You can build an executable yourself. When inside the root directory of the solution run the follow:

```dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true```

This will build a single executable for simplicity. You can build/publish as you please if you want to do something different.
