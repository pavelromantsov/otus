using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class FileToDoListRepository : IToDoListRepository
    {

        private readonly string _baseDirectory;
        //private readonly string _toDoUser;

        public FileToDoListRepository(string baseDirectory)
        {
            _baseDirectory = Path.Combine(baseDirectory, "todolists.json"); ;
            Directory.CreateDirectory(baseDirectory); // Создаем директорию, если её нет
        }


        // Получает список задач по идентификатору.

        public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            var data = await ReadData(ct);
            return data.FirstOrDefault(x => x.Id == id);
        }

        
        // Получает все списки задач пользователя.  
        public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
        {
            var data = await ReadData(ct);
            //var user = ToDoUser.UserId;

            return data.Where(x => x.User.UserId == userId).ToList();
        }

        // Добавляет новый список задач.
        
        public async Task Add(ToDoList list, CancellationToken ct)
        {
            var data = await ReadData(ct);
            data.Add(list);
            await WriteData(data, ct);
        }

        
        // Удаляет список задач по идентификатору.
        
        public async Task Delete(Guid id, CancellationToken ct)
        {
            var data = await ReadData(ct);
            var index = data.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                data.RemoveAt(index);
                await WriteData(data, ct);
            }
        }

        // Проверяет, существует ли список с таким именем у пользователя.

        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            var data = await ReadData(ct);
            return data.Any(x => x.User.UserId == userId && x.Name == name);
        }

        private async Task<List<ToDoList>> ReadData(CancellationToken ct)
        {
            if (!File.Exists(_baseDirectory)) return new List<ToDoList>();

            var content = await File.ReadAllTextAsync(_baseDirectory, ct);
            return JsonSerializer.Deserialize<List<ToDoList>>(content);
                //?? new List<ToDoList>();
        }

        private async Task WriteData(List<ToDoList> data, CancellationToken ct)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var serialized = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(_baseDirectory, serialized, ct);
        }

        }
}
