using Core.DTOs;
using Core.Helpers;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId)
        {
            return await _context.Conversations.FirstOrDefaultAsync(conversation => conversation.Id == conversationId);
        }

        public async Task<Conversation?> GetConversationByApplicationIdAsync(Guid applicationId)
        {
            return await _context.Conversations.FirstOrDefaultAsync(conversation => conversation.ApplicationId == applicationId);
        }

        public async Task<bool> UserIsParticipantAsync(Guid userId, Guid conversationId)
        {
            return await _context.Conversations.AnyAsync(conversation =>
                conversation.Id == conversationId
                && (conversation.EmployerId == userId || conversation.EmployeeId == userId));
        }

        public async Task<List<ChatConversationListItemDTO>> GetConversationsForEmployerAsync(
            Guid employerId,
            ConversationStatusEnum status)
        {
            var rows = await (
                from conversation in _context.Conversations
                where conversation.EmployerId == employerId && conversation.Status == status
                join jobPost in _context.JobPosts on conversation.JobPostId equals jobPost.Id
                join employee in _context.Users.OfType<Employee>() on conversation.EmployeeId equals employee.Id
                join location in _context.RestaurantLocations on jobPost.RestaurantLocationId equals location.Id into locationGroup
                from location in locationGroup.DefaultIfEmpty()
                select new ConversationRow(
                    conversation.Id,
                    conversation.ApplicationId,
                    conversation.JobPostId,
                    jobPost.Title,
                    location != null ? location.Name : null,
                    location != null ? location.City : null,
                    employee.FirstName + " " + employee.LastName,
                    employee.Id,
                    employee.ProfilePhoto,
                    conversation.CreatedAtUtc,
                    conversation.Status,
                    conversation.LastMessageAtUtc)).ToListAsync();

            return await MapConversationRowsAsync(rows, employerId);
        }

        public async Task<List<ChatConversationListItemDTO>> GetConversationsForEmployeeAsync(
            Guid employeeId,
            ConversationStatusEnum status)
        {
            var rows = await (
                from conversation in _context.Conversations
                where conversation.EmployeeId == employeeId && conversation.Status == status
                join jobPost in _context.JobPosts on conversation.JobPostId equals jobPost.Id
                join employer in _context.Users.OfType<Employer>() on conversation.EmployerId equals employer.Id
                join location in _context.RestaurantLocations on jobPost.RestaurantLocationId equals location.Id into locationGroup
                from location in locationGroup.DefaultIfEmpty()
                select new ConversationRow(
                    conversation.Id,
                    conversation.ApplicationId,
                    conversation.JobPostId,
                    jobPost.Title,
                    location != null ? location.Name : null,
                    location != null ? location.City : null,
                    employer.Name,
                    employer.Id,
                    employer.ProfilePhoto,
                    conversation.CreatedAtUtc,
                    conversation.Status,
                    conversation.LastMessageAtUtc)).ToListAsync();

            return await MapConversationRowsAsync(rows, employeeId);
        }

        public async Task<int> GetTotalUnreadCountAsync(Guid userId)
        {
            var conversationIds = await _context.Conversations
                .Where(conversation =>
                    (conversation.EmployerId == userId || conversation.EmployeeId == userId)
                    && conversation.Status == ConversationStatusEnum.Active)
                .Select(conversation => conversation.Id)
                .ToListAsync();

            var unreadCounts = await GetUnreadCountsAsync(userId, conversationIds);
            return unreadCounts.Values.Sum();
        }

        public async Task ArchiveExpiredConversationsForUserAsync(Guid userId, DateTime utcNow)
        {
            var archiveCutoff = utcNow.AddHours(-ChatRules.ArchiveHoursAfterShiftStart);

            var conversations = await (
                from conversation in _context.Conversations
                join jobPost in _context.JobPosts on conversation.JobPostId equals jobPost.Id
                where (conversation.EmployerId == userId || conversation.EmployeeId == userId)
                      && conversation.Status == ConversationStatusEnum.Active
                      && jobPost.StartingDate <= archiveCutoff
                select conversation).ToListAsync();

            foreach (var conversation in conversations)
            {
                conversation.Archive(utcNow);
                _context.Conversations.Update(conversation);
            }
        }

        public async Task NormalizeEmptyConversationStatusesAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE [Conversations] SET [Status] = N'Active' WHERE [Status] = N'' OR [Status] IS NULL");
        }

        public async Task MarkConversationReadAsync(Guid userId, Guid conversationId, DateTime readAtUtc)
        {
            var existingState = await _context.ConversationReadStates
                .FirstOrDefaultAsync(state =>
                    state.UserId == userId
                    && state.ConversationId == conversationId);

            if (existingState == null)
            {
                await _context.ConversationReadStates.AddAsync(
                    ConversationReadState.Create(userId, conversationId, readAtUtc));
                return;
            }

            if (readAtUtc > existingState.LastReadAtUtc)
            {
                existingState.MarkRead(readAtUtc);
                _context.ConversationReadStates.Update(existingState);
            }
        }

        public async Task<List<ChatMessageDTO>> GetMessagesAsync(Guid conversationId)
        {
            return await _context.ChatMessages
                .Where(message => message.ConversationId == conversationId)
                .OrderBy(message => message.SentAtUtc)
                .Select(message => new ChatMessageDTO
                {
                    Id = message.Id,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    SentAtUtc = message.SentAtUtc
                })
                .ToListAsync();
        }

        public async Task AddConversationAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
        }

        public void UpdateConversation(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
        }

        public async Task AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
        }

        private async Task<List<ChatConversationListItemDTO>> MapConversationRowsAsync(List<ConversationRow> rows, Guid userId)
        {
            if (rows.Count == 0)
                return new List<ChatConversationListItemDTO>();

            var conversationIds = rows.Select(row => row.Id).ToList();
            var unreadCounts = await GetUnreadCountsAsync(userId, conversationIds);
            var lastMessages = await _context.ChatMessages
                .Where(message => conversationIds.Contains(message.ConversationId))
                .GroupBy(message => message.ConversationId)
                .Select(group => new
                {
                    ConversationId = group.Key,
                    Message = group.OrderByDescending(message => message.SentAtUtc).First()
                })
                .ToDictionaryAsync(item => item.ConversationId, item => item.Message);

            return rows
                .Select(row =>
                {
                    lastMessages.TryGetValue(row.Id, out var lastMessage);
                    var isArchived = row.Status == ConversationStatusEnum.Archived;

                    return new ChatConversationListItemDTO
                    {
                        ConversationId = row.Id,
                        ApplicationId = row.ApplicationId,
                        JobPostId = row.JobPostId,
                        JobPostTitle = row.JobPostTitle,
                        RestaurantLocationName = row.RestaurantLocationName,
                        RestaurantLocationCity = row.RestaurantLocationCity,
                        OtherPartyName = row.OtherPartyName,
                        OtherPartyId = row.OtherPartyId,
                        OtherPartyProfilePhoto = row.OtherPartyProfilePhoto,
                        LastMessagePreview = lastMessage?.Content,
                        LastMessageAtUtc = row.LastMessageAtUtc ?? lastMessage?.SentAtUtc ?? row.CreatedAtUtc,
                        UnreadCount = isArchived ? 0 : unreadCounts.GetValueOrDefault(row.Id),
                        Status = row.Status.ToString(),
                        IsReadOnly = isArchived,
                        CanSendMessages = !isArchived
                    };
                })
                .OrderByDescending(item => item.LastMessageAtUtc)
                .ToList();
        }

        private async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid userId, List<Guid> conversationIds)
        {
            if (conversationIds.Count == 0)
                return new Dictionary<Guid, int>();

            var readStates = await _context.ConversationReadStates
                .Where(state => state.UserId == userId && conversationIds.Contains(state.ConversationId))
                .ToDictionaryAsync(state => state.ConversationId, state => state.LastReadAtUtc);

            var incomingMessages = await _context.ChatMessages
                .Where(message =>
                    conversationIds.Contains(message.ConversationId)
                    && message.SenderId != userId)
                .Select(message => new { message.ConversationId, message.SentAtUtc })
                .ToListAsync();

            return incomingMessages
                .GroupBy(message => message.ConversationId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(message =>
                    {
                        var lastReadAt = readStates.GetValueOrDefault(group.Key, DateTime.MinValue);
                        return message.SentAtUtc > lastReadAt;
                    }));
        }

        private sealed record ConversationRow(
            Guid Id,
            Guid ApplicationId,
            Guid JobPostId,
            string JobPostTitle,
            string? RestaurantLocationName,
            string? RestaurantLocationCity,
            string OtherPartyName,
            Guid OtherPartyId,
            string? OtherPartyProfilePhoto,
            DateTime CreatedAtUtc,
            ConversationStatusEnum Status,
            DateTime? LastMessageAtUtc);
    }
}
