# ToDoList Database (PostgreSQL)

## Описание

База данных PostgreSQL для хранения пользователей, списков задач и задач для Telegram-бота ToDoList.

## Структура базы данных

### Таблицы

#### ToDoUser
Хранит информацию о пользователях.

| Колонка | Тип | Описание |
|---------|-----|----------|
| UserId | UUID | Первичный ключ |
| TelegramUserId | BIGINT | ID пользователя в Telegram (уникальный) |
| TelegramUserName | VARCHAR(255) | Имя пользователя в Telegram |
| RegisteredAt | TIMESTAMP | Дата и время регистрации |

#### ToDoList
Хранит списки задач пользователей.

| Колонка | Тип | Описание |
|---------|-----|----------|
| Id | UUID | Первичный ключ |
| UserId | UUID | Внешний ключ на ToDoUser |
| Name | VARCHAR(500) | Название списка |
| CreatedAt | TIMESTAMP | Дата и время создания |

#### ToDoItem
Хранит задачи пользователей.

| Колонка | Тип | Описание |
|---------|-----|----------|
| Id | UUID | Первичный ключ |
| UserId | UUID | Внешний ключ на ToDoUser |
| ListId | UUID | Внешний ключ на ToDoList (может быть NULL) |
| Name | VARCHAR(500) | Название задачи |
| CreatedAt | TIMESTAMP | Дата и время создания |
| Deadline | TIMESTAMP | Крайний срок выполнения |
| State | INT | Состояние (0 = Active, 1 = Completed) |
| StateChangedAt | TIMESTAMP | Дата и время изменения состояния |

### Связи

- `ToDoList.UserId` → `ToDoUser.UserId` (CASCADE DELETE)
- `ToDoItem.UserId` → `ToDoUser.UserId` (NO ACTION)
- `ToDoItem.ListId` → `ToDoList.Id` (CASCADE DELETE)

### Индексы

- `IX_ToDoList_UserId` - индекс на ToDoList.UserId
- `IX_ToDoItem_UserId` - индекс на ToDoItem.UserId
- `IX_ToDoItem_ListId` - индекс на ToDoItem.ListId
- `UX_ToDoUser_TelegramUserId` - уникальный индекс на ToDoUser.TelegramUserId
- `IX_ToDoItem_State` - индекс на ToDoItem.State (для быстрой фильтрации по статусу)
- `IX_ToDoItem_Deadline` - индекс на ToDoItem.Deadline (для поиска просроченных задач)

## Файлы

- **ToDoListDb.sql** - создание базы данных, таблиц, внешних ключей и индексов
- **ToDoListDb_Insert.sql** - заполнение тестовыми данными
- **ToDoListDb_Select.sql** - запросы для выборки данных

## Использование

### Создание базы данных

```bash
psql -U postgres -f ToDoListDb.sql
```

### Заполнение тестовыми данными

```bash
psql -U postgres -d ToDoList -f ToDoListDb_Insert.sql
```

### Примеры запросов

Смотрите файл `ToDoListDb_Select.sql` для примеров всех типов запросов (18 запросов).

## Особенности PostgreSQL

- Используются двойные кавычки для сохранения регистра имен таблиц и колонок
- Тип данных `UUID` вместо `UNIQUEIDENTIFIER`
- `TIMESTAMP` вместо `DATETIME2`
- `VARCHAR` вместо `NVARCHAR`
- Функция `NOW() AT TIME ZONE 'UTC'` для получения текущего времени в UTC
- Приведение типов с помощью `::UUID`, `::TIMESTAMP`
- Оператор `ILIKE` для поиска без учета регистра
- `INTERVAL` для работы с временными интервалами

## Примечания

- Все даты хранятся в UTC формате (TIMESTAMP)
- TelegramUserId имеет уникальный индекс для быстрого поиска и предотвращения дублирования
- Задачи могут существовать без списка (ListId = NULL)
- При удалении пользователя каскадно удаляются его списки, а затем и задачи
- Состояние задачи (ToDoItemState): 0 = Active, 1 = Completed

## Запросы репозитория

Файл `ToDoListDb_Select.sql` содержит 18 типовых запросов:

1. Поиск пользователя по TelegramUserId
2. Получение пользователя по UserId
3. Все списки пользователя
4. Список с информацией о пользователе
5. Все задачи пользователя
6. Активные задачи пользователя
7. Задачи конкретного списка
8. Задача с полной информацией
9. Задачи без списка
10. Просроченные активные задачи
11. Завершенные задачи
12. Статистика по задачам
13. Задачи с деталями списков
14. Все пользователи
15. Списки с количеством задач
16. Поиск задач по названию
17. Задачи на сегодня
18. Задачи на неделю
