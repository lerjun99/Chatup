using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence
{
    public class ChatDBContextFactory : IDesignTimeDbContextFactory<ChatDBContext>
    {
        public ChatDBContext CreateDbContext(string[] args)
        {
            // Load configuration
            IConfiguration configuration = new ConfigurationBuilder()
           .SetBasePath(Path.GetPathRoot(Environment.SystemDirectory))
           .AddJsonFile("app/chatup/appconfig.json", optional: true, reloadOnChange: true)
           .Build();


            // Build options
            var optionsBuilder = new DbContextOptionsBuilder<ChatDBContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("Chatup_ConnectionString"));

            return new ChatDBContext(optionsBuilder.Options);
        }
    }
}
