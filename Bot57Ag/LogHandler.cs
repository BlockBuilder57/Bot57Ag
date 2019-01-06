using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Bot57Ag
{
    public class LogHandler
    {
        public Task CustomLogger(LogMessage LogMsg)
        {
            Console.WriteLine(LogMsg.Message);
            return Task.CompletedTask;
        }
    }
}
