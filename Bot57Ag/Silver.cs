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
            new Silver().LoginTask(ArgsToken, FromToken).GetAwaiter().GetResult();
        }

        public async Task LoginTask(string token = null, bool fromtoken = false)
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            client.Log += new LogHandler().CustomLogger;

            using (SQLContext sql = new SQLContext())
            {
                if (sql.GetConfig(ConfigIndex) == null || fromtoken == true)
                {
                    Registration(token, out token, out string Prefix, out string[] Admins);
                    if (ConfigIndex == -1)
                    {
                        NoConfigSetup(token, Prefix, Admins);
                        Console.WriteLine($"\n\nAll set. As you gave a token as an argument, the config will not be saved.");
                        await Task.Delay(3500);
                        Console.Clear();
                    }
                    else
                    {
                        sql.Configs.Add(new SQLConfig
                        {
                            Id = ConfigIndex + 1,
                            Token = token,
                            PrefixDefault = Prefix,
                            AdminIds = Admins
                        });
                        sql.SaveChanges();
                        Console.WriteLine($"\n\nAll set, appended config #{ConfigIndex} to database.");
                        await Task.Delay(3500);
                        Console.Clear();
                    }
                }

                await client.LoginAsync(TokenType.Bot, sql.GetConfig(ConfigIndex).Token);
                LockTokens();
                await client.StartAsync();
            }

            client.Ready += () =>
            {
                using (SQLContext sql = new SQLContext())
                {
                    if (ConfigIndex != -1)
                    {
                        foreach (SocketGuild guild in client.Guilds)
                            if (sql.GetGuild(guild) == null)
                                sql.Guilds.Add(new SQLGuild
                                {
                                    GuildId = guild.Id.ToString(),
                                    Prefix = sql.GetConfig(ConfigIndex).PrefixDefault,
                                    DropFunBucks = false
                                });
                        sql.SaveChanges();
                    }
                }
                UpdateWindowTitle(client);
                return Task.CompletedTask;
            };

            await new CommandHandler(client, new CommandService()).InstallCommandsAsync();

            await Task.Delay(-1);
        }
        
        public void UpdateWindowTitle(DiscordSocketClient client)
        {
            using (SQLContext sql = new SQLContext())
                if (sql.GetConfig(ConfigIndex) != null)
                    Console.Title = $"Bot57Ag (Silver v{ThisAssembly.Git.Tag}) - {client.CurrentUser.Username}#{client.CurrentUser.Discriminator} on {client.Guilds.Count} guild(s) (Config #{ConfigIndex}, Prefix {sql.GetConfig(ConfigIndex).PrefixDefault})";
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
            Console.WriteLine($" Token: {TokenOut.Substring(0, 6)}...{TokenOut.Substring(TokenOut.Length - 6)}\nPrefix: {PrefixOut}\nAdmins: {string.Join(',', AdminsOut)}\n\nDoes everything look correct? (y/n)");
            if (Console.ReadKey().Key == ConsoleKey.Y)
                return;
            else
                Registration(TokenOut, out TokenOut, out PrefixOut, out AdminsOut);
        }

        private static bool _LockTokens;

        public static bool TokensLocked()
        {
            return _LockTokens;
        }

        public void LockTokens()
        {
            _LockTokens = true;
        }

        public static SQLConfig NoConfigSQLConfig;
        private bool NoConfigSetup_Ran = false;

        public void NoConfigSetup(string token, string prefix, string[] ids)
        {
            if (!NoConfigSetup_Ran)
            {
                NoConfigSQLConfig = new SQLConfig
                {
                    Id = -1,
                    Token = token,
                    PrefixDefault = prefix,
                    AdminIds = ids
                };
                NoConfigSetup_Ran = true;
            }
        }
    }
}
