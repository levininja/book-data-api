# Book Data API

A standalone ASP.NET Core Web API for serving book data and related information. This API provides endpoints for managing books, book reviews, bookshelves, book tones, book tone recommendations, and book cover images.

## Features

- **Book Management**: CRUD operations for books with metadata
- **Book Reviews**: Store and retrieve book reviews with ratings
- **Book Tone Recommendations**: AI-powered tone suggestions with feedback system
- **Bookshelves**: Organize books into custom bookshelves with grouping support
- **Book Cover Images**: Automatic book cover image fetching and storage
- **Search**: Full-text search capabilities for books and reviews
- **Tones**: Categorize books by tone/mood with hierarchical structure
- **Tone Assignment**: Assign tones to books and book reviews
- **Swagger Documentation**: Interactive API documentation

## Architecture

This project uses a **shared library architecture** with `BookDataApi.Shared` containing:
- **Core Models**: `Book`, `BookToneRecommendation`, `Tone`
- **All DTOs**: Data transfer objects for API contracts
- **Shared Contracts**: Reusable across multiple projects

The main API project extends these shared models with Entity Framework navigation properties.

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL database

## Setup Instructions

### 1. Database Setup

1. Install PostgreSQL if you haven't already
2. Create a new database:
   ```sql
   CREATE DATABASE book_data_api;
   ```
3. Update the connection string in `appsettings.json` if needed:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=book_data_api;Username=your_username;Password=your_password"
     }
   }
   ```

### 2. Environment Configuration

1. Copy `appsettings.Development.json` and update the Google Custom Search API settings:
   ```json
   {
     "GoogleCustomSearch": {
       "ApiKey": "your-google-api-key",
       "SearchEngineId": "your-search-engine-id"
     }
   }
   ```

### 3. Database Migration

Run the following commands to set up the database:

```bash
# Install Entity Framework tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create and apply database migrations
dotnet ef database update
```

### 4. Running the Application

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

The API will be available at:
- **API Base URL**: http://localhost:5020
- **Swagger Documentation**: http://localhost:5020/swagger

## API Endpoints

### Books
- `GET /api/books` - Get all books (with pagination and search)
- `GET /api/books/{id}` - Get book by ID
- `POST /api/books` - Create new book
- `PUT /api/books/{id}` - Update book
- `DELETE /api/books/{id}` - Delete book

### Book Tone Recommendations
- `GET /api/booktonerecommendations` - Get all recommendations (with optional tone filtering)
- `GET /api/booktonerecommendations/{id}` - Get recommendation by ID
- `GET /api/booktonerecommendations/book/{bookId}` - Get recommendations for a specific book
- `POST /api/booktonerecommendations` - Create new recommendation
- `PUT /api/booktonerecommendations/{id}` - Update recommendation feedback and tone

### Book Reviews
- `GET /api/bookreviews` - Get all book reviews
- `GET /api/bookreviews/{id}` - Get book review by ID
- `POST /api/bookreviews` - Create new book review
- `PUT /api/bookreviews/{id}` - Update book review
- `DELETE /api/bookreviews/{id}` - Delete book review

### Bookshelves
- `GET /api/bookshelves` - Get all bookshelves with configuration
- `GET /api/bookshelves/{id}` - Get bookshelf by ID
- `POST /api/bookshelves` - Create new bookshelf
- `PUT /api/bookshelves/{id}` - Update bookshelf
- `DELETE /api/bookshelves/{id}` - Delete bookshelf

### Bookshelf Groupings
- `GET /api/bookshelfgroupings` - Get all bookshelf groupings
- `POST /api/bookshelfgroupings` - Create new grouping
- `PUT /api/bookshelfgroupings/{id}` - Update grouping
- `DELETE /api/bookshelfgroupings/{id}` - Delete grouping

### Book Covers
- `GET /api/bookcovers/{searchTerm}` - Get book cover image
- `POST /api/bookcovers` - Upload book cover image

### Tones
- `GET /api/tones` - Get all tones
- `GET /api/tones/{id}` - Get tone by ID
- `POST /api/tones` - Create new tone
- `PUT /api/tones/{id}` - Update tone
- `DELETE /api/tones/{id}` - Delete tone

### Tone Assignment
- `GET /api/toneassignment` - Get tone assignment data for books and reviews
- `POST /api/toneassignment/assignment` - Update tone assignments

## Development

### Project Structure

```
book-data-api/
├── Controllers/          # API controllers
├── Data/                # Database context and configuration
├── Models/              # Entity models (extend shared models)
├── Services/            # Business logic services
├── Migrations/          # Database migrations
├── Program.cs           # Application entry point
└── BookDataApi.Shared/  # Shared library (separate project)
    ├── Models/          # Core models without EF navigation
    └── Dtos/            # All DTOs for API contracts
```

### Shared Library DTOs

The following DTOs are available in `BookDataApi.Shared`:

**Book-related:**
- `BookDto`, `CreateBookDto`, `UpdateBookDto`
- `BookToneRecommendationDto`, `CreateBookToneRecommendationDto`, `UpdateBookToneRecommendationDto`

**Bookshelf-related:**
- `BookshelfDto`, `BookshelfConfigurationDto`
- `BookshelfDisplayItemDto`, `BookshelfGroupingItemDto`

**Tone-related:**
- `ToneDto`, `ToneItemDto`, `ToneAssignmentDto`
- `BookReviewToneItemDto`, `GenreToneAssociationDto`

**Other:**
- `BookCoverImageDto`, `ErrorViewModel`

### Adding New Features

1. **For shared functionality**: Add to `BookDataApi.Shared` project
2. **For EF-specific features**: Add to main project's `Models/` directory
3. Create model classes in the appropriate location
4. Add DbSet to `ApplicationDbContext`
5. Create a new migration: `dotnet ef migrations add MigrationName`
6. Apply the migration: `dotnet ef database update`
7. Create controller in `Controllers/` directory
8. Add any necessary services in `Services/` directory

## Configuration

### Environment Variables

The application uses the following configuration sections:

- **ConnectionStrings**: Database connection string
- **GoogleCustomSearch**: Google Custom Search API settings
- **Logging**: Logging configuration
- **Kestrel**: Server configuration (port 5020)

### CORS

The API is configured to allow all origins in development. For production, update the CORS policy in `Program.cs`.

## Troubleshooting

### Common Issues

1. **Database Connection**: Ensure PostgreSQL is running and the connection string is correct
2. **Port Already in Use**: The application runs on port 5020 by default. Change it in `appsettings.Development.json` if needed
3. **Migration Errors**: If you encounter migration issues, you can remove the `Migrations/` folder and create a new initial migration
4. **Shared Library Build Issues**: Ensure `BookDataApi.Shared` builds before the main project

### Logs

Check the console output for detailed error messages and logs.

## License

MIT License

Copyright (c) 2024 Levi Hobbs

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 