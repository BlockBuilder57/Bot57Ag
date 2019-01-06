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
            if (!(msg is SocketUserMessage message))
                return;

            int argPos = 0;

            string prefix = "☺"; //please never use this
            using (SQLContext sql = new SQLContext())
            {
                if (!(sql.Config.Count() > 0))
                    prefix = sql.Config.ToArray()[Silver.ConfigIndex].PrefixDefault;
                if (message.Channel is SocketGuildChannel sgc)
                    if (sql.Guilds.Find(sgc.Guild.Id.ToString()) != null && sql.Guilds.Find(sgc.Guild.Id.ToString()).Prefix != null)
                        prefix = sql.Guilds.Find(((SocketGuildChannel)message.Channel).Guild.Id.ToString()).Prefix;
            }

            if (!message.HasStringPrefix(prefix, ref argPos))
                return;

            SocketCommandContext context = new SocketCommandContext(dsc, message);

            IResult result = await cmdsrv.ExecuteAsync(context, argPos, null);

            if (!result.IsSuccess)
                await new LogHandler().CustomLogger(new LogMessage(LogSeverity.Verbose, result.Error.ToString(), result.ErrorReason));
        }
    }
}
