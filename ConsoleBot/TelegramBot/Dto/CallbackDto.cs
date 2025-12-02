using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.TelegramBot.Dto
{
    public class CallbackDto
    {
        public string Action { get; set; } //с помощью него будет определять за какое действие отвечает кнопка
        public CallbackDto(string action) 
        { 
            Action = action; 
        }

        public static CallbackDto FromString(string input) //На вход принимает строку ввида "{action}|{prop1}|{prop2}...". Нужно создать CallbackDto с Action = action. Нужно учесть что в строке может не быть |, тогда всю строку сохраняем в Action.
        {
            if (input.Contains("|"))
            {
                var parts = input.Split('|');
                return new CallbackDto(parts[0]); // Берём первый элемент как Action
            }
            else
            {
                return new CallbackDto(input); // Вся строка принимается как Action
            }
        }

        public override string ToString()
        {
            return Action;
        }
    }
}
