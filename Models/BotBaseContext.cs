using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JifBot.Models
{
    public partial class BotBaseContext : DbContext
    {
        public BotBaseContext()
        {
        }

        public BotBaseContext(DbContextOptions<BotBaseContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Configuration> Configuration { get; set; }
        public virtual DbSet<Honk> Honk { get; set; }
        public virtual DbSet<Message> Message { get; set; }
        public virtual DbSet<Signature> Signature { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Variable> Variable { get; set; }
        public virtual DbSet<Greeting> Greeting { get; set; }
        public virtual DbSet<ReactionBan> ReactionBan { get; set; }
        public virtual DbSet<ServerConfig> ServerConfig { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=references/BotBase.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Token);

                entity.Property(e => e.DictId).IsRequired();

                entity.Property(e => e.DictKey).IsRequired();

                entity.Property(e => e.Prefix).IsRequired();
            });

            modelBuilder.Entity<Honk>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId).ValueGeneratedNever();
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.Message1)
                    .IsRequired()
                    .HasColumnName("Message");
            });

            modelBuilder.Entity<Signature>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.Signature1)
                    .IsRequired()
                    .HasColumnName("Signature");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.UserId).ValueGeneratedNever();
            });

            modelBuilder.Entity<Variable>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name);

                entity.Property(e => e.Value);
            });

            modelBuilder.Entity<Greeting>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id);

                entity.Property(e => e.Greeting1)
                    .IsRequired()
                    .HasColumnName("Greeting");
            });

            modelBuilder.Entity<ReactionBan>(entity =>
            {
                entity.HasKey(e => e.ChannelId);

                entity.Property(e => e.ChannelId);

                entity.Property(e => e.ServerId);

                entity.Property(e => e.ChannelName);
            });

            modelBuilder.Entity<ServerConfig>(entity =>
            {
                entity.HasKey(e => e.ServerId);

                entity.Property(e => e.ServerId);

                entity.Property(e => e.JoinId);

                entity.Property(e => e.LeaveId);

                entity.Property(e => e.MessageId);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
