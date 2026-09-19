using Application.Interfaces.Persistence;
using Application.Models.Chat;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Persistence.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ChatService> _logger;

        public ChatService(AppDbContext dbContext, ILogger<ChatService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ChatPermissionResult> CheckChatPermissionAsync(long currentUserId, long targetUserId)
        {
            if (currentUserId <= 0 || targetUserId <= 0 || currentUserId == targetUserId)
            {
                return new ChatPermissionResult
                {
                    Allowed = false,
                    Reason = "Invalid user identification.",
                    RequiresUpgrade = false
                };
            }

            // 1. Check target user exists and is active
            var targetUser = await _dbContext.Registration
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == targetUserId && !r.IsDeleted && r.IsActive);

            if (targetUser == null)
            {
                return new ChatPermissionResult
                {
                    Allowed = false,
                    Reason = "The requested profile is no longer available.",
                    RequiresUpgrade = false
                };
            }

            // 2. Check blocking (either user has not-liked or blocked the other)
            bool isBlocked = await _dbContext.UserNotLikeProfiles
                .AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && (
                    (x.UserId == currentUserId && x.NotLikedId == targetUserId) ||
                    (x.UserId == targetUserId && x.NotLikedId == currentUserId)
                ));

            if (!isBlocked)
            {
                long u1 = Math.Min(currentUserId, targetUserId);
                long u2 = Math.Max(currentUserId, targetUserId);
                isBlocked = await _dbContext.Conversations
                    .AsNoTracking()
                    .AnyAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser && c.User1Id == u1 && c.User2Id == u2 && c.IsBlocked);
            }

            if (isBlocked)
            {
                return new ChatPermissionResult
                {
                    Allowed = false,
                    Reason = "Messaging is unavailable for this conversation.",
                    RequiresUpgrade = false
                };
            }

            // 3. Check membership & paid plan status
            var currentUser = await _dbContext.Registration
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == currentUserId && !r.IsDeleted && r.IsActive);

            if (currentUser == null)
            {
                return new ChatPermissionResult
                {
                    Allowed = false,
                    Reason = "Current user profile not found or inactive.",
                    RequiresUpgrade = false
                };
            }

            bool isPremium = currentUser.IsPremiumMember;
            bool hasActivePlan = await _dbContext.PlanPurchases
                .AsNoTracking()
                .AnyAsync(p => p.UserId == currentUserId && !p.IsDeleted && p.ExpiresAt > DateTime.UtcNow);

            // 4. Check mutual interest (Accepted interest status)
            bool hasAcceptedInterest = await _dbContext.Userfavoriteprofile
                .AsNoTracking()
                .AnyAsync(f => !f.IsDeleted && f.Status == InterestStatus.Accepted && (
                    (f.UserId == currentUserId && f.LikedId == targetUserId) ||
                    (f.UserId == targetUserId && f.LikedId == currentUserId)
                ));

            if (isPremium || hasActivePlan || hasAcceptedInterest)
            {
                return new ChatPermissionResult
                {
                    Allowed = true,
                    Reason = null,
                    RequiresUpgrade = false
                };
            }

            return new ChatPermissionResult
            {
                Allowed = false,
                Reason = "Upgrade your membership plan or establish mutual interest to start conversations.",
                RequiresUpgrade = true,
                IsMutualInterestRequired = true
            };
        }

        public async Task<ConversationDto> GetOrCreateUserConversationAsync(long currentUserId, long targetUserId)
        {
            if (currentUserId <= 0 || targetUserId <= 0 || currentUserId == targetUserId)
            {
                throw new ArgumentException("Invalid user IDs for 1-to-1 conversation.");
            }

            long u1 = Math.Min(currentUserId, targetUserId);
            long u2 = Math.Max(currentUserId, targetUserId);

            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser && c.User1Id == u1 && c.User2Id == u2);

            if (conv == null)
            {
                var perm = await CheckChatPermissionAsync(currentUserId, targetUserId);
                if (!perm.Allowed)
                {
                    throw new InvalidOperationException(perm.Reason ?? "Permission denied to start this conversation.");
                }

                conv = new Conversation
                {
                    Type = ConversationType.UserToUser,
                    User1Id = u1,
                    User2Id = u2,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = currentUserId.ToString(),
                    IsActive = true
                };

                _dbContext.Conversations.Add(conv);
                await _dbContext.SaveChangesAsync(currentUserId.ToString());
            }

            var otherId = currentUserId == u1 ? u2 : u1;
            var participant = await _dbContext.Registration
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == otherId);

            return MapToConversationDto(conv, currentUserId, participant);
        }

        public async Task<ConversationDto> GetOrCreateSupportConversationAsync(long currentUserId, string? subject = null)
        {
            if (currentUserId <= 0)
            {
                throw new ArgumentException("Invalid user ID for support conversation.");
            }

            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => !c.IsDeleted && c.Type == ConversationType.Support && c.User1Id == currentUserId && (c.SupportStatus == SupportTicketStatus.Open || c.SupportStatus == SupportTicketStatus.InProgress));

            if (conv == null)
            {
                conv = new Conversation
                {
                    Type = ConversationType.Support,
                    User1Id = currentUserId,
                    SupportStatus = SupportTicketStatus.Open,
                    SupportSubject = string.IsNullOrWhiteSpace(subject) ? "Support & Help Request" : subject.Trim(),
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = currentUserId.ToString(),
                    IsActive = true
                };

                _dbContext.Conversations.Add(conv);
                await _dbContext.SaveChangesAsync(currentUserId.ToString());
            }

            return MapToConversationDto(conv, currentUserId, null);
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(long currentUserId)
        {
            if (currentUserId <= 0) return new List<ConversationDto>();

            var conversations = await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => !c.IsDeleted && (
                    (c.Type == ConversationType.UserToUser && (c.User1Id == currentUserId || c.User2Id == currentUserId)) ||
                    (c.Type == ConversationType.Support && c.User1Id == currentUserId)
                ))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedOn)
                .ToListAsync();

            var otherUserIds = conversations
                .Where(c => c.Type == ConversationType.UserToUser)
                .Select(c => c.User1Id == currentUserId ? (c.User2Id ?? 0) : c.User1Id)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var participants = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => otherUserIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            var result = new List<ConversationDto>();
            foreach (var conv in conversations)
            {
                Registration? participant = null;
                if (conv.Type == ConversationType.UserToUser)
                {
                    long otherId = conv.User1Id == currentUserId ? (conv.User2Id ?? 0) : conv.User1Id;
                    participants.TryGetValue(otherId, out participant);
                }

                result.Add(MapToConversationDto(conv, currentUserId, participant));
            }

            return result;
        }

        public async Task<List<ConversationDto>> GetStaffSupportTicketsAsync(SupportTicketStatus? statusFilter = null, long? assignedStaffId = null)
        {
            var query = _dbContext.Conversations
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.Type == ConversationType.Support);

            if (statusFilter.HasValue)
            {
                query = query.Where(c => c.SupportStatus == statusFilter.Value);
            }

            if (assignedStaffId.HasValue && assignedStaffId.Value > 0)
            {
                query = query.Where(c => c.AssignedStaffId == assignedStaffId.Value);
            }

            var conversations = await query
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedOn)
                .ToListAsync();

            var userIds = conversations.Select(c => c.User1Id).Distinct().ToList();
            var users = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => userIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            var result = new List<ConversationDto>();
            foreach (var conv in conversations)
            {
                users.TryGetValue(conv.User1Id, out var user);
                result.Add(MapToStaffSupportDto(conv, user));
            }

            return result;
        }

        public async Task<ConversationDto?> GetConversationByIdAsync(long conversationId, long currentUserId, bool isStaff = false)
        {
            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && !c.IsDeleted);

            if (conv == null) return null;

            if (!isStaff)
            {
                bool isParticipant = (conv.Type == ConversationType.UserToUser && (conv.User1Id == currentUserId || conv.User2Id == currentUserId))
                    || (conv.Type == ConversationType.Support && conv.User1Id == currentUserId);

                if (!isParticipant) return null;
            }

            Registration? participant = null;
            if (conv.Type == ConversationType.UserToUser)
            {
                long otherId = conv.User1Id == currentUserId ? (conv.User2Id ?? 0) : conv.User1Id;
                participant = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(r => r.Id == otherId);
            }
            else if (isStaff)
            {
                participant = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(r => r.Id == conv.User1Id);
                return MapToStaffSupportDto(conv, participant);
            }

            return MapToConversationDto(conv, currentUserId, participant);
        }

        public async Task<ChatMessageDto> SendUserMessageAsync(long senderUserId, SendMessageRequest request)
        {
            if (senderUserId <= 0 || request.ReceiverId <= 0 || senderUserId == request.ReceiverId)
            {
                throw new ArgumentException("Invalid sender or receiver.");
            }

            if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            {
                throw new ArgumentException("Message content or attachment is required.");
            }

            var perm = await CheckChatPermissionAsync(senderUserId, request.ReceiverId);
            if (!perm.Allowed)
            {
                throw new InvalidOperationException(perm.Reason ?? "Permission denied to send message.");
            }

            long u1 = Math.Min(senderUserId, request.ReceiverId);
            long u2 = Math.Max(senderUserId, request.ReceiverId);

            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser && c.User1Id == u1 && c.User2Id == u2);

            if (conv == null)
            {
                conv = new Conversation
                {
                    Type = ConversationType.UserToUser,
                    User1Id = u1,
                    User2Id = u2,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = senderUserId.ToString(),
                    IsActive = true
                };
                _dbContext.Conversations.Add(conv);
                await _dbContext.SaveChangesAsync(senderUserId.ToString());
            }

            var message = new ChatMessage
            {
                ConversationId = conv.Id,
                SenderId = senderUserId,
                SenderRole = MessageSenderRole.User,
                ReceiverId = request.ReceiverId,
                Content = request.Content?.Trim() ?? string.Empty,
                MessageType = request.MessageType,
                AttachmentUrl = request.AttachmentUrl,
                AttachmentFileName = request.AttachmentFileName,
                AttachmentFileSize = request.AttachmentFileSize,
                Status = ChatMessageStatus.Sent,
                SentAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = senderUserId.ToString(),
                IsActive = true
            };

            _dbContext.ChatMessages.Add(message);
            await _dbContext.SaveChangesAsync(senderUserId.ToString());

            // Update conversation summaries
            conv.LastMessageId = message.Id;
            conv.LastMessageAt = message.SentAt;
            conv.LastMessagePreview = GetPreviewText(message);
            conv.LastMessageSenderId = senderUserId;

            if (senderUserId == conv.User1Id)
            {
                conv.User2UnreadCount += 1;
            }
            else
            {
                conv.User1UnreadCount += 1;
            }

            await _dbContext.SaveChangesAsync(senderUserId.ToString());

            var sender = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(r => r.Id == senderUserId);

            return MapToChatMessageDto(message, senderUserId, sender);
        }

        public async Task<ChatMessageDto> SendSupportMessageAsync(long senderId, MessageSenderRole senderRole, SendSupportMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            {
                throw new ArgumentException("Message content or attachment is required.");
            }

            Conversation? conv = null;

            if (request.ConversationId.HasValue && request.ConversationId.Value > 0)
            {
                conv = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value && c.Type == ConversationType.Support && !c.IsDeleted);
            }

            if (conv == null && senderRole == MessageSenderRole.User)
            {
                conv = new Conversation
                {
                    Type = ConversationType.Support,
                    User1Id = senderId,
                    SupportStatus = SupportTicketStatus.Open,
                    SupportSubject = string.IsNullOrWhiteSpace(request.Subject) ? "Support & Help Request" : request.Subject.Trim(),
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = senderId.ToString(),
                    IsActive = true
                };
                _dbContext.Conversations.Add(conv);
                await _dbContext.SaveChangesAsync(senderId.ToString());
            }

            if (conv == null)
            {
                throw new InvalidOperationException("Support conversation not found.");
            }

            var message = new ChatMessage
            {
                ConversationId = conv.Id,
                SenderId = senderId,
                SenderRole = senderRole,
                ReceiverId = senderRole == MessageSenderRole.Staff ? conv.User1Id : (long?)null,
                Content = request.Content?.Trim() ?? string.Empty,
                MessageType = request.MessageType,
                AttachmentUrl = request.AttachmentUrl,
                AttachmentFileName = request.AttachmentFileName,
                AttachmentFileSize = request.AttachmentFileSize,
                Status = ChatMessageStatus.Sent,
                SentAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = senderId.ToString(),
                IsActive = true
            };

            _dbContext.ChatMessages.Add(message);
            await _dbContext.SaveChangesAsync(senderId.ToString());

            // Update conversation summaries
            conv.LastMessageId = message.Id;
            conv.LastMessageAt = message.SentAt;
            conv.LastMessagePreview = GetPreviewText(message);
            conv.LastMessageSenderId = senderId;

            if (senderRole == MessageSenderRole.Staff)
            {
                conv.User1UnreadCount += 1;
                if (conv.SupportStatus == SupportTicketStatus.Open)
                {
                    conv.SupportStatus = SupportTicketStatus.InProgress;
                }
                if (!conv.AssignedStaffId.HasValue)
                {
                    conv.AssignedStaffId = senderId;
                }
            }
            else
            {
                conv.SupportUnreadCount += 1;
                if (conv.SupportStatus == SupportTicketStatus.Resolved || conv.SupportStatus == SupportTicketStatus.Closed)
                {
                    conv.SupportStatus = SupportTicketStatus.InProgress;
                }
            }

            await _dbContext.SaveChangesAsync(senderId.ToString());

            string senderName = "Support Team";
            string? senderPhoto = null;

            if (senderRole == MessageSenderRole.User)
            {
                var user = await _dbContext.Registration.AsNoTracking().FirstOrDefaultAsync(r => r.Id == senderId);
                if (user != null)
                {
                    senderName = user.Name ?? "User";
                    senderPhoto = user.ImagePath;
                }
            }

            return new ChatMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                SenderRole = message.SenderRole,
                SenderName = senderName,
                SenderPhotoUrl = senderPhoto,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                MessageType = message.MessageType,
                AttachmentUrl = message.AttachmentUrl,
                AttachmentFileName = message.AttachmentFileName,
                AttachmentFileSize = message.AttachmentFileSize,
                Status = message.Status,
                SentAt = message.SentAt,
                DeliveredAt = message.DeliveredAt,
                ReadAt = message.ReadAt,
                IsMine = true
            };
        }

        public async Task<PaginatedChatMessagesDto> GetMessageHistoryAsync(long conversationId, long currentUserId, bool isStaff, int page = 1, int pageSize = 30)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 30;

            var conv = await _dbContext.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId && !c.IsDeleted);

            if (conv == null)
            {
                throw new KeyNotFoundException("Conversation not found.");
            }

            if (!isStaff)
            {
                bool isParticipant = (conv.Type == ConversationType.UserToUser && (conv.User1Id == currentUserId || conv.User2Id == currentUserId))
                    || (conv.Type == ConversationType.Support && conv.User1Id == currentUserId);

                if (!isParticipant)
                {
                    throw new UnauthorizedAccessException("Unauthorized to access this conversation history.");
                }
            }

            var query = _dbContext.ChatMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted);

            if (!isStaff)
            {
                query = query.Where(m => !(m.SenderId == currentUserId ? m.IsDeletedForSender : m.IsDeletedForReceiver));
            }

            int totalCount = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Reverse for chronological presentation
            messages.Reverse();

            var senderIds = messages.Where(m => m.SenderRole == MessageSenderRole.User).Select(m => m.SenderId).Distinct().ToList();
            var senders = await _dbContext.Registration
                .AsNoTracking()
                .Where(r => senderIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            var dtoList = messages.Select(m =>
            {
                string sName = m.SenderRole == MessageSenderRole.Staff ? "Support Staff" : (m.SenderRole == MessageSenderRole.System ? "System" : "User");
                string? sPhoto = null;

                if (m.SenderRole == MessageSenderRole.User && senders.TryGetValue(m.SenderId, out var u))
                {
                    sName = u.Name ?? "User";
                    sPhoto = u.ImagePath;
                }

                bool isMine = isStaff ? (m.SenderRole == MessageSenderRole.Staff && m.SenderId == currentUserId) : (m.SenderId == currentUserId && m.SenderRole == MessageSenderRole.User);

                return new ChatMessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderRole = m.SenderRole,
                    SenderName = sName,
                    SenderPhotoUrl = sPhoto,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    MessageType = m.MessageType,
                    AttachmentUrl = m.AttachmentUrl,
                    AttachmentFileName = m.AttachmentFileName,
                    AttachmentFileSize = m.AttachmentFileSize,
                    Status = m.Status,
                    SentAt = m.SentAt,
                    DeliveredAt = m.DeliveredAt,
                    ReadAt = m.ReadAt,
                    IsMine = isMine
                };
            }).ToList();

            return new PaginatedChatMessagesDto
            {
                ConversationId = conversationId,
                Messages = dtoList,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = (page * pageSize) < totalCount
            };
        }

        public async Task<int> MarkConversationAsReadAsync(long conversationId, long currentUserId, bool isStaff, long? lastReadMessageId = null)
        {
            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && !c.IsDeleted);

            if (conv == null) return 0;

            var query = _dbContext.ChatMessages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted && m.Status != ChatMessageStatus.Read);

            if (lastReadMessageId.HasValue && lastReadMessageId.Value > 0)
            {
                query = query.Where(m => m.Id <= lastReadMessageId.Value);
            }

            if (conv.Type == ConversationType.UserToUser)
            {
                query = query.Where(m => m.ReceiverId == currentUserId);

                if (conv.User1Id == currentUserId)
                {
                    conv.User1UnreadCount = 0;
                }
                else if (conv.User2Id == currentUserId)
                {
                    conv.User2UnreadCount = 0;
                }
            }
            else if (conv.Type == ConversationType.Support)
            {
                if (isStaff)
                {
                    query = query.Where(m => m.SenderRole == MessageSenderRole.User);
                    conv.SupportUnreadCount = 0;
                }
                else
                {
                    query = query.Where(m => m.SenderRole == MessageSenderRole.Staff || m.SenderRole == MessageSenderRole.System);
                    conv.User1UnreadCount = 0;
                }
            }

            var unreadMessages = await query.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var msg in unreadMessages)
            {
                msg.Status = ChatMessageStatus.Read;
                msg.ReadAt = now;
            }

            await _dbContext.SaveChangesAsync(currentUserId.ToString());
            return unreadMessages.Count;
        }

        public async Task<int> GetTotalUnreadCountAsync(long currentUserId)
        {
            if (currentUserId <= 0) return 0;

            var user1Count = await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.User1Id == currentUserId)
                .SumAsync(c => c.User1UnreadCount);

            var user2Count = await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.Type == ConversationType.UserToUser && c.User2Id == currentUserId)
                .SumAsync(c => c.User2UnreadCount);

            return user1Count + user2Count;
        }

        public async Task<int> GetStaffUnreadSupportCountAsync(long? staffId = null)
        {
            var query = _dbContext.Conversations
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.Type == ConversationType.Support && c.SupportStatus != SupportTicketStatus.Closed);

            if (staffId.HasValue && staffId.Value > 0)
            {
                query = query.Where(c => c.AssignedStaffId == staffId.Value || !c.AssignedStaffId.HasValue);
            }

            return await query.SumAsync(c => c.SupportUnreadCount);
        }

        public async Task<bool> BlockUserAsync(long currentUserId, long targetUserId, bool block)
        {
            long u1 = Math.Min(currentUserId, targetUserId);
            long u2 = Math.Max(currentUserId, targetUserId);

            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => !c.IsDeleted && c.Type == ConversationType.UserToUser && c.User1Id == u1 && c.User2Id == u2);

            if (conv != null)
            {
                conv.IsBlocked = block;
                conv.BlockedByUserId = block ? currentUserId : null;
                await _dbContext.SaveChangesAsync(currentUserId.ToString());
                return true;
            }

            return false;
        }

        public async Task<bool> AssignSupportTicketAsync(long conversationId, long staffId, string staffName)
        {
            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.Type == ConversationType.Support && !c.IsDeleted);

            if (conv == null) return false;

            conv.AssignedStaffId = staffId;
            if (conv.SupportStatus == SupportTicketStatus.Open)
            {
                conv.SupportStatus = SupportTicketStatus.InProgress;
            }

            await _dbContext.SaveChangesAsync(staffName);
            return true;
        }

        public async Task<bool> UpdateSupportTicketStatusAsync(long conversationId, SupportTicketStatus status, string updatedBy)
        {
            var conv = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.Type == ConversationType.Support && !c.IsDeleted);

            if (conv == null) return false;

            conv.SupportStatus = status;
            await _dbContext.SaveChangesAsync(updatedBy);
            return true;
        }

        private static string GetPreviewText(ChatMessage message)
        {
            return message.MessageType switch
            {
                ChatMessageType.Text => string.IsNullOrWhiteSpace(message.Content) ? "[Message]" : (message.Content.Length > 60 ? message.Content[..60] + "..." : message.Content),
                ChatMessageType.Image => "📷 Photo",
                ChatMessageType.VoiceNote => "🎤 Voice Note",
                ChatMessageType.Document => "📄 Document",
                ChatMessageType.ContactCard => "📇 Contact Info",
                _ => "[Attachment]"
            };
        }

        private static ConversationDto MapToConversationDto(Conversation conv, long currentUserId, Registration? participant)
        {
            bool isUser1 = conv.User1Id == currentUserId;
            int unread = conv.Type == ConversationType.Support ? conv.User1UnreadCount : (isUser1 ? conv.User1UnreadCount : conv.User2UnreadCount);

            string pName = conv.Type == ConversationType.Support ? "M4 Nikah Support Team" : (participant?.Name ?? "Member");
            string? regNum = participant?.RegisterNumber;
            string? photo = participant?.ImagePath;
            bool isOnline = participant?.LastSeenAt.HasValue == true && participant.LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-5);

            return new ConversationDto
            {
                Id = conv.Id,
                Type = conv.Type,
                ParticipantUserId = conv.Type == ConversationType.Support ? 0 : (participant?.Id ?? (isUser1 ? (conv.User2Id ?? 0) : conv.User1Id)),
                ParticipantName = pName,
                ParticipantRegisterNumber = regNum,
                ParticipantPhotoUrl = photo,
                IsParticipantOnline = isOnline,
                ParticipantLastSeenAt = participant?.LastSeenAt,
                SupportStatus = conv.SupportStatus,
                SupportSubject = conv.SupportSubject,
                AssignedStaffId = conv.AssignedStaffId,
                LastMessageId = conv.LastMessageId,
                LastMessagePreview = conv.LastMessagePreview,
                LastMessageAt = conv.LastMessageAt,
                LastMessageSenderId = conv.LastMessageSenderId,
                UnreadCount = unread,
                IsBlocked = conv.IsBlocked,
                BlockedByUserId = conv.BlockedByUserId,
                CanReply = !conv.IsBlocked
            };
        }

        private static ConversationDto MapToStaffSupportDto(Conversation conv, Registration? user)
        {
            return new ConversationDto
            {
                Id = conv.Id,
                Type = conv.Type,
                ParticipantUserId = conv.User1Id,
                ParticipantName = user?.Name ?? "User",
                ParticipantRegisterNumber = user?.RegisterNumber,
                ParticipantPhotoUrl = user?.ImagePath,
                IsParticipantOnline = user?.LastSeenAt.HasValue == true && user.LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-5),
                ParticipantLastSeenAt = user?.LastSeenAt,
                SupportStatus = conv.SupportStatus,
                SupportSubject = conv.SupportSubject,
                AssignedStaffId = conv.AssignedStaffId,
                LastMessageId = conv.LastMessageId,
                LastMessagePreview = conv.LastMessagePreview,
                LastMessageAt = conv.LastMessageAt,
                LastMessageSenderId = conv.LastMessageSenderId,
                UnreadCount = conv.SupportUnreadCount,
                IsBlocked = conv.IsBlocked,
                BlockedByUserId = conv.BlockedByUserId,
                CanReply = true
            };
        }

        private static ChatMessageDto MapToChatMessageDto(ChatMessage msg, long currentUserId, Registration? sender)
        {
            return new ChatMessageDto
            {
                Id = msg.Id,
                ConversationId = msg.ConversationId,
                SenderId = msg.SenderId,
                SenderRole = msg.SenderRole,
                SenderName = sender?.Name ?? "User",
                SenderPhotoUrl = sender?.ImagePath,
                ReceiverId = msg.ReceiverId,
                Content = msg.Content,
                MessageType = msg.MessageType,
                AttachmentUrl = msg.AttachmentUrl,
                AttachmentFileName = msg.AttachmentFileName,
                AttachmentFileSize = msg.AttachmentFileSize,
                Status = msg.Status,
                SentAt = msg.SentAt,
                DeliveredAt = msg.DeliveredAt,
                ReadAt = msg.ReadAt,
                IsMine = msg.SenderId == currentUserId && msg.SenderRole == MessageSenderRole.User
            };
        }
    }
}
