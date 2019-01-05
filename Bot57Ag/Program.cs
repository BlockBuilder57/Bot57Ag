using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace Bot57Ag
{
    class Program
    {
        private static DiscordSocketClient client;

        static void Main(string[] args)
        {
            if (args.Length > 0)
                new Program().LoginTask(args[0]).GetAwaiter().GetResult();
            else
                new Program().LoginTask().GetAwaiter().GetResult();
        }

        public async Task LoginTask(string giventoken = null)
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            client.Log += new LogHandler().LogLevel_Warnings;

            using (SQLContext sqlcontext = new SQLContext())
            {
                if (!(sqlcontext.Config.Count() > 0))
                {
                    if (giventoken == null)
                        throw new Exception("No token exists. Please provide one in the first command arguments.");
                    sqlcontext.Config.Add(new SQLConfig
                    {
                        Token = giventoken,
                        PrefixDefault = "]",
                        AdminIDs = new string[0]
                    });
                    sqlcontext.SaveChanges();
                    Console.WriteLine("Appended config to database.");
                }

                await client.LoginAsync(TokenType.Bot, sqlcontext.Config.First().Token);
                await client.StartAsync();
            }

            client.Ready += () =>
            {
                using (SQLContext sqlcontext = new SQLContext())
                {
                    foreach (SocketGuild guild in client.Guilds)
                        if (sqlcontext.Guilds.Find(guild.Id.ToString()) == null)
                            sqlcontext.Guilds.Add(new SQLGuild
                            {
                                GuildID = guild.Id.ToString(),
                                Prefix = sqlcontext.Config.First().PrefixDefault,
                                UseFunBucks = false
                            });
                    sqlcontext.SaveChanges();
                }
                return Task.CompletedTask;
            };

            await new CommandHandler(client, new CommandService()).InstallCommandsAsync();

            await Task.Delay(-1);
        }
    }
}
