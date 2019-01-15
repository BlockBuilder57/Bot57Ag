using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using Bot57Ag.Preconditions;

namespace Bot57Ag.Commands
{
    [Summary("Miscellaneous commands, no group. What more could you ask for?")]
    public class MiscCommands : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService cmdsrv;

        public MiscCommands(CommandService cmdsrv)
        {
            this.cmdsrv = cmdsrv;
        }

        [Command("ping")]
        [Summary("Tennis for Two (also known as Computer Tennis) is a sports vi...")]
        public async Task MiscPing()
        {
            await ReplyAsync($"hi, hello, i am here\ntook `~{Context.Client.Latency}ms`");
        }

        [Command("status")]
        [Summary("Checks versions and other various settings.")]
        public async Task MiscStatus()
        {
            EmbedBuilder aboutEmbed = Silver.Tools.GetStockEmbed();
            aboutEmbed.AddField("OS", System.Runtime.InteropServices.RuntimeInformation.OSDescription, false);
            aboutEmbed.AddField("Silver Version", Silver.VersionString, true);
            aboutEmbed.AddField("Discord.NET Version", typeof(DiscordConfig).GetTypeInfo().Assembly.GetName().Version.ToString(3), true);
            await ReplyAsync(null, false, aboutEmbed.Build());
        }

        [Command("about")]
        [Summary("Who hurt you little ~~MacBook~~ bot?")]
        public async Task MiscAbout()
        {
            await ReplyAsync("i'm tired so let's make this quick\nblock - my dad\nwam - the irish man dad stole his ideas from\nthat's enough for now");
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("M E T A")]
        public async Task MiscHelp([Remainder] string search = null)
        {
            EmbedBuilder helpEmbed = Silver.Tools.GetStockEmbed(string.IsNullOrWhiteSpace(search) ? "Help" : $"Results for {search}");
            if (string.IsNullOrWhiteSpace(search))
            {
                foreach (ModuleInfo module in cmdsrv.Modules.OrderBy(x => !x.Name.Contains("Misc")))
                {
                    if (!module.IsSubmodule)
                    {
                        string moduleinfo = "";
                        foreach (CommandInfo cmd in module.Commands)
                        {
                            if (cmd.CheckPreconditionsAsync(Context, null).Result.IsSuccess)
                            {
                                string[] Aliases = cmd.Aliases.Take(cmd.Aliases.Count / cmd.Module.Aliases.Count).Skip(1).ToArray();
                                for (int i = 0; i < Aliases.Length; i++)
                                    Aliases[i] = Aliases[i].Split(' ').Last();
                                string Alias = string.Join(", ", Aliases);
                                moduleinfo += $"{cmd.Name}{(string.IsNullOrWhiteSpace(Alias) ? "" : $" ( {Alias} )")}, ";
                            }
                        }
                        foreach (ModuleInfo submod in module.Submodules)
                        {
                            if (submod.GetExecutableCommandsAsync(Context, null).Result.Count > 0)
                            {
                                string[] Aliases = submod.Aliases.Take(submod.Aliases.Count / submod.Parent.Aliases.Count).Skip(1).ToArray();
                                for (int i = 0; i < Aliases.Length; i++)
                                    Aliases[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Aliases[i].Split(' ').Last());
                                string Alias = string.Join(", ", Aliases);
                                moduleinfo += $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(submod.Name)} Cmds{(String.IsNullOrWhiteSpace(Alias) ? "" : $" ( {Alias} )")}, ";
                            }
                        }
                        if (module.GetExecutableCommandsAsync(Context, null).Result.Count > 0)
                        {
                            string FieldTitle = $"{(module.Group != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(module.Group) : module.Name.Replace("Commands", ""))} Commands";
                            if (Silver.SQL.GetGuild(Context.Guild) != null && !string.IsNullOrWhiteSpace(module.Group))
                            {
                                FieldTitle += " ( ";
                                if (Silver.SQL.GetGuild(Context.Guild) != null)
                                    FieldTitle += Silver.SQL.GetGuild(Context.Guild).Prefix + string.Join($", {Silver.SQL.GetGuild(Context.Guild).Prefix}", module.Aliases);
                                FieldTitle += " )";
                            }
                                
                            helpEmbed.AddField(FieldTitle, $"{module.Summary}\n`{moduleinfo.Substring(0, moduleinfo.Length - 2)}`");
                        }
                    }
                }
            }
            else
            {
                //eventually add modules to the search too, they'll take priorty over the commands
                foreach (CommandInfo cmd in cmdsrv.Commands.Where(x => x.Aliases.Any(y => y.Contains(search.ToLowerInvariant()))).Take(5))
                {
                    if (cmd.CheckPreconditionsAsync(Context, null).Result.IsSuccess)
                    {
                        string SearchTitle = "";
                        if (Silver.SQL.GetGuild(Context.Guild) != null)
                        {
                            string Aliases = string.Join($", {Silver.SQL.GetGuild(Context.Guild).Prefix}", cmd.Aliases.Take(cmd.Aliases.Count / cmd.Module.Aliases.Count).Skip(1));
                            SearchTitle += $"{Silver.SQL.GetGuild(Context.Guild).Prefix}{cmd.Aliases[0]}{(string.IsNullOrWhiteSpace(Aliases) ? "" : $" ( {Silver.SQL.GetGuild(Context.Guild).Prefix}{Aliases} )")}";
                        }
                        string CommandParameters = $"```csharp\n";
                        List<string> fields = new List<string>();
                        foreach (var field in cmd.Parameters)
                            fields.Add($"{(field.IsRemainder ? "[Remainder] " : "")}{field.Type.ToString().Split('.').LastOrDefault()} {field.Name}{(field.IsOptional ? $" = {(field.DefaultValue == null ? "null" : "")}" : "")}{(field.Summary != null ? $"\n({field.Summary})" : "")}");
                        CommandParameters += $"{string.Join(", ", fields)}```";
                        helpEmbed.AddField(SearchTitle, $"{cmd.Summary}\n{CommandParameters}");
                    }
                }
            }
            await ReplyAsync(null, false, helpEmbed.Build());
        }

        [Command("emoteecho")]
        public async Task MiscEmoteEcho()
        {
            IUserMessage msg = await ReplyAsync("add a react and this message will be edited to the react");
            async Task onReact(Cacheable<IUserMessage, ulong> cachemsg, ISocketMessageChannel chnl, SocketReaction react)
            {
                if (react.UserId == Context.Message.Author.Id)
                    await msg.ModifyAsync(x => x.Content = $"ok here: {react.Emote.ToString()}");
                Context.Client.ReactionAdded -= onReact;
            };
            Context.Client.ReactionAdded += onReact;
        }

        [Command("prefix", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageGuild, Group = "OwnerOrManager")]
        [RequireConfigAdmin(Group = "OwnerOrManager")]
        [RequireContext(ContextType.Guild)]
        [RequireDatabaseConnection]
        [Summary("Sets the prefix for this guild. Bot admins and guild managers only.")]
        public async Task MiscPrefix([Remainder] string prefix)
        {
            if (Silver.SQL.GetGuild(Context.Guild) != null)
            {
                Silver.SQL.GetGuild(Context.Guild).Prefix = prefix;
                Silver.SQL.SaveChanges();
                await ReplyAsync($"Done! From now on, use `{prefix}` to run commands in this guild.");
            }
            else
                await ReplyAsync("Error! Guild not found in database. This should never happen, let the owner know.");
        }
    }
}
