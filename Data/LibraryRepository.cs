using Dapper;
using LibraryDapperHomework.Models;
using Npgsql;

namespace LibraryDapperHomework.Data;

public sealed class LibraryRepository
{
    private readonly string _connectionString;

    public LibraryRepository(string connectionString)
    {
        _connectionString = connectionString;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<int> AddAuthorAsync(Author author)
    {
        await using var connection = CreateConnection();
        const string sql = """
            INSERT INTO authors (name, country)
            VALUES (@Name, @Country)
            RETURNING id;
            """;

        return await connection.ExecuteScalarAsync<int>(sql, author);
    }

    public async Task<int> AddBookAsync(Book book)
    {
        await using var connection = CreateConnection();
        const string sql = """
            INSERT INTO books (title, year, author_id)
            VALUES (@Title, @Year, @AuthorId)
            RETURNING id;
            """;

        return await connection.ExecuteScalarAsync<int>(sql, book);
    }

    public async Task<int> AddGenreAsync(Genre genre)
    {
        await using var connection = CreateConnection();
        const string sql = """
            INSERT INTO genres (name)
            VALUES (@Name)
            RETURNING id;
            """;

        return await connection.ExecuteScalarAsync<int>(sql, genre);
    }

    public async Task AddGenreToBookAsync(int bookId, int genreId)
    {
        await using var connection = CreateConnection();
        const string sql = """
            INSERT INTO book_genres (book_id, genre_id)
            VALUES (@bookId, @genreId)
            ON CONFLICT DO NOTHING;
            """;

        await connection.ExecuteAsync(sql, new { bookId, genreId });
    }

    public async Task<IEnumerable<Book>> GetBooksAsync()
    {
        await using var connection = CreateConnection();
        return await connection.QueryAsync<Book>("SELECT * FROM books ORDER BY id;");
    }

    public async Task<IEnumerable<Book>> GetBooksAfterYearAsync(int year)
    {
        await using var connection = CreateConnection();
        const string sql = "SELECT * FROM books WHERE year >= @year ORDER BY year;";
        return await connection.QueryAsync<Book>(sql, new { year });
    }

    public async Task<IEnumerable<Author>> GetAuthorsWithBooksAsync()
    {
        await using var connection = CreateConnection();
        const string sql = """
            SELECT a.id, a.name, a.country,
                   b.id, b.title, b.year, b.author_id
            FROM authors AS a
            LEFT JOIN books AS b ON b.author_id = a.id
            ORDER BY a.id, b.id;
            """;

        var authors = new Dictionary<int, Author>();
        await connection.QueryAsync<Author, Book?, Author>(
            sql,
            (author, book) =>
            {
                if (!authors.TryGetValue(author.Id, out var savedAuthor))
                {
                    savedAuthor = author;
                    authors[author.Id] = savedAuthor;
                }

                if (book is not null)
                {
                    savedAuthor.Books.Add(book);
                }

                return savedAuthor;
            },
            splitOn: "id");

        return authors.Values;
    }

    // QueryMultiple receives books and their genres from two SELECT statements
    // sent to PostgreSQL as one command.
    public async Task<IReadOnlyList<Book>> GetBooksWithGenresAsync()
    {
        await using var connection = CreateConnection();
        const string sql = """
            SELECT id, title, year, author_id
            FROM books
            ORDER BY id;

            SELECT bg.book_id,
                   g.id AS genre_id,
                   g.name AS genre_name
            FROM book_genres AS bg
            JOIN genres AS g ON g.id = bg.genre_id
            ORDER BY bg.book_id, g.name;
            """;

        using var result = await connection.QueryMultipleAsync(sql);
        var books = (await result.ReadAsync<Book>()).ToList();
        var genreRows = await result.ReadAsync<BookGenreRow>();
        var booksById = books.ToDictionary(book => book.Id);

        foreach (var row in genreRows)
        {
            if (booksById.TryGetValue(row.BookId, out var book))
            {
                book.Genres.Add(new Genre { Id = row.GenreId, Name = row.GenreName });
            }
        }

        return books;
    }

    public async Task<IEnumerable<Book>> SearchBooksAsync(string term)
    {
        await using var connection = CreateConnection();
        const string sql = """
            SELECT *
            FROM books
            WHERE title ILIKE @pattern
            ORDER BY title;
            """;

        return await connection.QueryAsync<Book>(sql, new { pattern = $"%{term}%" });
    }

    public async Task<IEnumerable<Book>> GetBooksPagedAsync(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be from 1 to 100.");
        }

        await using var connection = CreateConnection();
        const string sql = """
            SELECT *
            FROM books
            ORDER BY id
            LIMIT @size OFFSET @offset;
            """;
        var offset = (page - 1) * pageSize;

        return await connection.QueryAsync<Book>(sql, new { size = pageSize, offset });
    }

    public async Task<bool> UpdateBookYearAsync(int bookId, int newYear)
    {
        await using var connection = CreateConnection();
        const string sql = "UPDATE books SET year = @newYear WHERE id = @bookId;";
        return await connection.ExecuteAsync(sql, new { bookId, newYear }) > 0;
    }

    public async Task<bool> DeleteAuthorAsync(int authorId)
    {
        await using var connection = CreateConnection();
        const string sql = "DELETE FROM authors WHERE id = @authorId;";
        return await connection.ExecuteAsync(sql, new { authorId }) > 0;
    }

    public async Task<bool> TransferBookAsync(int bookId, int newAuthorId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            const string authorExistsSql = "SELECT EXISTS(SELECT 1 FROM authors WHERE id = @newAuthorId);";
            var authorExists = await connection.ExecuteScalarAsync<bool>(
                authorExistsSql,
                new { newAuthorId },
                transaction);

            if (!authorExists)
            {
                await transaction.RollbackAsync();
                return false;
            }

            const string transferSql = """
                UPDATE books
                SET author_id = @newAuthorId
                WHERE id = @bookId;
                """;
            var affectedRows = await connection.ExecuteAsync(
                transferSql,
                new { bookId, newAuthorId },
                transaction);

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Intentionally vulnerable. This method exists only for a classroom demo.
    public async Task<IEnumerable<Book>> SearchBooksUnsafeAsync(string term)
    {
        await using var connection = CreateConnection();
        var sql = $"SELECT * FROM books WHERE title ILIKE '%{term}%' ORDER BY title;";
        return await connection.QueryAsync<Book>(sql);
    }

    public async Task<IEnumerable<Book>> GetBooksByAuthorFunctionAsync(int authorId)
    {
        await using var connection = CreateConnection();
        const string sql = "SELECT * FROM get_books_by_author(@authorId);";
        return await connection.QueryAsync<Book>(sql, new { authorId });
    }
}
