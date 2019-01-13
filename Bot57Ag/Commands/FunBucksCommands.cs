using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Threading.Tasks;
using Bot57Ag.Preconditions;

namespace Bot57Ag.Commands
{
    [Group("funbucks")]
    [Alias("fb")]
    [Summary("Manage your Fun Bucks here!")]
    [RequireDatabaseConnection]
    public class FunBucksCommands : ModuleBase<SocketCommandContext>
    {
        [Command("register")]
        [Summary("Register for a Fun Bucks account.")]
        public async Task FunBucksRegister()
        {
            if (Silver.SQL.GetUser(Context.User) == null)
            {
                Silver.SQL.Users.Add(new SQLUser
                {
                    UserId = Context.User.Id.ToString(),
                    FunBucks = 0.00m,
                    FunBucksLastPaycheck = DateTimeOffset.Now
                });
                Silver.SQL.SaveChanges();
                await ReplyAsync($"Account ID `{Context.User.Id}` created.");
            }
            else
                await ReplyAsync($"You already have an account.");
        }

        [Command("deleteaccount")]
        [Summary("Delete your Fun Bucks account. :(")]
        public async Task FunBucksDeleteAccount()
        {
            IUserMessage msg = await ReplyAsync("Are you sure you want to delete your account? You'll lose all of your Fun Bucks!\nIf you do, react to this message.");
            async Task onReact(Cacheable<IUserMessage, ulong> cachemsg, ISocketMessageChannel chnl, SocketReaction react)
            {
                if (react.UserId == Context.User.Id)
                {
                    if (Silver.SQL.GetUser(Context.User) != null)
                        Silver.SQL.Users.Remove(Silver.SQL.GetUser(Context.User));
                    Silver.SQL.SaveChanges();
                    await msg.ModifyAsync(x => x.Content = $"Your account has been deleted. Sorry to see you go! :(");
                }
                Context.Client.ReactionAdded -= onReact;
            };
            Context.Client.ReactionAdded += onReact;
        }

        [Command("leaderboard", RunMode = RunMode.Async)]
        [RequireFunBucksAccount]
        [Summary("View the Fun Bucks leaderboards!")]
        public async Task FunBucksLeaderboards()
        {
            string LeaderboardString = "";
            SQLUser[] UserList = Silver.SQL.Users.OrderByDescending(x => x.FunBucks).Take(10).ToArray();
            int maxmoneysize = 0;
            for (int i = 0; i < UserList.Length; i++)
            {
                SQLUser sqlusr = UserList[i];
                IUser usr = Context.Client.GetUser(ulong.Parse(sqlusr.UserId));
                if (sqlusr.FunBucks.ToString().Length + 1 > maxmoneysize)
                    maxmoneysize = sqlusr.FunBucks.ToString().Length + 2;
                LeaderboardString += $"{i+1}. `{("🜛" + sqlusr.FunBucks.ToString()).PadLeft(maxmoneysize)}` - {Silver.SQL.GetUserPrefName(usr, true)}\n";
            }
            EmbedBuilder tester = Silver.Tools.GetStockEmbed();
            tester.Description = LeaderboardString;
            await ReplyAsync(null, false, tester.Build());
        }

        [Command("balance")]
        [Alias("bal", "money")]
        [RequireFunBucksAccount]
        [Summary("Check your Fun Bucks balance.")]
        public async Task FunBucksBalance(SocketGuildUser usr = null)
        {
            if (usr == null)
                usr = Context.Guild.GetUser(Context.User.Id);
            if (Silver.SQL.GetUser(usr) != null)
                await ReplyAsync($"{(usr == Context.User ? "Your" : $"{usr.Nickname}'s")} balance is 🜛{Silver.SQL.GetUser(usr).FunBucks.ToString("F2")}."); //🜛 is the alchemical symbol for silver
            else
                await ReplyAsync();
        }

        [Command("paycheck")]
        [Alias("work", "earn")]
        [RequireFunBucksAccount]
        [Summary("Earn Fun Bucks! (Note: may require working in the salt mine)")]
        public async Task FunBucksPaycheck()
        {
            decimal PaycheckAmount = new Random().Next(920, 1875)/100m;
            TimeSpan untilCheck = DateTimeOffset.Now.Subtract(Silver.SQL.GetUser(Context.User).FunBucksLastPaycheck + new TimeSpan(0, 0, 30));
            if (untilCheck.TotalSeconds >= 0)
            {
                Silver.SQL.GetUser(Context.User).FunBucks += PaycheckAmount;
                Silver.SQL.GetUser(Context.User).FunBucksLastPaycheck = DateTimeOffset.Now;
                Silver.SQL.SaveChanges();
                await ReplyAsync($"🜛{PaycheckAmount.ToString("F2")} has been added to your account. (🜛{Silver.SQL.GetUser(Context.User).FunBucks.ToString("F2")} total)");
            }
            else
                await ReplyAsync($"You must wait {(int)untilCheck.Negate().TotalMinutes}m, {(int)untilCheck.Negate().Seconds}s until your next paycheck.");
        }

        [Command("transfer")]
        [RequireFunBucksAccount]
        [Summary("Transfer funds to another user's account.")]
        public async Task FunBucksTransfer(decimal amount, IUser usr = null)
        {
            if (Silver.SQL.GetUser(usr) != null)
            {
                SQLUser totransfer = Silver.SQL.GetUser(usr);
                SQLUser fromtransfer = Silver.SQL.GetUser(Context.User);

                if (!(fromtransfer.FunBucks < amount))
                {
                    fromtransfer.FunBucks -= amount;
                    totransfer.FunBucks += amount;
                    await ReplyAsync($"{usr.Mention} has recieved 🜛{amount.ToString("F2")}.");
                }

                Silver.SQL.SaveChanges();
            }
            else
            {
                if (Silver.Tools.JSON["NoFunBucksAccount"] != null)
                    await ReplyAsync(Silver.Tools.JSON["NoFunBucksAccount"].ToString());
            }
        }

        [Command("nickname", RunMode = RunMode.Async)]
        [RequireDatabaseConnection]
        [RequireFunBucksAccount]
        [Summary("Sets your nickname used by the bot.")]
        public async Task MiscNickname(string UserTest = null, [Remainder] string UserNick = null)
        {
            TypeReaderResult usr_result = await new UserTypeReader<IUser>().ReadAsync(Context, Context.Message.Content, null);
            IUser usr = Context.User;
            if (usr_result.IsSuccess && Silver.SQL.GetCurConfig().AdminIds.Contains(Context.User.Id.ToString()))
                usr = (IUser)usr_result.BestMatch;

            if (Silver.SQL.GetUser(usr) != null)
            {
                if (string.IsNullOrWhiteSpace(UserTest))
                {
                    if (Silver.SQL.GetUser(usr).Nickname != null)
                        await ReplyAsync($"Your nickname is `{Silver.SQL.GetUser(usr).Nickname.Replace("@", "@\u200B")}`.");
                    else
                        await ReplyAsync($"You don't have a nickname.");
                }
                else
                {
                    string fixednick = $"{UserTest}{(String.IsNullOrEmpty(UserNick) ? "" : $" {UserNick}")}";
                    fixednick = fixednick.Substring(0, Math.Clamp(fixednick.Length, 0, 32)).Replace("@", "@\u200B");

                    if (fixednick.ToLower() == "clear")
                    {
                        Silver.SQL.GetUser(usr).Nickname = null;
                        Silver.SQL.SaveChanges();
                        IUserMessage msg = await ReplyAsync("Done! Your nickname has been deleted.\nIf you wanted you name to actually be \"clear\", react to this message with any emote.");
                        async Task onReact(Cacheable<IUserMessage, ulong> cachemsg, ISocketMessageChannel chnl, SocketReaction react)
                        {
                            if (react.UserId == usr.Id)
                            {
                                Silver.SQL.GetUser(usr).Nickname = "clear";
                                Silver.SQL.SaveChanges();
                                await msg.ModifyAsync(x => x.Content = $"Done! Your nickname is now `{fixednick}`.");
                            }
                            Context.Client.ReactionAdded -= onReact;
                        };
                        Context.Client.ReactionAdded += onReact;
                    }
                    else
                    {
                        Silver.SQL.GetUser(usr).Nickname = fixednick;
                        Silver.SQL.SaveChanges();
                        await ReplyAsync($"Done! Your nickname is now `{fixednick}`.");
                    }
                }
            }
        }

        [Group("admin")]
        [RequireConfigAdmin]
        [Summary("AUTHORIZED PERSONELL ONLY")]
        public class FunBucksAdminCommands : ModuleBase<SocketCommandContext>
        {
            [Command("clearempties")]
            [Summary("Get rid of the stragglers.")]
            public async Task FunBucksAdminClearEmpties()
            {
                int RemovedUsers = 0;
                foreach (SQLUser user in Silver.SQL.Users)
                {
                    if (user.FunBucks == 0.00m)
                    {
                        Silver.SQL.Users.Remove(user);
                        RemovedUsers++;
                    }
                }
                Silver.SQL.SaveChanges();
                await ReplyAsync($"Removed **{RemovedUsers}** account(s).");
            }

            [Command("forceregister")]
            [Summary("Forcibly registers a user.")]
            public async Task FunBucksAdminForceRegister(IUser usr)
            {
                if (Silver.SQL.GetUser(usr) == null)
                {
                    Silver.SQL.Users.Add(new SQLUser
                    {
                        UserId = usr.Id.ToString(),
                        FunBucks = 0.00m,
                        FunBucksLastPaycheck = DateTimeOffset.Now
                    });
                    Silver.SQL.SaveChanges();
                    await ReplyAsync($"Account ID `{usr.Id}` created.");
                }
                else
                    await ReplyAsync($"That user already has an account.");
            }

            [Command("deleteaccount")]
            [Summary("Delete an account.")]
            public async Task FunBucksAdminDeleteAccount(IUser usr)
            {
                if (Silver.SQL.GetUser(usr) != null)
                    Silver.SQL.Users.Remove(Silver.SQL.GetUser(usr));
                Silver.SQL.SaveChanges();
                await ReplyAsync($"Removed ${usr.Username}'s account.");
            }

            [Command("setbal")]
            [Summary("Cheat the system.")]
            public async Task FunBucksAdminSetBal(IUser usr, decimal money)
            {
                if (Silver.SQL.GetUser(usr) != null)
                    Silver.SQL.GetUser(usr).FunBucks = money;
                Silver.SQL.SaveChanges();
                await ReplyAsync($"Set {Silver.SQL.GetUserPrefName(Context.Guild.GetUser(usr.Id))}'s Fun Bucks to 🜛{money.ToString("F2")}");
            }

            [Command("setnick")]
            [Summary("Recondition the populous.")]
            public async Task FunBucksAdminSetNickname(IUser usr, [Remainder] string nickname)
            {
                if (Silver.SQL.GetUser(usr) != null)
                {
                    Silver.SQL.GetUser(usr).Nickname = nickname;
                    Silver.SQL.SaveChanges();
                    await ReplyAsync($"Set {Silver.SQL.GetUserPrefName(Context.Guild.GetUser(usr.Id))}'s nickname.");
                }
            }
        }
    }
}
