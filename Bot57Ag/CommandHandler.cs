using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Reflection;

namespace Bot57Ag
{
    class CommandHandler
    {
        private readonly DiscordSocketClient dsc;
        private readonly CommandService cmdsrv;

        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            dsc = client;
            cmdsrv = commands;
        }

        public async Task InstallCommandsAsync()
        {
            dsc.MessageReceived += HandleCommandAsync;

            await cmdsrv.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        private async Task HandleCommandAsync(SocketMessage msg)
        {
            SocketUserMessage message = msg as SocketUserMessage;
            if (message == null)
                return;

            int argPos = 0;

            string prefix = "☺"; //please never show this
            using (SQLContext sqlcontext = new SQLContext())
            {
                if (!(sqlcontext.Config.Count() > 0))
                    prefix = sqlcontext.Config.First().PrefixDefault;
                if (message.Channel is SocketGuildChannel sgc)
                    if (sqlcontext.Guilds.Find(sgc.Guild.Id.ToString()) != null && sqlcontext.Guilds.Find(sgc.Guild.Id.ToString()).Prefix != null)
                        prefix = sqlcontext.Guilds.Find(((SocketGuildChannel)message.Channel).Guild.Id.ToString()).Prefix;
            }

            if (!message.HasStringPrefix(prefix, ref argPos))
                return;

            SocketCommandContext context = new SocketCommandContext(dsc, message);

            IResult result = await cmdsrv.ExecuteAsync(context, argPos, null);
        }
    }
}
