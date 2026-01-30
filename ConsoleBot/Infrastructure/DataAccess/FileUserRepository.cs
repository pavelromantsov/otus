using System.Text.Json;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class FileUserRepository : IUserRepository
    {
        private readonly string _baseDirectory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); 

        public FileUserRepository(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            Directory.CreateDirectory(baseDirectory); 
        }

        // Получить пользователя по UserId
        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                var filePath = Path.Combine(_baseDirectory, $"{userId}.json");
                if (!File.Exists(filePath))
                    return null;
                var content = await File.ReadAllTextAsync(filePath, ct);
                return JsonSerializer.Deserialize<ToDoUser>(content)!;
            }
            finally { _semaphore.Release(); }
        }

        // Получить пользователя по TelegramUserId
        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
        {
            var users = await GetAllUsersAsync(telegramUserId, ct);
            return users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
        }

        // Добавить пользователя
        public async Task AddAsync(ToDoUser user, CancellationToken ct)
        {
                var filePath = Path.Combine(_baseDirectory, $"{user.UserId}.json");
                var serializedData = JsonSerializer.Serialize(user);
                await File.WriteAllTextAsync(filePath, serializedData, ct);            

        }

        // Вернуть всех пользователей
        public async Task<List<ToDoUser>> GetAllUsersAsync(long telegramUserId, CancellationToken ct)
         {
            var files = Directory.EnumerateFiles(_baseDirectory, "*.json").ToList();
            //LINQ
            var result = await Task.WhenAll(
                files.Select(async f =>
                {
                    var content = await File.ReadAllTextAsync(f, ct);
                    return JsonSerializer.Deserialize<ToDoUser>(content);
                }));

            return result
                .Where(u => u != null)
                .Cast<ToDoUser>()
                .ToList();
        }

        public async Task<ToDoUser?> GetUserAsync(long userId,  CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                var user = await GetUserByTelegramUserIdAsync(userId, ct);
                var filePath = Path.Combine(_baseDirectory, $"{user.UserId}.json");
                if (!File.Exists(filePath))
                {
                    var newUser = new ToDoUser();
                    await AddAsync(newUser, ct); 
                    return newUser;
                }
                var content = await File.ReadAllTextAsync(filePath, ct);
                return JsonSerializer.Deserialize<ToDoUser>(content)!;            
            }
            finally { _semaphore.Release(); }

        }
    }
}
