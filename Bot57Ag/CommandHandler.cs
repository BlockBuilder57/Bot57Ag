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
            if (Silver.SQL.GetCurConfig() != null)
                prefix = Silver.SQL.GetCurConfig().PrefixDefault;
            if (message.Channel is SocketGuildChannel sgc)
                if (Silver.SQL.GetGuild(sgc.Guild) != null && Silver.SQL.GetGuild(sgc.Guild).Prefix != null)
                    prefix = Silver.SQL.GetGuild(sgc.Guild).Prefix;

            if (!message.HasStringPrefix(prefix, ref argPos))
                return;

            SocketCommandContext context = new SocketCommandContext(dsc, message);

            IResult result = await cmdsrv.ExecuteAsync(context, argPos, null);

            if (!result.IsSuccess)
                await new LogHandler().CustomLogger(new LogMessage(LogSeverity.Verbose, result.Error.ToString(), result.ErrorReason));
        }
    }
}
