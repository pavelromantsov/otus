-- Включить UUID-расширения (если не включены)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Таблица ToDoUserModel (предполагаемая структура по FK)
CREATE TABLE IF NOT EXISTS public."ToDoUserModel" (
    "UserId" UUID PRIMARY KEY DEFAULT uuid_generate_v4()
);

-- Таблица ToDoListModel (предполагаемая структура по FK)
CREATE TABLE IF NOT EXISTS public."ToDoListModel" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4()
);

-- Основная таблица ToDoItemModel
CREATE TABLE public."ToDoItemModel" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" UUID NOT NULL,
    "ListId" UUID,
    "Name" VARCHAR(500) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Deadline" TIMESTAMP WITH TIME ZONE NOT NULL,
    "State" INTEGER NOT NULL DEFAULT 0,
    "StateChangedAt" TIMESTAMP WITH TIME ZONE,
    
    CONSTRAINT "FK_ToDoItemModel_ToDoUserModel" 
        FOREIGN KEY ("UserId") REFERENCES public."ToDoUserModel"("UserId") ON DELETE CASCADE,
    
    CONSTRAINT "FK_ToDoItemModel_ToDoListModel" 
        FOREIGN KEY ("ListId") REFERENCES public."ToDoListModel"("Id") ON DELETE SET NULL
);

--Таблица Notifications (с индексом на UserId)
CREATE TABLE public."Notifications" (
    "id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "user_id" uuid NOT NULL REFERENCES public."ToDoUser"("UserId") ON DELETE CASCADE,
    "type" text NOT NULL,
    "text" text NOT NULL,
    "scheduled_at" timestamptz NOT NULL,
    "is_notified" boolean NOT NULL DEFAULT false,
    "notified_at" timestamptz
);

-- Индексы
CREATE INDEX idx_notifications_user_id ON public."Notifications"("user_id");
CREATE INDEX idx_notifications_scheduled_at_is_notified ON public."Notifications"("scheduled_at") 
WHERE "is_notified" = false;