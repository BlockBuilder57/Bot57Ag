using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;
using System.Timers;

namespace Bot57Ag
{
    class Silver
    {
        private static DiscordSocketClient client;
        public static SQLContext SQL = new SQLContext();
        public static Random Rand = new Random();
        public static string PathString = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";

        private readonly static Version Version = new Version(0, 2);
        public static string VersionString = $"v{Version}-{ThisAssembly.Git.Commit}{(ThisAssembly.Git.IsDirty ? "-dirty" : "")}";

        public static int ConfigIndex = 0;

        static void Main(string[] args)
        {
            Console.Title = $"Bot57Ag (Silver v{Version})";
            string ArgsToken = null;
            bool FromToken = false;

            for (int i = 0; i < args.Length - 1; i++)
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

            Tools.RefreshJSON();

            if (SQL.GetCurConfig() == null || fromtoken == true)
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

            if (string.IsNullOrWhiteSpace(SQL.GetCurConfig().Token))
                return;
            await client.LoginAsync(TokenType.Bot, SQL.GetCurConfig().Token);
            await client.StartAsync();

            client.Ready += ClientReady;

            await new CommandHandler(client, new CommandService()).InstallCommandsAsync();

            await Task.Delay(-1);
        }

        private async Task ClientReady()
        {
            if (ConfigIndex != -1)
            {
                foreach (SocketGuild guild in client.Guilds)
                    if (SQL.GetGuild(guild) == null)
                        SQL.Guilds.Add(new SQLGuild
                        {
                            GuildId = guild.Id.ToString(),
                            Prefix = SQL.GetCurConfig().PrefixDefault,
                            DropFunBucks = false
                        });
                SQL.SaveChanges();
            }

            UpdateWindowTitle();
            await SetStatusAsync();
            Timer updater = Tools.CreateTimer(TimeSpan.FromMinutes(5), async (s, e) =>
            {
                UpdateWindowTitle();
                await SetStatusAsync();
            });
        }

        public static void UpdateWindowTitle()
        {
            if (SQL.GetCurConfig() != null)
                Console.Title = $"Bot57Ag (Silver {VersionString}) - {client.CurrentUser.Username}#{client.CurrentUser.Discriminator} on {client.Guilds.Count} guild(s) (Config #{ConfigIndex}, Default Prefix {SQL.GetCurConfig().PrefixDefault})";
        }

        private async Task SetStatusAsync()
        {
            string[] status = new string[] { "something broke" };
            if (Tools.GetJSONValue("statusStrings") != null)
                status = Tools.GetJSONValue("statusStrings").Split('|');
            switch (status[0].ToLowerInvariant()[0])
            {
                case 'p':
                default:
                    await client.SetGameAsync(status.Length > 1 ? status[1] : status[0], type: ActivityType.Playing);
                    break;
                case 'w':
                    await client.SetGameAsync(status.Length > 1 ? status[1] : status[0], type: ActivityType.Watching);
                    break;
                case 'l':
                    await client.SetGameAsync(status.Length > 1 ? status[1] : status[0], type: ActivityType.Listening);
                    break;
            }
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

        public class Tools
        {
            public static dynamic JSON { get; private set; }

            public static void RefreshJSON()
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bot57Ag.Resources.strings.json"))
                using (StreamReader read = new StreamReader(stream))
                    JSON = JsonConvert.DeserializeObject(File.Exists(PathString + "strings.json") ? "{}" : read.ReadToEnd());
            }

            public static string GetJSONValue(string key)
            {
                if (JSON[key] != null)
                {
                    if (JSON[key] is JArray)
                    {
                        string[] strings = JSON[key].ToObject<string[]>();
                        return strings[Rand.Next(0, strings.Length-1)];
                    }
                    else if (JSON[key] is JObject)
                        return JSON[key].ToString();
                }
                return null;
            }

            //i basically stole this from wam sorry man
            public static Timer CreateTimer(TimeSpan length, ElapsedEventHandler handler)
            {
                Timer timer = new Timer(length.TotalMilliseconds);
                timer.Elapsed += handler;
                timer.Start();

                return timer;
            }

            public static EmbedBuilder GetStockEmbed(string title = null)
            {
                string footertext = "something errored";
                if (GetJSONValue("statusStrings") != null)
                {
                    footertext = GetJSONValue("statusStrings");
                    switch (footertext.ToLowerInvariant()[0])
                    {
                        case 'p':
                        default:
                            footertext = $"Playing {footertext.Remove(0, 2)}";
                            break;
                        case 'w':
                            footertext = $"Watching {footertext.Remove(0, 2)}";
                            break;
                        case 'l':
                            footertext = $"Listening to {footertext.Remove(0, 2)}";
                            break;
                    }
                }
                
                return new EmbedBuilder
                {
                    Color = new Discord.Color(0x0047AB),
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = client.CurrentUser.GetAvatarUrl(),
                        Name = !string.IsNullOrWhiteSpace(title) ? $"{title} - Bot57Ag" : "Bot57Ag"
                    },
                    Footer = new EmbedFooterBuilder
                    {
                        IconUrl = client.GetUser(120398901927739393).GetAvatarUrl(),
                        Text = footertext
                    }
                };
            }
        }
    }
}
