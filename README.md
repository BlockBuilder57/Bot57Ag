# Bot57Ag
Another go at a Discord bot. Now integrates a PostgreSQL server for fun and "profit".
## Compiling
If you want to run this bot, you can do so by just simply running the .sln provided.
I don't know how NuGet exactly works, but hopefully it should download all the required packages.
To actually be able to have the bot function, install a run of the mill PostgreSQL server and configure it with a "Bot57Ag" user, and give it a good password, as this stores your bot's token.
## Running
On first boot, you will be required to enter th database password. On subsequent boots, the text stored in the `dbpass.txt` file will be used as the password.
You will be stepped through the instructions of setup, then you will be able to run the bot! Refer to the `help` command for a list of commands.