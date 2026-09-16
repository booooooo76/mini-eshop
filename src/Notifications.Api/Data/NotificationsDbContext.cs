using Microsoft.EntityFrameworkCore;
using Notifications.Api.Models;

namespace Notifications.Api.Data;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
}
