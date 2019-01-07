using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace Bot57Ag
{
    class Silver
    {
        private static DiscordSocketClient client;
        public static SQLContext SQL = new SQLContext();
        public static Version Version = new Version(0, 1);
        public static int ConfigIndex = 0;

        static void Main(string[] args)
        {
            Console.Title = $"Bot57Ag (Silver v{Version})";
            string ArgsToken = null;
            bool FromToken = false;

            for (int i = 0; i < args.Length-1; i++)
            {
                if (args[i].ToLower().Contains("configindex"))
                {
                    i++;
                    ConfigIndex = int.Parse(args[i]);
                }
                if (args[i].ToLower().Contains("token"))
                {
                    i++;
                    ConfigIndex = -1;
                    FromToken = true;
                    ArgsToken = args[i];
                }
            }
            try
            {
                new Silver().LoginTask(ArgsToken, FromToken).GetAwaiter().GetResult();
            }
            catch (System.Net.Sockets.SocketException exceop)
            {
                Console.WriteLine(exceop);
            }
        }

        public async Task LoginTask(string token = null, bool fromtoken = false)
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            client.Log += new LogHandler().CustomLogger;

            if (SQL.GetConfig(ConfigIndex) == null || fromtoken == true)
            {
                if (!SQL.HasConnected)
                {
                    Console.WriteLine($"Error: A connection could not be made to the database.\nDo you want to run without a database? (No data can be retrieved or saved!) [y/n]");
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        ConfigIndex = -1;
                        Console.Clear();
                    }
                    else
                        Environment.Exit(0);
                }
                Registration(token, out token, out string Prefix, out string[] Admins);
                if (ConfigIndex == -1)
                {
                    SQL.NoConfigSetup(token, Prefix, Admins);
                    Console.WriteLine($"\n\nAll set. Remember, as there is no connection to the database, the config will not be saved.");
                    await Task.Delay(3500);
                    Console.Clear();
                }
                else
                {
                    SQL.Configs.Add(new SQLConfig
                    {
                        Id = ConfigIndex + 1,
                        Token = token,
                        PrefixDefault = Prefix,
                        AdminIds = Admins
                    });
                    SQL.SaveChanges();
                    Console.WriteLine($"\n\nAll set, appended config #{ConfigIndex} to database.");
                    await Task.Delay(3500);
                    Console.Clear();
                }
            }

            if (string.IsNullOrWhiteSpace(SQL.GetConfig(ConfigIndex).Token))
                return;
            await client.LoginAsync(TokenType.Bot, SQL.GetConfig(ConfigIndex).Token);
            SQL.LockTokens();
            await client.StartAsync();

            client.Ready += () =>
            {
                if (ConfigIndex != -1)
                {
                    foreach (SocketGuild guild in client.Guilds)
                        if (SQL.GetGuild(guild) == null)
                            SQL.Guilds.Add(new SQLGuild
                            {
                                GuildId = guild.Id.ToString(),
                                Prefix = SQL.GetConfig(ConfigIndex).PrefixDefault,
                                DropFunBucks = false
                            });
                    SQL.SaveChanges();
                }
                UpdateWindowTitle(client);
                return Task.CompletedTask;
            };

            await new CommandHandler(client, new CommandService()).InstallCommandsAsync();

            await Task.Delay(-1);
        }
        
        public static void UpdateWindowTitle(DiscordSocketClient client)
        {
            if (SQL.GetConfig(ConfigIndex) != null)
                Console.Title = $"Bot57Ag (Silver v{ThisAssembly.Git.Tag}) - {client.CurrentUser.Username}#{client.CurrentUser.Discriminator} on {client.Guilds.Count} guild(s) (Config #{ConfigIndex}, Prefix {SQL.GetConfig(ConfigIndex).PrefixDefault})";
        }

        private void Registration(string TokenIn, out string TokenOut, out string PrefixOut, out string[] AdminsOut)
        {
            if (TokenIn == null)
            {
                Console.WriteLine("No token exists. Please enter one below to use. If it is incorrect, the bot will not work.");
                TokenOut = Console.ReadLine();
            }
            else
                TokenOut = TokenIn;
            Console.WriteLine("Please provide a default prefix for the bot to use once it joins a server.\nYou can change this prefix later through the owner commands.");
            PrefixOut = Console.ReadLine();
            Console.Write("You also need an admin to be able to manage the bot.\nPlease give a comma seperated list of user ids you want to have ");
            Console.BackgroundColor = ConsoleColor.White; //emphasis because you can't bold a console
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("full control");
            Console.ResetColor();
            Console.Write(" over the bot.\n");
            AdminsOut = Console.ReadLine().Split(',');

            Console.Clear();
            Console.WriteLine($" Token: {TokenOut.Substring(0, 6)}...{TokenOut.Substring(TokenOut.Length - 6)}\nPrefix: {PrefixOut}\nAdmins: {string.Join(',', AdminsOut)}\n\nDoes everything look correct? [y/n]");
            if (Console.ReadKey().Key == ConsoleKey.Y)
                return;
            else
                Registration(TokenOut, out TokenOut, out PrefixOut, out AdminsOut);
        }

        
    }
}
