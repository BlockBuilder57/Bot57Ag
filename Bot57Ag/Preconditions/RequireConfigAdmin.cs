using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot57Ag.Preconditions
{
    public class RequireConfigAdmin : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Silver.SQL.GetCurConfig().AdminIds.Contains(context.User.Id.ToString()))
                return await Task.FromResult(PreconditionResult.FromSuccess());
            else
                return await Task.FromResult(PreconditionResult.FromError("User is not a bot admin."));
        }
    }
}
