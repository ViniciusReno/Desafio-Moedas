using Microsoft.EntityFrameworkCore;
using Queues.Models;

namespace Queues.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) 
            : base(options)
        {
        }

        public DbSet<ItemFila> ItemFilas { get; set; }
    }
}
