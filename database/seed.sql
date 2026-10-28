TRUNCATE TABLE book_genres, genres, books, authors RESTART IDENTITY CASCADE;

INSERT INTO authors (name, country) VALUES
    ('J.R.R. Tolkien', 'UK'),
    ('George Orwell', 'UK');

INSERT INTO books (title, year, author_id) VALUES
    ('The Hobbit', 1937, 1),
    ('The Fellowship of the Ring', 1954, 1),
    ('1984', 1949, 2);

INSERT INTO genres (name) VALUES
    ('Fantasy'),
    ('Adventure'),
    ('Dystopia');

INSERT INTO book_genres (book_id, genre_id) VALUES
    (1, 1),
    (1, 2),
    (2, 1),
    (2, 2),
    (3, 3);
