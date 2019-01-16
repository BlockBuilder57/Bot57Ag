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
using System.Diagnostics;

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

        [Command("status", RunMode = RunMode.Async)]
        [Alias("perf")]
        [Summary("Checks versions and other various settings.")]
        public async Task MiscStatus()
        {
            EmbedBuilder statusEmbed = Silver.Tools.GetStockEmbed("Status");
            Process curproc = Process.GetCurrentProcess();
            statusEmbed.AddField("OS", System.Runtime.InteropServices.RuntimeInformation.OSDescription, false);
            statusEmbed.AddField("Uptime", (DateTime.Now - curproc.StartTime).ToString(@"%d\:hh\:mm\:ss"), true);
            statusEmbed.AddField("Memory usage", $"Private: {Silver.Tools.ToSize(curproc.PrivateMemorySize64, Silver.Tools.SizeUnits.MB)}, Post GC: {Silver.Tools.ToSize(GC.GetTotalMemory(true), Silver.Tools.SizeUnits.MB)}", true);
            statusEmbed.AddField("Silver Version", Silver.VersionString, true);
            statusEmbed.AddField("Discord.NET Version", typeof(DiscordConfig).GetTypeInfo().Assembly.GetName().Version.ToString(3), true);
            curproc.Dispose();
            await ReplyAsync(null, false, statusEmbed.Build());
        }

        [Command("credits")]
        [Alias("about")]
        [Summary("Who hurt you little ~~MacBook~~ bot?")]
        public async Task MiscAbout()
        {
            EmbedBuilder creditsEmbed = Silver.Tools.GetStockEmbed("Credits");
            creditsEmbed.Description = "(psst! click on the above link to see the source code!)";
            string people = "";
            people += "<@120398901927739393> - #1 Dad (creator)\n";
            people += "<@99801098088370176> - Idea stealee\n";
            people += "<@277916164661968896> - Good code derg\n";
            people += "<@141011672826511360> - Good code cat\n";
            people += "<@196823721938386944> - Sends me shitty memes until I cry\n";
            people += "Everyone in Gaia - Cool people who have given me experiences i never would have had otherwise";
            creditsEmbed.AddField("People", people);
            string code = "";
            code += "[Discord.Net](https://github.com/RogueException/Discord.Net) - The library I use for connecting and interacting with the Discord API\n";
            code += "[PostgreSQL](https://www.postgresql.org/) - Provides a SQL server for data storage\n";
            code += "[Npgsql](https://www.npgsql.org/) - Lets my code interact with a PostgreSQL server\n";
            code += "[GitInfo](https://github.com/kzu/GitInfo) - Interacts with Git to get info like commit id and tag name\n";
            creditsEmbed.AddField("Code Libraries", code);
            await ReplyAsync(null, false, creditsEmbed.Build());
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("M E T A")]
        public async Task MiscHelp([Remainder] string search = null)
        {
            EmbedBuilder helpEmbed = Silver.Tools.GetStockEmbed(string.IsNullOrWhiteSpace(search) ? "Help" : $"Results for {search}");
            if (string.IsNullOrWhiteSpace(search))
            {
                foreach (ModuleInfo module in cmdsrv.Modules.OrderBy(x => !x.Name.Contains("Misc")))
                    if (!module.IsSubmodule && MiscHelp_ModulePrinter(module) != null)
                        helpEmbed.AddField(MiscHelp_ModulePrinter(module));
            }
            else
            {
                IEnumerable<ModuleInfo> searchModules = cmdsrv.Modules.Where(x => x.Aliases.Any(y => y.Contains(search.ToLowerInvariant())));
                IEnumerable<CommandInfo> searchCommands = cmdsrv.Commands.Where(x => x.Aliases.Any(y => y.Contains(search.ToLowerInvariant())));
                foreach (ModuleInfo module in searchModules.Take(3))
                    if (MiscHelp_ModulePrinter(module) != null)
                        helpEmbed.AddField(MiscHelp_ModulePrinter(module));

                foreach (CommandInfo cmd in searchCommands.Take(3))
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
                        foreach (Discord.Commands.ParameterInfo field in cmd.Parameters)
                            CommandParameters += $"{(field.IsRemainder ? "[Remainder] " : "")}{field.Type.ToString().Split('.').LastOrDefault()} {field.Name}{(field.IsOptional ? $" = {(field.DefaultValue ?? "null")}" : "")}{(field.Summary != null ? $"\n({field.Summary})" : "")}, ";
                        CommandParameters = CommandParameters.Remove(CommandParameters.Length - 2, 2) + "```";

                        helpEmbed.AddField(SearchTitle, $"{cmd.Summary}{(cmd.Parameters.Count > 0 ? "\n" + CommandParameters : "")}");
                    }
                }
                if (helpEmbed.Fields.Count == 0)
                    helpEmbed.Description = "No results found.";
                else
                    helpEmbed.Description = $"{searchModules.Count() + searchCommands.Count()} result(s) found ({searchModules.Count()} module(s), {searchCommands.Count()} command(s)), displaying {helpEmbed.Fields.Count}";
            }
            await ReplyAsync(null, false, helpEmbed.Build());
        }

        private EmbedFieldBuilder MiscHelp_ModulePrinter(ModuleInfo module)
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
                    moduleinfo += $"{cmd.Name}{(string.IsNullOrWhiteSpace(Alias) ? "" : $" ({Alias})")}, ";
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
                    moduleinfo += $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(submod.Name)} Cmds{(String.IsNullOrWhiteSpace(Alias) ? "" : $" ({Alias})")}, ";
                }
            }
            if (module.GetExecutableCommandsAsync(Context, null).Result.Count > 0)
            {
                string FieldTitle = $"{(module.Group != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(module.Group) : module.Name.Replace("Commands", ""))} Commands";
                if (Silver.SQL.GetGuild(Context.Guild) != null && !string.IsNullOrWhiteSpace(module.Group))
                {
                    FieldTitle += " ( ";
                    if (Silver.SQL.GetGuild(Context.Guild) != null)
                        FieldTitle += Silver.SQL.GetGuild(Context.Guild).Prefix + string.Join($", {Silver.SQL.GetGuild(Context.Guild).Prefix}", (module.IsSubmodule ? module.Aliases.Take(module.Aliases.Count / module.Parent.Aliases.Count) : module.Aliases));
                    FieldTitle += " )";
                }

                return new EmbedFieldBuilder
                {
                    Name = FieldTitle,
                    Value = $"{module.Summary}\n`{moduleinfo.Substring(0, moduleinfo.Length - 2)}`"
                };
            }
            return null;
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
