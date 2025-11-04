using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using System.IO;
using System.Data.SqlTypes;


namespace ConsoleBot.Infrastructure.DataAccess
{

    public class FileToDoRepository : IToDoRepository
    {
        private readonly string _baseDirectory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // разрешаем только одному потоку читать/писать одновременно
        private readonly ConcurrentDictionary<Guid, Guid> _index;


        // Key: ToDoItemId, Value: UserId
        private readonly object _lockObject = new object();
        
        public FileToDoRepository(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            Directory.CreateDirectory(baseDirectory); // Создаем директорию, если её нет
            _index = LoadIndex();
        }
        private ConcurrentDictionary<Guid, Guid> LoadIndex()
        {
            var indexFilePath = Path.Combine(_baseDirectory, "index.json");
            if (!File.Exists(indexFilePath))
                return new ConcurrentDictionary<Guid, Guid>();

            var content = File.ReadAllText(indexFilePath);
            return JsonSerializer.Deserialize<ConcurrentDictionary<Guid, Guid>>(content)!;            
        }
        // Сохранение индекса в файл
        private void SaveIndex()
        {
            lock (_lockObject)
            {
                var indexFilePath = Path.Combine(_baseDirectory, "index.json");
                var serializedData = JsonSerializer.Serialize(_index);
                File.WriteAllText(indexFilePath, serializedData);
            }
        }
        // Возвращает все задачи пользователя
        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken); // ждём освобождения семафора
            try
            {
                var directory = Path.Combine(_baseDirectory, userId.ToString());
                if (!Directory.Exists(directory))
                    return Array.Empty<ToDoItem>();

                var files = Directory.EnumerateFiles(directory, "*.json").Select(file => file.Replace(".json", ""));
                var result = await Task.WhenAll(files.Select(async f =>
                {
                    var content = await File.ReadAllTextAsync(f + ".json", cancellationToken);
                    return JsonSerializer.Deserialize<ToDoItem>(content)!;
                }));

                return result.ToList();
            }
            finally {_semaphore.Release();}
        }

        // Возвращает активную задачу пользователя
        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
                    var allTasks = await GetAllByUserIdAsync(userId, cancellationToken);
                    return allTasks.Where(t => t.State == ToDoItemState.Active).ToList();
        }

        // Возвращает задачу по ID
        public async Task<ToDoItem?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken); // ждём освобождения семафора
            try
            {
                var directories = Directory.EnumerateDirectories(_baseDirectory);
                foreach (var dir in directories)
                {
                    var filePath = Path.Combine(dir, $"{id}.json");
                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                        return JsonSerializer.Deserialize<ToDoItem>(content)!;
                    }
                }
                return null;
            }
            finally {_semaphore.Release();}
        }

        // Добавляет задачу
        public async Task AddAsync(ToDoItem item, CancellationToken cancellationToken)
        {

                var directory = Path.Combine(_baseDirectory, item.User.UserId.ToString());
                Directory.CreateDirectory(directory); // Создаем папку пользователя, если её нет
                var filePath = Path.Combine(directory, $"{item.Id}.json");
                var serializedData = JsonSerializer.Serialize(item);
                await File.WriteAllTextAsync(filePath, serializedData, cancellationToken);
                _index[item.Id] = item.User.UserId;
                SaveIndex();
        }

        // Обновляет задачу
        public async Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken)
        {
            await AddAsync(item, cancellationToken); // Сохраняем заново файл с обновлёнными данными
        }

        // Удаляет задачу
        public async Task Delete(Guid id, CancellationToken cancellationToken)
        {
                
                if (_index.TryRemove(id, out var userId))
                {
                    var directory = Path.Combine(_baseDirectory, userId.ToString());
                    var filePath = Path.Combine(directory, $"{id}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                SaveIndex();
        }

        // Проверяет, существует ли задача с таким именем у пользователя
        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken)
        {
            var tasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return tasks.Any(t => t.Name == name);
        }

        // Количество активных задач пользователя
        public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken)
        {
            var tasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return tasks.Count(t => t.State == ToDoItemState.Active);
        }

        public async Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken)
        {
            var tasks = await GetAllByUserIdAsync(userId, cancellationToken);
            return tasks.Where(task => task.User.UserId == userId && predicate(task)).ToList();
        }
    }

}
