# Book Data API Endpoints

This document outlines all available RESTful API endpoints for the Book Data API.

## Books API Endpoints

### Base URL: `/api/books`

#### GET /api/books
Retrieve a paginated list of books with optional search functionality.

**Query Parameters:**
- `search` (optional): Search term to filter books by searchable string
- `page` (optional, default: 1): Page number for pagination
- `pageSize` (optional, default: 20): Number of books per page

**Response:**
```json
{
  "books": [
    {
      "id": 1,
      "title": "Book Title",
      "authorFirstName": "John",
      "authorLastName": "Doe",
      "isbn10": "1234567890",
      "isbn13": "9781234567890",
      "averageRating": 4.5,
      "numberOfPages": 300,
      "originalPublicationYear": 2020,
      "searchableString": "book title john doe",
      "titleByAuthor": "Book Title by John Doe",
      "coverImageId": 1,
      "bookshelves": [...],
      "tones": [...],
      "coverImage": {...}
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5
  }
}
```

#### GET /api/books/{id}
Retrieve a specific book by ID with all related data.

**Response:** Returns detailed book information including bookshelves, tones, and cover image.

#### POST /api/books
Create a new book.

**Request Body:**
```json
{
  "title": "Book Title",
  "authorFirstName": "John",
  "authorLastName": "Doe",
  "isbn10": "1234567890",
  "isbn13": "9781234567890",
  "averageRating": 4.5,
  "numberOfPages": 300,
  "originalPublicationYear": 2020,
  "searchableString": "book title john doe",
  "coverImageId": 1
}
```

**Response:** Returns the created book with ID.

#### PUT /api/books/{id}
Update an existing book.

**Request Body:** Same structure as POST, but all fields are optional.

**Response:** Returns the updated book with all related data.

#### DELETE /api/books/{id}
Delete a book.

**Response:** 204 No Content on success.

## Book Tone Recommendations API Endpoints

### Base URL: `/api/book-tone-recommendations`

#### GET /api/book-tone-recommendations
Retrieve all book tone recommendations with optional filtering.

**Query Parameters:**
- `bookId` (optional): Filter by specific book ID
- `tone` (optional): Filter by tone name (partial match)

**Response:**
```json
[
  {
    "id": 1,
    "bookId": 1,
    "tone": "Mysterious",
    "toneId": 5,
    "feedback": 1,
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

#### GET /api/book-tone-recommendations/{id}
Retrieve a specific book tone recommendation by ID.

#### GET /api/book-tone-recommendations/book/{bookId}
Retrieve all tone recommendations for a specific book.

#### POST /api/book-tone-recommendations
Create a new book tone recommendation.

**Request Body:**
```json
{
  "bookId": 1,
  "tone": "Mysterious",
  "feedback": 1
}
```

**Validation:**
- `bookId`: Required, must reference an existing book
- `tone`: Required, non-empty string
- `feedback`: Must be between -2 and 1

**Response:** Returns the created recommendation with ID.

#### PUT /api/book-tone-recommendations/{id}
Update the feedback for an existing book tone recommendation.

**Request Body:**
```json
{
  "toneId": 5,
  "feedback": 0
}
```

**Response:** Returns the updated recommendation.



## Feedback Values

The feedback field in book tone recommendations uses the following scale:
- `-2`: Strongly disagree
- `-1`: Disagree
- `0`: Neutral
- `1`: Agree

## Error Responses

All endpoints return appropriate HTTP status codes:
- `200 OK`: Successful operation
- `201 Created`: Resource created successfully
- `204 No Content`: Resource deleted successfully
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

Error responses include a descriptive message in the response body. 