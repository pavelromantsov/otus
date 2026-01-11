using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using ConsoleBot.Infrastructure.DataAccess;

namespace ConsoleBot.Core.Services
{
    public class ToDoListService:IToDoListService
    {
        private readonly IToDoListRepository _todoListRepository;
        public ToDoListService(IToDoListRepository todoListRepository)
        {
            _todoListRepository = todoListRepository;
        }

        // Добавляет новый список задач для пользователя.
        // Имя списка ограничено длиной до 10 символов и должно быть уникальным среди всех списков пользователя.
        public async Task<ToDoList> Add(ToDoUser user, string name, CancellationToken ct)
        {
            if (name.Length > 10)
                throw new ArgumentException("Имя списка не может превышать 10 символов.");

            if (await _todoListRepository.ExistsByName(user.UserId, name, ct))
                throw new ArgumentException("Список с таким именем уже существует.");

            var newList = new ToDoList
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.UtcNow,
                User = user  
            };

            await _todoListRepository.Add(newList, ct);
            return newList;
        }

        // Получает список задач по идентификатору.
        public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            return await _todoListRepository.Get(id, ct);
        }

        // Удаляет список задач по идентификатору.
        public async Task Delete(Guid id, CancellationToken ct)
        {
            await _todoListRepository.Delete(id, ct);
        }

        // Получает все списки задач пользователя.
        public async Task<IReadOnlyList<ToDoList>> GetUserLists(Guid userId, CancellationToken ct)
        {
            return await _todoListRepository.GetByUserId(userId, ct);
        }
    }
}
