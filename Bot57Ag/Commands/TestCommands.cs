using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;

namespace Bot57Ag.Commands
{
    public class TestCommands : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task TestPing()
        {
            await ReplyAsync($"hi, hello, i am here\ntook `~{Context.Client.Latency}ms`");
        }

        [Command("logoff", RunMode = RunMode.Async)]
        [Alias("logoff", "shutdown", "quit")]
        [RequireOwner]
        public async Task TestLogoff()
        {
            IUserMessage death = await ReplyAsync("ok bye");
            await Task.Delay(1500);
            await death.DeleteAsync();
            await Context.Client.SetStatusAsync(UserStatus.Offline);
            await Context.Client.LogoutAsync();
            await Task.Delay(500);
            Environment.Exit(0);
        }

        [Command("gimmeguilds", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task TestGimmeGuilds()
        {
            string tosend = "Servers I'm In (A Guided Tour):";
            foreach (SocketGuild guild in Context.Client.Guilds)
                tosend += $"\n{guild.Name} - `{guild.Id}`";
            await ReplyAsync(tosend);
        }

        [Command("leaveguild", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task TestGimmeGuilds(ulong guildid)
        {
            SocketGuild guild = Context.Client.GetGuild(guildid);
            if (guild == null)
                await ReplyAsync("Guild not found.");
            else
            {
                IUserMessage msg = await ReplyAsync($"Leaving {guild.Name}, is this correct? React within 5 seconds.");
                await Task.Delay(5000);
                Discord.Rest.RestApplication application = await Context.Client.GetApplicationInfoAsync();
                IUser reactor = msg.GetReactionUsersAsync(new Emoji("✅"), 1).First().Result.First();
                Console.WriteLine(reactor.Id);
                Console.WriteLine(application.Owner.Id);
                if (reactor.Id == application.Owner.Id)
                {
                    await msg.ModifyAsync(x => x.Content = $"Leaving {guild.Name}.");
                    await Task.Delay(8000);
                    await Context.Client.GetGuild(guild.Id).LeaveAsync();
                }
                await msg.DeleteAsync();
            }
        }
    }
}
