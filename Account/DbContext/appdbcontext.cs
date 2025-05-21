using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.ComponentModel.DataAnnotations;
using Account.Models;

namespace Account.Data
{
    public class appdbcontext : DbContext
    {
        public appdbcontext(DbContextOptions<appdbcontext> options) : base(options)
        {
        }

        public DbSet<Disciplin> Disciplin { get; set; }
        public DbSet<Teacher> Teacher { get; set; }
    }
}
