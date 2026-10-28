# Library Dapper Homework

Консольное приложение на C# демонстрирует работу Dapper с PostgreSQL:

- связь книг и жанров «многие ко многим»;
- `QueryMultiple` с двумя `SELECT` в одном запросе;
- поиск через `ILIKE` и параметры;
- пагинацию через `LIMIT` / `OFFSET`;
- перенос книги другому автору в транзакции;
- небезопасную конкатенацию SQL и защищённый параметризованный запрос;
- вызов PostgreSQL-функции через Dapper.

## Подготовка базы данных

Создайте пользователя и базу (если они ещё не созданы):

```sql
CREATE USER lab WITH PASSWORD 'lab123';
CREATE DATABASE library OWNER lab;
```

Затем выполните скрипты из корня проекта:

```bash
psql -U lab -d library -f database/schema.sql
psql -U lab -d library -f database/seed.sql
```

`seed.sql` очищает учебные таблицы перед добавлением тестовых данных.

## Запуск

Требуются .NET 8 SDK и PostgreSQL.

```bash
dotnet restore
dotnet run
```

По умолчанию используется строка подключения:

```text
Host=localhost;Port=5432;Database=library;Username=lab;Password=lab123
```

Её можно заменить переменной окружения `LIBRARY_DB_CONNECTION`:

```powershell
$env:LIBRARY_DB_CONNECTION = "Host=localhost;Database=library;Username=lab;Password=secret"
dotnet run
```

## SQL-инъекция

`SearchBooksUnsafeAsync` намеренно уязвим и нужен только для демонстрации. Строка
`' OR 1=1 --` меняет смысл SQL-запроса и возвращает все книги. Метод
`SearchBooksAsync` передаёт то же значение через параметр `@pattern`, поэтому оно
обрабатывается как обычный текст и не влияет на структуру SQL.

Не используйте `SearchBooksUnsafeAsync` в реальном приложении.

## Публикация в GitHub

```bash
git init
git add .
git commit -m "Complete Dapper PostgreSQL homework"
git branch -M main
git remote add origin https://github.com/USERNAME/REPOSITORY.git
git push -u origin main
```

Замените `USERNAME/REPOSITORY` адресом своего репозитория.
