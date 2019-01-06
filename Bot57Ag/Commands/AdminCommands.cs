using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Bot57Ag.Preconditions;

namespace Bot57Ag.Commands
{
    [RequireConfigAdmin]
    [Summary("Commands only the bot admins can run.\n")]
    public class AdminCommands : ModuleBase<SocketCommandContext>
    {
        [Command("logoff", RunMode = RunMode.Async)]
        [Alias("logoff", "shutdown", "quit")]
        public async Task AdminLogoff()
        {
            IUserMessage death = await ReplyAsync("ok bye");
            await Task.Delay(1500);
            await death.DeleteAsync();
            await Context.Client.SetStatusAsync(UserStatus.Offline);
            await Context.Client.LogoutAsync();
            await Task.Delay(500);
            Environment.Exit(0);
        }

        [Group("guild")]
        [Summary("Controls various guild options. Leaving, listing, etc.")]
        public class AdminGuildCommands : ModuleBase<SocketCommandContext>
        {
            [Command("gimme", RunMode = RunMode.Async)]
            public async Task AdminGuildGimme()
            {
                string tosend = "Guilds I'm In (A Guided Tour):";
                foreach (SocketGuild guild in Context.Client.Guilds)
                    tosend += $"\n{guild.Name} - `{guild.Id}`";
                await ReplyAsync(tosend);
            }

            [Command("leave", RunMode = RunMode.Async)]
            public async Task AdminGuildLeave(ulong guildid)
            {
                SocketGuild guild = Context.Client.GetGuild(guildid);
                if (guild == null)
                    await ReplyAsync("Guild not found.");
                else
                {
                    IUserMessage msg = await ReplyAsync($"Leaving {guild.Name}, is this correct? React with any emote.");
                    async Task onReact(Cacheable<IUserMessage, ulong> cachemsg, ISocketMessageChannel chnl, SocketReaction react)
                    {
                        RestApplication application = await Context.Client.GetApplicationInfoAsync();
                        if (react.UserId == application.Owner.Id)
                        {
                            await msg.ModifyAsync(x => x.Content = $"Leaving {guild.Name}.");
                            await Task.Delay(8000);
                            await Context.Client.GetGuild(guild.Id).LeaveAsync();
                        }
                        await msg.DeleteAsync();
                        Context.Client.ReactionAdded -= onReact;
                    };
                    Context.Client.ReactionAdded += onReact;
                }
            }
        }
    }
}
