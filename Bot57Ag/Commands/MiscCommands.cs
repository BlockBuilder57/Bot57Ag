﻿using System;
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
        public async Task MiscPing()
        {
            await ReplyAsync($"hi, hello, i am here\ntook `~{Context.Client.Latency}ms`");
        }

        [Command("about")]
        public async Task MiscAbout()
        {
            EmbedBuilder aboutEmbed = Silver.Tools.GetStockEmbed();
            aboutEmbed.AddField("OS", System.Runtime.InteropServices.RuntimeInformation.OSDescription, false);
            aboutEmbed.AddField("Silver Version", Silver.VersionString, true);
            aboutEmbed.AddField("Discord.NET Version", typeof(DiscordConfig).GetTypeInfo().Assembly.GetName().Version.ToString(3), true);
            await ReplyAsync(null, false, aboutEmbed.Build());
        }

        [Command("help", RunMode = RunMode.Async)]
        public async Task MiscHelp()
        {
            EmbedBuilder helpEmbed = Silver.Tools.GetStockEmbed();
            foreach (ModuleInfo module in cmdsrv.Modules.OrderBy(x => !x.Name.Contains("Misc")))
            {
                if (!module.IsSubmodule)
                {
                    string moduleinfo = "";
                    foreach (CommandInfo cmd in module.Commands)
                    {
                        if (cmd.CheckPreconditionsAsync(Context, null).Result.IsSuccess)
                        {
                            string Alias = cmd.Aliases.Where(x => !x.Contains(cmd.Name)).FirstOrDefault();
                            moduleinfo += $"{cmd.Name}{(String.IsNullOrWhiteSpace(Alias) ? "" : $" ({Alias.Split(' ').LastOrDefault()})")}, ";
                        }
                    }
                    foreach (ModuleInfo submod in module.Submodules)
                    {
                        if (submod.GetExecutableCommandsAsync(Context, null).Result.Count > 0)
                        {
                            string Alias = submod.Aliases.Where(x => !x.Contains(submod.Name)).FirstOrDefault();
                            moduleinfo += $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(submod.Name)} Cmds{(String.IsNullOrWhiteSpace(Alias) ? "" : $" ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Alias.Split(' ').LastOrDefault())})")}, ";
                        }
                    }
                    if (module.GetExecutableCommandsAsync(Context, null).Result.Count > 0)
                    {
                        string FieldTitle = $"{(module.Group != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(module.Group) : module.Name.Replace("Commands", ""))} Commands";
                        if (Silver.SQL.GetGuild(Context.Guild) != null && !string.IsNullOrWhiteSpace(module.Group))
                            FieldTitle += $" ( {Silver.SQL.GetGuild(Context.Guild).Prefix}{module.Group} )";
                        helpEmbed.AddField(FieldTitle, $"{module.Summary}`{moduleinfo.Substring(0, moduleinfo.Length-2)}`");
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
        [RequireUserPermission(GuildPermission.Administrator, Group = "OwnerOrManager")]
        [RequireConfigAdmin(Group = "OwnerOrManager")]
        [RequireContext(ContextType.Guild)]
        [RequireDatabaseConnection]
        [Summary("Sets the prefix for this guild. Admins only.")]
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
