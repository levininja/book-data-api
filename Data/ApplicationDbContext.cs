using Microsoft.EntityFrameworkCore;
using book_data_api.Models;

namespace book_data_api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BookCoverImage> BookCoverImages { get; set; } = null!;
        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<BookReview> BookReviews { get; set; } = null!;
        public DbSet<Bookshelf> Bookshelves { get; set; } = null!;
        public DbSet<BookshelfGrouping> BookshelfGroupings { get; set; } = null!;
        public DbSet<Tone> Tones { get; set; } = null!;
        public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookCoverImage>()
                .HasIndex(b => b.Name)
                .IsUnique();
                
            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Book_AverageRating", "\"AverageRating\" BETWEEN 0 AND 5");
                });
                entity.Property(b => b.AverageRating).HasColumnType("decimal(3,2)");
                
                // Assumes BookCoverImage can be associated with multiple books if needed, but the FK is on Book
                entity.HasOne(b => b.CoverImage)
                    .WithMany()  // No inverse navigation (BookCoverImage doesn't need a list of books)
                    .HasForeignKey(b => b.CoverImageId)
                    .IsRequired(false)  // Allows null for books without images
                    .OnDelete(DeleteBehavior.SetNull);  // If image is deleted, set FK to null instead of cascading
                    
                // Add index for searchable string
                entity.HasIndex(b => b.SearchableString)
                    .HasDatabaseName("IX_Book_SearchableString");
            });
                
            modelBuilder.Entity<BookReview>(entity =>
            {
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_BookReview_ReviewerRating", "\"ReviewerRating\" BETWEEN 0 AND 5");
                });
                entity.Property(br => br.ReviewerRating).HasColumnType("integer");
                
                // Add index for reviews with content
                entity.Property(br => br.HasReviewContent)
                    .HasComputedColumnSql("CASE WHEN \"Review\" IS NOT NULL AND \"Review\" != '' THEN true ELSE false END", stored: true);

                entity.HasIndex(br => br.HasReviewContent)
                    .HasDatabaseName("IX_BookReview_HasReviewContent")
                    .HasFilter("\"HasReviewContent\" = true");
                    
                // Configure one-to-many relationship with Book
                entity.HasOne(br => br.Book)
                    .WithMany(b => b.BookReviews)
                    .HasForeignKey(br => br.BookId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });
                
            // Configure many-to-many relationship between Book and Bookshelf
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Bookshelves)
                .WithMany(bs => bs.Books)
                .UsingEntity(j => j.ToTable("BookBookshelves"));
                
            // Configure many-to-many relationship between Book and Tone
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Tones)
                .WithMany(t => t.Books)
                .UsingEntity(j => j.ToTable("BookTones"));
                
            modelBuilder.Entity<Bookshelf>()
                .HasIndex(bs => bs.Name)
                .IsUnique();
                
            // Configure many-to-many relationship between Bookshelf and BookshelfGrouping
            modelBuilder.Entity<Bookshelf>()
                .HasMany(bs => bs.BookshelfGroupings)
                .WithMany(bg => bg.Bookshelves)
                .UsingEntity(j => j.ToTable("BookshelfGroupingBookshelves"));
                
            modelBuilder.Entity<BookshelfGrouping>()
                .HasIndex(bg => bg.Name)
                .IsUnique();
                
            // Configure self-referencing relationship for Tone
            modelBuilder.Entity<Tone>(entity =>
            {
                entity.HasOne(t => t.Parent)
                    .WithMany(t => t.Subtones)
                    .HasForeignKey(t => t.ParentId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid orphaned records
                    
                entity.HasIndex(t => t.Name)
                    .IsUnique();
            });
        }
    }
} 