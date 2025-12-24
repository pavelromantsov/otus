-- =============================================
-- ToDoList Database - Select Queries (PostgreSQL)
-- =============================================

-- Connect to database
\c "ToDoList";

-- =============================================
-- 1. Get user by Telegram User ID
-- =============================================
-- Parameters: @TelegramUserId (BIGINT)
SELECT 
    "UserId",
    "TelegramUserId",
    "TelegramUserName",
    "RegisteredAt"
FROM "ToDoUser"
WHERE "TelegramUserId" = 123456789;

-- =============================================
-- 2. Get user by UserId
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "UserId",
    "TelegramUserId",
    "TelegramUserName",
    "RegisteredAt"
FROM "ToDoUser"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID;

-- =============================================
-- 3. Get all lists for a user
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "Name",
    "CreatedAt"
FROM "ToDoList"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
ORDER BY "CreatedAt" DESC;

-- =============================================
-- 4. Get list by Id with user information
-- =============================================
-- Parameters: @ListId (UUID)
SELECT 
    l."Id",
    l."Name" AS "ListName",
    l."CreatedAt" AS "ListCreatedAt",
    u."UserId",
    u."TelegramUserId",
    u."TelegramUserName",
    u."RegisteredAt"
FROM "ToDoList" l
INNER JOIN "ToDoUser" u ON l."UserId" = u."UserId"
WHERE l."Id" = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'::UUID;

-- =============================================
-- 5. Get all items for a user
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
ORDER BY "Deadline" ASC;

-- =============================================
-- 6. Get all active items for a user
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
    AND "State" = 0
ORDER BY "Deadline" ASC;

-- =============================================
-- 7. Get all items in a specific list
-- =============================================
-- Parameters: @ListId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "ListId" = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'::UUID
ORDER BY "State", "Deadline" ASC;

-- =============================================
-- 8. Get item by Id with full details
-- =============================================
-- Parameters: @ItemId (UUID)
SELECT 
    i."Id" AS "ItemId",
    i."Name" AS "ItemName",
    i."CreatedAt" AS "ItemCreatedAt",
    i."Deadline",
    i."State",
    i."StateChangedAt",
    u."UserId",
    u."TelegramUserId",
    u."TelegramUserName",
    u."RegisteredAt",
    l."Id" AS "ListId",
    l."Name" AS "ListName",
    l."CreatedAt" AS "ListCreatedAt"
FROM "ToDoItem" i
INNER JOIN "ToDoUser" u ON i."UserId" = u."UserId"
LEFT JOIN "ToDoList" l ON i."ListId" = l."Id"
WHERE i."Id" = 'EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE'::UUID;

-- =============================================
-- 9. Get items without a list (orphaned items)
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "ListId" IS NULL
    AND "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
ORDER BY "Deadline" ASC;

-- =============================================
-- 10. Get overdue active items for a user
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
    AND "State" = 0
    AND "Deadline" < (NOW() AT TIME ZONE 'UTC')
ORDER BY "Deadline" ASC;

-- =============================================
-- 11. Get completed items for a user
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
    AND "State" = 1
ORDER BY "StateChangedAt" DESC;

-- =============================================
-- 12. Get statistics for a user
-- =============================================
-- Parameters: @UserId (UUID)
-- Aggregate query: total items, active, completed, overdue
SELECT 
    u."UserId",
    u."TelegramUserName",
    COUNT(i."Id") AS "TotalItems",
    SUM(CASE WHEN i."State" = 0 THEN 1 ELSE 0 END) AS "ActiveItems",
    SUM(CASE WHEN i."State" = 1 THEN 1 ELSE 0 END) AS "CompletedItems",
    SUM(CASE WHEN i."State" = 0 AND i."Deadline" < (NOW() AT TIME ZONE 'UTC') THEN 1 ELSE 0 END) AS "OverdueItems"
FROM "ToDoUser" u
LEFT JOIN "ToDoItem" i ON u."UserId" = i."UserId"
WHERE u."UserId" = '11111111-1111-1111-1111-111111111111'::UUID
GROUP BY u."UserId", u."TelegramUserName";

-- =============================================
-- 13. Get items by list with list details
-- =============================================
-- Parameters: @UserId (UUID)
-- Join query to get all items with their list information
SELECT 
    l."Id" AS "ListId",
    l."Name" AS "ListName",
    l."CreatedAt" AS "ListCreatedAt",
    i."Id" AS "ItemId",
    i."Name" AS "ItemName",
    i."Deadline",
    i."State",
    i."CreatedAt" AS "ItemCreatedAt"
FROM "ToDoList" l
LEFT JOIN "ToDoItem" i ON l."Id" = i."ListId"
WHERE l."UserId" = '11111111-1111-1111-1111-111111111111'::UUID
ORDER BY l."Name", i."State", i."Deadline";

-- =============================================
-- 14. Get all users
-- =============================================
SELECT 
    "UserId",
    "TelegramUserId",
    "TelegramUserName",
    "RegisteredAt"
FROM "ToDoUser"
ORDER BY "RegisteredAt" DESC;

-- =============================================
-- 15. Get lists with item count
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    l."Id",
    l."Name",
    l."CreatedAt",
    COUNT(i."Id") AS "ItemCount",
    SUM(CASE WHEN i."State" = 0 THEN 1 ELSE 0 END) AS "ActiveItemCount",
    SUM(CASE WHEN i."State" = 1 THEN 1 ELSE 0 END) AS "CompletedItemCount"
FROM "ToDoList" l
LEFT JOIN "ToDoItem" i ON l."Id" = i."ListId"
WHERE l."UserId" = '11111111-1111-1111-1111-111111111111'::UUID
GROUP BY l."Id", l."Name", l."CreatedAt"
ORDER BY l."CreatedAt" DESC;

-- =============================================
-- 16. Search items by name (case-insensitive)
-- =============================================
-- Parameters: @UserId (UUID), @SearchTerm (VARCHAR)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
    AND "Name" ILIKE '%project%'
ORDER BY "CreatedAt" DESC;

-- =============================================
-- 17. Get items due today
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
    AND "State" = 0
    AND DATE("Deadline") = CURRENT_DATE
ORDER BY "Deadline" ASC;

-- =============================================
-- 18. Get items due this week
-- =============================================
-- Parameters: @UserId (UUID)
SELECT 
    "Id",
    "UserId",
    "ListId",
    "Name",
    "CreatedAt",
    "Deadline",
    "State",
    "StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = '11111111-1111-1111-1111-111111111111'::UUID
    AND "State" = 0
    AND "Deadline" >= CURRENT_DATE
    AND "Deadline" < CURRENT_DATE + INTERVAL '7 days'
ORDER BY "Deadline" ASC;
