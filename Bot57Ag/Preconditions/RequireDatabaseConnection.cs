using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot57Ag.Preconditions
{
    public class RequireDatabaseConnection : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Silver.ConfigIndex == -1)
                return await Task.FromResult(PreconditionResult.FromError("Not connected to database."));
            else
                return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
