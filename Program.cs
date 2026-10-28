using LibraryDapperHomework.Data;

const string defaultConnectionString =
    "Host=localhost;Port=5432;Database=library;Username=lab;Password=lab123";
var connectionString = Environment.GetEnvironmentVariable("LIBRARY_DB_CONNECTION")
                       ?? defaultConnectionString;
var repository = new LibraryRepository(connectionString);

Console.WriteLine("--- Books with genres (QueryMultiple) ---");
foreach (var book in await repository.GetBooksWithGenresAsync())
{
    var genres = book.Genres.Count == 0
        ? "no genres"
        : string.Join(", ", book.Genres.Select(genre => genre.Name));
    Console.WriteLine($"{book.Title} [{book.Year}]: {genres}");
}

Console.WriteLine("\n--- Search: ring ---");
foreach (var book in await repository.SearchBooksAsync("ring"))
{
    Console.WriteLine(book.Title);
}

Console.WriteLine("\n--- Page 1 (2 books) ---");
foreach (var book in await repository.GetBooksPagedAsync(page: 1, pageSize: 2))
{
    Console.WriteLine(book.Title);
}

Console.WriteLine("\n--- PostgreSQL function: author 1 ---");
foreach (var book in await repository.GetBooksByAuthorFunctionAsync(authorId: 1))
{
    Console.WriteLine(book.Title);
}

Console.WriteLine("\n--- SQL injection demonstration ---");
const string injection = "' OR 1=1 --";
var safeCount = (await repository.SearchBooksAsync(injection)).Count();
var unsafeCount = (await repository.SearchBooksUnsafeAsync(injection)).Count();
Console.WriteLine($"Parameterized query returned: {safeCount}");
Console.WriteLine($"String concatenation returned: {unsafeCount}");

// Transaction example. Uncomment to transfer book 1 to author 2:
// var transferred = await repository.TransferBookAsync(bookId: 1, newAuthorId: 2);
// Console.WriteLine($"Transferred: {transferred}");
