using System.Text.Json;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class FileUserRepository : IUserRepository
    {
        private readonly string _baseDirectory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // разрешаем только одному потоку читать/писать одновременно

        public FileUserRepository(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            Directory.CreateDirectory(baseDirectory); // Создаем директорию, если её нет
        }

        // Получить пользователя по UserId
        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var filePath = Path.Combine(_baseDirectory, $"{userId}.json");
                if (!File.Exists(filePath))
                    return null;
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize<ToDoUser>(content)!;
            }
            finally { _semaphore.Release(); }
        }

        // Получить пользователя по TelegramUserId
        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            var users = await GetAllUsersAsync(telegramUserId, cancellationToken);
            return users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
        }

        // Добавить пользователя
        public async Task AddAsync(ToDoUser user, CancellationToken cancellationToken)
        {
                var filePath = Path.Combine(_baseDirectory, $"{user.UserId}.json");
                var serializedData = JsonSerializer.Serialize(user);
                await File.WriteAllTextAsync(filePath, serializedData, cancellationToken);            

        }

        // Вернуть всех пользователей
        public async Task<List<ToDoUser>> GetAllUsersAsync(long telegramUserId, CancellationToken cancellationToken)
        {

                var files = Directory.EnumerateFiles(_baseDirectory, "*.json");
                var result =await Task.WhenAll(files.Select(async f =>
                {
                    var content = await File.ReadAllTextAsync(f, cancellationToken);
                    return JsonSerializer.Deserialize<ToDoUser>(content)!;
                }));
                return result.ToList();

        }

        public async Task<ToDoUser?> GetUserAsync(long userId,  CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var user = await GetUserByTelegramUserIdAsync(userId, cancellationToken);
                var filePath = Path.Combine(_baseDirectory, $"{user.UserId}.json");
                if (!File.Exists(filePath))
                {
                    var newUser = new ToDoUser(userId, cancellationToken);
                    await AddAsync(newUser, cancellationToken); // Сохраняем пользователя в файл
                    return newUser;
                }
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize<ToDoUser>(content)!;            
            }
            finally { _semaphore.Release(); }

        }
    }
}
