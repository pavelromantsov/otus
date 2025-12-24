-- =============================================
-- ToDoList Database Schema (PostgreSQL)
-- =============================================

-- Create Database
CREATE DATABASE "ToDoList";

-- Connect to database
\c "ToDoList";

-- =============================================
-- Table: ToDoUser
-- =============================================
CREATE TABLE "ToDoUser"
(
    "UserId" UUID NOT NULL PRIMARY KEY,
    "TelegramUserId" BIGINT NOT NULL,
    "TelegramUserName" VARCHAR(255) NULL,
    "RegisteredAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

-- =============================================
-- Table: ToDoList
-- =============================================
CREATE TABLE "ToDoList"
(
    "Id" UUID NOT NULL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Name" VARCHAR(500) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC')
);

-- =============================================
-- Table: ToDoItem
-- =============================================
CREATE TABLE "ToDoItem"
(
    "Id" UUID NOT NULL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ListId" UUID NULL,
    "Name" VARCHAR(500) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    "Deadline" TIMESTAMP NOT NULL,
    "State" INT NOT NULL DEFAULT 0, -- 0 = Active, 1 = Completed
    "StateChangedAt" TIMESTAMP NULL
);

-- =============================================
-- Foreign Keys
-- =============================================

-- ToDoList.UserId -> ToDoUser.UserId
ALTER TABLE "ToDoList"
    ADD CONSTRAINT "FK_ToDoList_ToDoUser"
    FOREIGN KEY ("UserId") REFERENCES "ToDoUser"("UserId")
    ON DELETE CASCADE;

-- ToDoItem.UserId -> ToDoUser.UserId
ALTER TABLE "ToDoItem"
    ADD CONSTRAINT "FK_ToDoItem_ToDoUser"
    FOREIGN KEY ("UserId") REFERENCES "ToDoUser"("UserId")
    ON DELETE NO ACTION;

-- ToDoItem.ListId -> ToDoList.Id
ALTER TABLE "ToDoItem"
    ADD CONSTRAINT "FK_ToDoItem_ToDoList"
    FOREIGN KEY ("ListId") REFERENCES "ToDoList"("Id")
    ON DELETE CASCADE;

-- =============================================
-- Indexes
-- =============================================

-- Index for ToDoList.UserId (Foreign Key)
CREATE INDEX "IX_ToDoList_UserId"
    ON "ToDoList"("UserId");

-- Index for ToDoItem.UserId (Foreign Key)
CREATE INDEX "IX_ToDoItem_UserId"
    ON "ToDoItem"("UserId");

-- Index for ToDoItem.ListId (Foreign Key)
CREATE INDEX "IX_ToDoItem_ListId"
    ON "ToDoItem"("ListId");

-- Unique Index for ToDoUser.TelegramUserId
CREATE UNIQUE INDEX "UX_ToDoUser_TelegramUserId"
    ON "ToDoUser"("TelegramUserId");

-- Additional performance indexes
CREATE INDEX "IX_ToDoItem_State"
    ON "ToDoItem"("State");

CREATE INDEX "IX_ToDoItem_Deadline"
    ON "ToDoItem"("Deadline");

-- =============================================
-- Comments for documentation
-- =============================================
COMMENT ON TABLE "ToDoUser" IS 'Stores user information for the ToDoList application';
COMMENT ON TABLE "ToDoList" IS 'Stores user task lists';
COMMENT ON TABLE "ToDoItem" IS 'Stores individual tasks/items';

COMMENT ON COLUMN "ToDoItem"."State" IS '0 = Active, 1 = Completed';
