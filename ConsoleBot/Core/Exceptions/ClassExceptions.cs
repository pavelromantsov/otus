using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.Core.Exceptions
{
    public class TaskCountLimitException : Exception
    {
        public TaskCountLimitException(int taskCountLimit) :
            base($"Превышено максимальное количество задач " +
                $"равное {taskCountLimit}")
        { }
    }

    // Исключение для ограничения длины задачи
    public class TaskLengthLimitException : Exception
    {
        public TaskLengthLimitException(int taskLength, int taskLengthLimit) :
            base($"Длина задачи ({taskLength}) превышает максимально " +
                $"допустимое значение {taskLengthLimit}")
        { }
    }

    // Исключение для дублирующей задачи
    public class DuplicateTaskException : Exception
    {
        public DuplicateTaskException(string task) :
            base($"Задача '{task}' уже существует")
        { }
    }
}