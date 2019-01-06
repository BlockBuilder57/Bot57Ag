using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using Bot57Ag.Preconditions;

namespace Bot57Ag.Commands
{
    [Group("funbucks")]
    [Alias("fb")]
    [Summary("Manage your Fun Bucks here!\n")]
    public class FunBucksCommands : ModuleBase<SocketCommandContext>
    {
        [Command("register")]
        [Summary("Register for a Fun Bucks account.\n")]
        public async Task FunBucksRegister()
        {
            using (SQLContext sql = new SQLContext())
            {
                if (sql.GetUser(Context.User) == null)
                {
                    sql.Users.Add(new SQLUser
                    {
                        UserID = Context.User.Id.ToString(),
                        FunBucks = 0.00m,
                        FunBucksLastPaycheck = DateTimeOffset.Now
                    });
                    sql.SaveChanges();
                    await ReplyAsync($"Account ID `{Context.User.Id}` created.");
                }
                else
                    await ReplyAsync($"GENERIC ACCOUNT ALREADY EXISTS MESSAGE");
            }
        }

        [Command("balance")]
        [Alias("bal", "money")]
        [RequireFunBucksAccount]
        [Summary("Check your Fun Bucks balance.\n")]
        public async Task FunBucksBalance(SocketGuildUser usr = null)
        {
            if (usr == null)
                usr = Context.Guild.GetUser(Context.User.Id);
            using (SQLContext sql = new SQLContext())
            {
                if (sql.GetUser(usr) != null)
                    await ReplyAsync($"{(usr == Context.User ? "Your" : $"{usr.Nickname}'s")} balance is 🜛{sql.GetUser(usr).FunBucks.ToString("F2")}."); //🜛 is the alchemical symbol for silver
                else
                    await ReplyAsync("That user does not have an account.");
            }
        }

        [Command("paycheck")]
        [Alias("work", "earn")]
        [RequireFunBucksAccount]
        [Summary("Earn Fun Bucks! (Note: may require working in the salt mine)\n")]
        public async Task FunBucksPaycheck()
        {
            decimal PaycheckAmount = new Random().Next(920, 1875)/100m;
            using (SQLContext sql = new SQLContext())
            {
                TimeSpan untilCheck = DateTimeOffset.Now.Subtract(sql.GetUser(Context.User).FunBucksLastPaycheck + new TimeSpan(0, 0, 30));
                if (untilCheck.TotalSeconds >= 0)
                {
                    sql.GetUser(Context.User).FunBucks += PaycheckAmount;
                    sql.GetUser(Context.User).FunBucksLastPaycheck = DateTimeOffset.Now;
                    sql.SaveChanges();
                    await ReplyAsync($"🜛{PaycheckAmount.ToString("F2")} has been added to your account. (🜛{sql.GetUser(Context.User).FunBucks.ToString("F2")} total)");
                }
                else
                    await ReplyAsync($"You must wait {(int)untilCheck.Negate().TotalMinutes}m, {(int)untilCheck.Negate().Seconds}s until your next paycheck.");
            }
        }

        [Group("admin")]
        [RequireConfigAdmin]
        [Summary("AUTHORIZED PERSONELL ONLY\n")]
        public class FunBucksAdminCommands : ModuleBase<SocketCommandContext>
        {
            [Command("clearempties")]
            [Summary("Get rid of the stragglers.\n")]
            public async Task FunBucksAdminClearEmpties()
            {
                int RemovedUsers = 0;
                using (SQLContext sql = new SQLContext())
                {
                    foreach (SQLUser user in sql.Users)
                    {
                        if (user.FunBucks == 0.00m)
                        {
                            sql.Users.Remove(user);
                            RemovedUsers++;
                        }
                    }
                    sql.SaveChanges();
                }
                await ReplyAsync($"Removed **{RemovedUsers}** account(s).");
            }

            [Command("setbal")]
            [Summary("Cheat the system.\n")]
            public async Task FunBucksAdminSetBal(SocketGuildUser usr, decimal money)
            {
                using (SQLContext sql = new SQLContext())
                {
                    if (sql.GetUser(usr) != null)
                        sql.GetUser(usr).FunBucks = money;
                    sql.SaveChanges();
                }
                await ReplyAsync($"Set {usr.Nickname}'s Fun Bucks to 🜛{money.ToString("F2")}");
            }
        }
    }
}
