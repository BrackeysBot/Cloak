<h1 align="center">Cloak</h1>
<p align="center"><img src="icon.png" width="128"></p>
<p align="center"><i>A Discord bot for managing self-roles and persistent roles.</i></p>
<p align="center">
<a href="https://github.com/BrackeysBot/Cloak/releases"><img src="https://img.shields.io/github/v/release/BrackeysBot/Cloak?include_prereleases&style=flat-square"></a>
<a href="https://github.com/BrackeysBot/Cloak/actions?query=workflow%3A%22.NET%22"><img src="https://img.shields.io/github/actions/workflow/status/BrackeysBot/Cloak/dotnet.yml?style=flat-square" alt="GitHub Workflow Status" title="GitHub Workflow Status"></a>
<a href="https://github.com/BrackeysBot/Cloak/issues"><img src="https://img.shields.io/github/issues/BrackeysBot/Cloak?style=flat-square" alt="GitHub Issues" title="GitHub Issues"></a>
<a href="https://github.com/BrackeysBot/Cloak/blob/main/LICENSE.md"><img src="https://img.shields.io/github/license/BrackeysBot/Cloak?style=flat-square" alt="MIT License" title="MIT License"></a>
</p>

## IMPORTANT: UPCOMING CHANGE TO REPOSITORY
In February 2023, Discord integrated self-roles natively into Discord via [Community Onboarding](https://support.discord.com/hc/en-us/articles/11074987197975-Community-Onboarding-FAQ), which has made the majority of this bot's purpose obsolete. At a date TBA, a new bot under the same name will be purposed to handle activity roles in the [Brackeys Community Discord server](https://discord.gg/brackeys) and will remain closed source to prevent gaming the system. As a result this repository will move to a new name and be publicly archived.

If you have forked or cloned this repository, be aware that your upstream URL will need to change when this happens.

## About
Cloak is a Discord bot which provides features to allow members to assign self-roles, as well as for staff members to designate certain roles as "persistent",
such that the roles are automatically reapplied to members who previously had them in the even that they leave and rejoin the guild.

## Installing and configuring Cloak 
Cloak runs in a Docker container, and there is a [docker-compose.yaml](docker-compose.yaml) file which simplifies this process.

### Clone the repository
To start off, clone the repository into your desired directory:
```bash
git clone https://github.com/BrackeysBot/Cloak.git
```
Step into the Cloak directory using `cd Cloak`, and continue with the steps below.

### Setting things up
The bot's token is passed to the container using the `DISCORD_TOKEN` environment variable. Create a file named `.env`, and add the following line:
```
DISCORD_TOKEN=your_token_here
```

Two directories are required to exist for Docker compose to mount as container volumes, `data` and `logs`:
```bash
mkdir data
mkdir logs
```
The `logs` directory is used to store logs in a format similar to that of a Minecraft server. `latest.log` will contain the log for the current day and current execution. All past logs are archived.

The `data` directory is used to store persistent state of the bot, such as config values and the infraction database.

There is currently no configuration file required to run Cloak.

### Launch Cloak
To launch Cloak, simply run the following commands:
```bash
sudo docker-compose build
sudo docker-compose up --detach
```

## Updating Cloak
To update Cloak, simply pull the latest changes from the repo and restart the container:
```bash
git pull
sudo docker-compose stop
sudo docker-compose build
sudo docker-compose up --detach
```

## Using Cloak
For further usage breakdown and explanation of commands, see [USAGE.md](USAGE.md).

## License
This bot is under the [MIT License](LICENSE.md).

## Disclaimer
This bot is tailored for use within the [Brackeys Discord server](https://discord.gg/brackeys). While this bot is open source and you are free to use it in your own servers, you accept responsibility for any mishaps which may arise from the use of this software. Use at your own risk.
