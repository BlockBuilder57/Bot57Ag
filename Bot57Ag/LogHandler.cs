using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Bot57Ag
{
    class LogHandler
    {
        public Task LogLevel_Warnings(LogMessage LogMsg)
        {
            Console.WriteLine(LogMsg.Message);
            return Task.CompletedTask;
        }
    }
}
