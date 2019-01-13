using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Discord;
using Npgsql;
using Discord.WebSocket;

namespace Bot57Ag
{
    public class SQLContext : DbContext
    {
        public DbSet<SQLConfig> Configs { get; set; }
        public DbSet<SQLGuild> Guilds { get; set; }
        public DbSet<SQLUser> Users { get; set; }

        public NpgsqlConnection connection;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SQLGuild>().HasKey(x => new { x.GuildId, x.ConfigId });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databasepass = "";
            if (File.Exists(Silver.PathString + "dbpass.txt"))
                databasepass = File.ReadAllText(Silver.PathString + "dbpass.txt");
            else
            {
                Console.WriteLine("Enter the database password please. It will be saved to dbpass.txt once you have entered it.");
                databasepass = Console.ReadLine();
                File.WriteAllText(Silver.PathString + "dbpass.txt", databasepass);
            }
            connection = new NpgsqlConnection($"Host=localhost;Database=Bot57Ag;Username=Bot57Ag;Password={databasepass}");
            connection.StateChange += StateChange;
            optionsBuilder.UseNpgsql(connection);
        }

        public bool HasConnected = false;

        private void StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if (!(e.CurrentState == System.Data.ConnectionState.Closed || e.CurrentState == System.Data.ConnectionState.Broken))
                HasConnected = true;
        }

        public override int SaveChanges()
        {
            if (Silver.ConfigIndex == -1)
                return 0;
            else
                return base.SaveChanges();
        }

        public static SQLConfig NoConfigSQLConfig;
        private bool NoConfigSetup_Ran = false;

        public void NoConfigSetup(string token, string prefix, string[] ids)
        {
            if (!NoConfigSetup_Ran)
            {
                NoConfigSQLConfig = new SQLConfig
                {
                    Id = -1,
                    Token = token,
                    PrefixDefault = prefix,
                    AdminIds = ids
                };
                NoConfigSetup_Ran = true;
            }
        }

        /*public SQLConfig GetConfig(int id)
        {
            return Silver.ConfigIndex == -1 ? NoConfigSQLConfig : Configs.Find(id + 1);
        }*/

        public SQLConfig GetCurConfig()
        {
            return Silver.ConfigIndex == -1 ? NoConfigSQLConfig : Configs.Find(Silver.ConfigIndex + 1);
        }

        public SQLUser GetUser(IUser usr)
        {
            return Silver.ConfigIndex == -1 ? null : Users.Find(usr.Id.ToString());
        }

        public string GetUserPrefName(IUser usr, bool WithUsername = false)
        {
            string PrefName = usr.Username;
            if (usr is SocketGuildUser sgu && sgu.Nickname != null)
                PrefName = sgu.Nickname;
            if (Users.Find(usr.Id.ToString()) != null && Users.Find(usr.Id.ToString()).Nickname != null)
                PrefName = Users.Find(usr.Id.ToString()).Nickname;
            if (WithUsername && PrefName != usr.Username)
                PrefName += $" ({usr.Username})";
            return PrefName.Replace("@", "@\u200B");
        }

        public SQLGuild GetGuild(IGuild guild)
        {
            return Silver.ConfigIndex == -1 ? null : Guilds.Find(new object[] { guild.Id.ToString(), Silver.ConfigIndex });
        }
    }

    //refer to this https://www.npgsql.org/doc/types/basic.html for datatypes

    public class SQLConfig
    {
        [Key]
        public int Id { get; set; }
        public string Token { get; set; }
        public string PrefixDefault { get; set; }
        public string[] AdminIds { get; set; }
    }

    public class SQLGuild
    {
        [Key]
        public string GuildId { get; set; }
        [Key]
        public int ConfigId { get; set; }
        public string Prefix { get; set; }
        public bool DropFunBucks { get; set; }
    }

    public class SQLUser
    {
        [Key]
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public decimal FunBucks { get; set; }
        public DateTimeOffset FunBucksLastPaycheck { get; set; }
    }
}
