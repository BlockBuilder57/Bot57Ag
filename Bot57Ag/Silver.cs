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

            if (args.Length > 0 && !int.TryParse(args[0], out ConfigIndex))
                new Silver().LoginTask(args[0]).GetAwaiter().GetResult();
            else
                new Silver().LoginTask().GetAwaiter().GetResult();
        }

        public async Task LoginTask(string giventoken = null)
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            client.Log += new LogHandler().CustomLogger;

            using (SQLContext sql = new SQLContext())
            {
                if (!(sql.Config.Count()-1 < ConfigIndex))
                {
                    Registration(giventoken, out string Token, out string Prefix, out string[] Admins);
                    sql.Config.Add(new SQLConfig
                    {
                        Token = Token,
                        PrefixDefault = Prefix,
                        AdminIDs = Admins
                    });
                    sql.SaveChanges();
                    Console.WriteLine($"\n\nAll set, appended config #{sql.Config.Count()} to database.");
                    await Task.Delay(3500);
                    Console.Clear();
                }

                await client.LoginAsync(TokenType.Bot, giventoken ?? sql.Config.ToArray()[ConfigIndex].Token);
                await client.StartAsync();
            }

            client.Ready += () =>
            {
                using (SQLContext sql = new SQLContext())
                {
                    foreach (SocketGuild guild in client.Guilds)
                        if (sql.GetGuild(guild) == null)
                            sql.Guilds.Add(new SQLGuild
                            {
                                GuildID = guild.Id.ToString(),
                                Prefix = sql.Config.ToArray()[ConfigIndex].PrefixDefault,
                                DropFunBucks = false
                            });
                    sql.SaveChanges();
                }
                UpdateWindowTitle(client);
                return Task.CompletedTask;
            };

            await new CommandHandler(client, new CommandService()).InstallCommandsAsync();

            await Task.Delay(-1);
        }
        
        public void UpdateWindowTitle(DiscordSocketClient client)
        {
            Console.Title = $"Bot57Ag (Silver v{Version}) - {client.CurrentUser.Username}#{client.CurrentUser.Discriminator} on {client.Guilds.Count} guild(s)";
        }

        private void Registration(string TokenIn, out string TokenOut, out string PrefixOut, out string[] AdminsOut)
        {
            if (TokenIn == null)
            {
                Console.WriteLine("No token exists. Please enter one below, and it will be saved to the database.");
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
    }
}
