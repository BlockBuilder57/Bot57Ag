using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Discord;

namespace Bot57Ag
{
    public class SQLContext : DbContext
    {
        public DbSet<SQLConfig> Configs { get; set; }
        public DbSet<SQLGuild> Guilds { get; set; }
        public DbSet<SQLUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SQLGuild>().HasKey(x => new { x.ConfigId, x.GuildId });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databasepass = "";
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\dbpass.txt"))
                databasepass = File.ReadAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\dbpass.txt");
            else
            {
                Console.WriteLine("Enter the database password please. It will be saved to dbpass.txt once you have entered it.");
                databasepass = Console.ReadLine();
                File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\dbpass.txt", databasepass);
            }
            optionsBuilder.UseNpgsql($"Host=localhost;Database=Bot57Ag;Username=Bot57Ag;Password={databasepass}");
        }

        public SQLConfig GetConfig(int id)
        {
            return Configs.Find(id);
        }

        public SQLUser GetUser(IUser usr)
        {
            return Users.Find(usr.Id.ToString());
        }

        public SQLUser GetUser(ulong id)
        {
            return Users.Find(id.ToString());
        }

        public SQLUser GetUser(string id)
        {
            return Users.Find(id);
        }

        public SQLGuild GetGuild(IGuild guild)
        {
            return Guilds.Find(new object[] { guild.Id.ToString(), Silver.ConfigIndex + 1 });
        }

        public SQLGuild GetGuild(ulong id)
        {
            return Guilds.Find(new object[] { id.ToString(), Silver.ConfigIndex + 1 });
        }

        public SQLGuild GetGuild(string id)
        {
            return Guilds.Find(new object[] { id, Silver.ConfigIndex + 1 });
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
        public int ConfigId { get; set; }
        [Key]
        public string GuildId { get; set; }
        public string Prefix { get; set; }
        public bool DropFunBucks { get; set; }
    }

    public class SQLUser
    {
        [Key]
        public string UserId { get; set; }
        public decimal FunBucks { get; set; }
        public DateTimeOffset FunBucksLastPaycheck { get; set; }
    }
}
