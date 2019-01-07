using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Bot57Ag.Preconditions
{
    public class RequireFunBucksAccount : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Silver.SQL.GetUser(context.User) != null)
                return await Task.FromResult(PreconditionResult.FromSuccess());
            else
            {
                if (Silver.SQL.GetGuild(context.Guild) != null && command.Module.Aliases.Contains(context.Message.Content.Remove(0, Silver.SQL.GetGuild(context.Guild).Prefix.Length).Split(' ')[0])) //bullshit non-fb-command protection
                    await context.Channel.SendMessageAsync($"You must have an account. Please register one by running `{Silver.SQL.GetGuild(context.Guild).Prefix}fb register`.");
                return await Task.FromResult(PreconditionResult.FromError("You must have an account."));
            }
        }
    }
}
