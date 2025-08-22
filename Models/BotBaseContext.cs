using System;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Variable> Variable { get; set; }
        public virtual DbSet<Greeting> Greeting { get; set; }
        public virtual DbSet<ReactionBan> ReactionBan { get; set; }
        public virtual DbSet<ServerConfig> ServerConfig { get; set; }
        public virtual DbSet<Command> Command { get; set; }
        public virtual DbSet<CommandParameter> CommandParameter { get; set; }
        public virtual DbSet<CommandParameterChoice> CommandParameterChoice { get; set; }
        public virtual DbSet<ReactRole> ReactRole { get; set; }
        public virtual DbSet<ChangeLog> ChangeLog { get; set; }
        public virtual DbSet<CommandCall> CommandCall { get; set; }
        public virtual DbSet<Character> Character { get; set; }
        public virtual DbSet<CharacterAlias> CharacterAlias { get; set; }
        public virtual DbSet<CharacterTag> CharacterTag { get; set; }
        public virtual DbSet<CourtRecord> CourtRecord { get; set; }
        public virtual DbSet<Qotd> Qotd { get; set; }
        public virtual DbSet<Timer> Timer { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string location = Environment.GetEnvironmentVariable("JIFBOT_DB");
                location = location.Replace("\"", "");
                optionsBuilder.UseSqlite($"Data Source={location}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Token).IsRequired();

                entity.Property(e => e.Prefix).IsRequired();

                entity.Property(e => e.Id).IsRequired();
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

                entity.Property(e => e.ReactMessageId);

                entity.Property(e => e.ReactChannelId);
            });

            modelBuilder.Entity<Command>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name);

                entity.Property(e => e.Category);

                entity.Property(e => e.Description);
            });


            modelBuilder.Entity<CommandParameter>(entity =>
            {
                entity.HasKey(e => new { e.Command, e.Name });

                entity.Property(e => e.Description);

                entity.Property(e => e.Required);
            });

            modelBuilder.Entity<CommandParameterChoice>(entity =>
            {
                entity.HasKey(e => new { e.Command, e.Parameter, e.Name });

                entity.Property(e => e.Command);

                entity.Property(e => e.Parameter);

                entity.Property(e => e.Name);
            });

            modelBuilder.Entity<ReactRole>(entity =>
            {
                entity.HasKey(e => e.RoleId);

                entity.Property(e => e.RoleId);

                entity.Property(e => e.Emote);

                entity.Property(e => e.Description);

                entity.Property(e => e.ServerId);
            });

            modelBuilder.Entity<ChangeLog>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.Date);

                entity.Property(e => e.Change);
            });

            modelBuilder.Entity<CommandCall>(entity =>
            {
                entity.HasKey(e => new { e.Command, e.Timestamp, e.ServerId });

                entity.Property(e => e.Command);

                entity.Property(e => e.Timestamp);

                entity.Property(e => e.ServerId);

                entity.Property(e => e.UserId);
            });

            modelBuilder.Entity<Character>(entity =>
            {
                entity.HasKey(e => e.Key);
            });

            modelBuilder.Entity<CharacterAlias>(entity =>
            {
                entity.HasKey(e => new { e.Key, e.Alias });
            });

            modelBuilder.Entity<CharacterTag>(entity =>
            {
                entity.HasKey(e => new { e.Key, e.Tag });
            });

            modelBuilder.Entity<CourtRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.YayVotes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, null),
                    v => JsonSerializer.Deserialize<List<ulong>>(v, null)
                );
                entity.Property(e => e.NayVotes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, null),
                    v => JsonSerializer.Deserialize<List<ulong>>(v, null)
                );
            });

            modelBuilder.Entity<Qotd>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Timer>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
