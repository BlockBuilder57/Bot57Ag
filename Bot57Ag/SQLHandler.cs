using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Bot57Ag
{
    public class SQLContext : DbContext
    {
        public DbSet<SQLConfig> Config { get; set; }
        public DbSet<SQLGuild> Guilds { get; set; }
        public DbSet<SQLUser> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databasepass = "";
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\dbpass.txt"))
                databasepass = File.ReadAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\dbpass.txt");
            else
            {
                Console.WriteLine("enter db pass please: ");
                databasepass = Console.ReadLine();
            }
            optionsBuilder.UseNpgsql($"Host=localhost;Database=Bot57Ag;Username=Bot57Ag;Password={databasepass}");
        }
    }

    public class SQLConfig
    {
        [Key]
        public string Token { get; set; }
        public string PrefixDefault { get; set; }
        public string[] AdminIDs { get; set; }
    }

    public class SQLGuild
    {
        [Key]
        public string GuildID { get; set; }
        public string Prefix { get; set; }
        public bool UseFunBucks { get; set; }
    }

    public class SQLUser
    {
        [Key]
        public string UserID { get; set; }
        public decimal FunBucks { get; set; }
    }
}
