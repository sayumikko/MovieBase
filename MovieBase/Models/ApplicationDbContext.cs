using Microsoft.EntityFrameworkCore;
using MovieBase.Models;

namespace MovieBase.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<MovieData> Movies { get; set; }
        public DbSet<Actor> Actors { get; set; }
        public DbSet<Director> Directors { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<MovieActor> MovieActors { get; set; }
        public DbSet<MovieTag> MovieTags { get; set; }
        public DbSet<MovieSimilarity> MovieSimilarities { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MovieActor>()
                .HasKey(ma => new { ma.MovieTitleId, ma.ActorId });

            modelBuilder.Entity<MovieActor>()
                .HasOne(ma => ma.MovieData)
                .WithMany(m => m.MovieActors)
                .HasForeignKey(ma => ma.MovieTitleId);

            modelBuilder.Entity<MovieActor>()
                .HasOne(ma => ma.Actor)
                .WithMany(a => a.MovieActors)
                .HasForeignKey(ma => ma.ActorId);

            modelBuilder.Entity<MovieTag>()
                .HasKey(mt => new { mt.MovieTitleId, mt.TagId });

            modelBuilder.Entity<MovieTag>()
                .HasOne(mt => mt.MovieData)
                .WithMany(m => m.MovieTags)
                .HasForeignKey(mt => mt.MovieTitleId);

            modelBuilder.Entity<MovieTag>()
                .HasOne(mt => mt.Tag)
                .WithMany(t => t.MovieTags)
                .HasForeignKey(mt => mt.TagId);

            modelBuilder.Entity<MovieData>()
                .HasOne(m => m.Director)
                .WithMany(d => d.MoviesDirected)
                .HasForeignKey(m => m.DirectorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MovieSimilarity>()
                .HasOne(ms => ms.Movie)
                .WithMany(m => m.SimilarMovies)
                .HasForeignKey(ms => ms.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieSimilarity>()
                .HasOne(ms => ms.SimilarMovie)
                .WithMany()
                .HasForeignKey(ms => ms.SimilarMovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
