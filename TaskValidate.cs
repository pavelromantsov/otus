using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBotCommands
{
    public static class TaskValidate
    {
        internal static int ParseAndValidateInt(string? str, int min, int max)
        {
            if (int.TryParse(str, out int number) && number >= min && number <= max)
            {
                return number;
            }
                throw new ArgumentException($"Значение должно быть числом от {min} до {max}.");
        }
            
        internal static void ValidateString(string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("Строка не должна быть пустой или содержать только пробелы");
            }

        }
    }
}
