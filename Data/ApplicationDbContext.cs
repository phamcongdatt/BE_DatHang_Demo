using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Models;

namespace QuanLyDatHang.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }  
        public DbSet<Category> Categories { get; set; }
        public object CategoryUpdateRequests { get; internal set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the one-to-one relationship between Order and Review
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Review)
                .WithOne(r => r.Order)
                .HasForeignKey<Review>(r => r.OrderId);
                
            // Configure foreign key from Store to User (Seller)
            modelBuilder.Entity<Store>()
                .HasOne(s => s.Seller)
                .WithMany(u => u.Stores)
                .HasForeignKey(s => s.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure foreign key from Order to User (Customer)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure foreign key from Review to Store
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Store)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure foreign key from Review to User (Customer)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

         

            // Configure enum to string conversion
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Store>()
                .Property(s => s.Status)
                .HasConversion<string>();
            
            modelBuilder.Entity<Menu>()
                .Property(m => m.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentMethod)
                .HasConversion<string>();
            
            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentStatus)
                .HasConversion<string>();


            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Menu)
                .WithMany(m => m.OrderDetails)
                .HasForeignKey(od => od.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Cart relationships
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Menu)
                .WithMany()
                .HasForeignKey(ci => ci.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Wishlist relationships
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Customer)
                .WithMany()
                .HasForeignKey(w => w.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Menu)
                .WithMany()
                .HasForeignKey(w => w.MenuId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 