using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;

namespace Nupco.EF.Repositories
{
    internal class NotificationHistoryRepository : BaseRepository<NotificationHistory>, INotificationHistoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly NotificationHistory_Assembler _NotificationHistory_Assembler;
        private readonly User_Assembler _User_Assembler;

        private string[] stringArray = { "Country" };


        public NotificationHistoryRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _NotificationHistory_Assembler = new NotificationHistory_Assembler();
            _User_Assembler = new User_Assembler();
            _logger = logger;
            _context = context;

        }


        public async Task<OperationOutput> GetAllNotificationHistoryByPagenation(NotificationHistoryParams _filter)
        {
            try
            {
                var spec = Specification<NotificationHistory>.All.And(new NotificationHistorySpecification(_filter));
                var NotificationHistory = await _context.NotificationHistories.Include(i => i.User).Skip((int)_filter.PageNumber * (int)_filter.PageSize).Take((int)_filter.PageSize).ToListAsync();
                var _NotificationHistory = _NotificationHistory_Assembler.WriteListDto(NotificationHistory);
                var counts = await CountAsync();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_NotificationHistory, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllNotificationHistory()
        {
            try
            {

                var entities = await _context.NotificationHistories.Include(i => i.User).ToListAsync();
                var _entities = _NotificationHistory_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync();
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetNotificationHistoryById(int id)
        {
            try
            {
                var NotificationHistory = await _context.NotificationHistories
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(f => f.Id == id);
                if (NotificationHistory != null)
                {
                    var _NotificationHistory = _NotificationHistory_Assembler.WriteDto(NotificationHistory);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_NotificationHistory, 1);
                    return _result;
                }
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetNotificationHistoryByEntityId(NotificationHistoryParams historyParams)
        {
            try
            {
                var spec = new NotificationHistorySpecification(historyParams);

                var query = _context.NotificationHistories
                    .Where(spec.ToExpression())
                    .Include(x => x.User);

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                // ✅ Safe pagination handling
                var page = historyParams.PageNumber.GetValueOrDefault(1);
                var size = historyParams.PageSize.GetValueOrDefault(10);

                page = page <= 0 ? 1 : page;
                size = size <= 0 ? 10 : size;

                var data = await query
                    .OrderByDescending(x => x.CreatedDate)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                //foreach (var notification in data)
                //{

                //    notification.ImageUrl = await ResolveNotificationImageUrlAsync(notification);
                //    notification.IsRead = true;


                //}

                //_context.NotificationHistories.UpdateRange(data);
                //await _context.SaveChangesAsync();

                var mappedData = _NotificationHistory_Assembler.WriteListDto(data);

                foreach (var item in mappedData)
                {
                    item.ImageUrl= !string.IsNullOrEmpty(item.ImageUrl) ? item.ImageUrl.Split(",")[0]:string.Empty;
                    var source = data.FirstOrDefault(x => x.Id == item.Id);
                    if (source?.User != null)
                    {

                        item.UserData = _User_Assembler.WriteDto(source.User);
                    }
                }

                // make notification as read when fetched by user
                if (!string.IsNullOrWhiteSpace(historyParams.NotifiedUser))
                {
                    foreach (var notification in data)
                        notification.IsRead = true;

                    _context.NotificationHistories.UpdateRange(data);
                    await _context.SaveChangesAsync();
                }

                var unreadCount = 0;
                if (!string.IsNullOrWhiteSpace(historyParams.NotifiedUser))
                {
                    unreadCount = await _context.NotificationHistories
                        .CountAsync(x => x.NotifiedUser == historyParams.NotifiedUser && !x.IsRead);
                }

                var result = new ResultOutputData()
                    .GenearetResultOutput(mappedData, totalCount);

                result.Output["UnreadCount"] = unreadCount;
                return result;
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> CreateNotificationHistory(NotificationHistoryObjDto entity)
        {
            try
            {
                var _model = _NotificationHistory_Assembler.WriteDal(entity);
                var _entity = await AddAsync(_model);
                await _context.SaveChangesAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entity, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> ReadNotificationHistory(int id)
        {
            var record = await _context.NotificationHistories.FirstOrDefaultAsync(f => f.Id == id);
            if (record == null)
                ResultOutputData.GenearetResultOutputNoDataReturned();

            record.IsRead = true;

            _context.NotificationHistories.Update(record);
            await _context.SaveChangesAsync();

            ResultOutputData resultOutput = new ResultOutputData();
            var _result = resultOutput.GenearetResultOutput(record, 1);
            return _result;

        }

        private async Task<string?> ResolveNotificationImageUrlAsync(NotificationHistory notification)
        {
            if (!string.IsNullOrWhiteSpace(notification.ImageUrl))
            {
                return notification.ImageUrl;
            }

            if (!int.TryParse(notification.RecordId, out var recordId))
            {
                return notification.ImageUrl;
            }

            var page = notification.Page?.Trim().ToLowerInvariant();

            // Prefer EntityId mapping (based on your entity list) when available.
            // Fallback to Page parsing for backward compatibility with older notification rows.
            if (notification.EntityId.HasValue)
            {
                // 12 = Chats -> recordId is the Chat message id
                if (notification.EntityId.Value == 12)
                    return await ResolveChatImageAsync(recordId);

                // 11 = Videos -> recordId is the Video id
                if (notification.EntityId.Value == 11)
                    return await ResolveVideoImageAsync(recordId);

                // 2 = Spaces -> recordId is the Post id
                if (notification.EntityId.Value == 2)
                    return await ResolvePostImageAsync(recordId);

                // 27 = Kafo Management -> recordId is KafoUsers id
                if (notification.EntityId.Value == 27)
                    return await ResolveKafoUserImageAsync(recordId);

                // 46 = Cards -> recordId is CardsUsers id
                if (notification.EntityId.Value == 46)
                    return await ResolveCardsUserImageAsync(recordId);
            }

            if (page == "chat")
                return await ResolveChatImageAsync(recordId);

            if (page == "video" || page == "videos")
                return await ResolveVideoImageAsync(recordId);

            if (page == "spaces" || page == "post" || page == "posts")
                return await ResolvePostImageAsync(recordId);

            if (page == "kafo")
                return await ResolveKafoUserImageAsync(recordId);

            // Existing cards notifications use page values tied to card type.
            if (page == "eid" || page == "appreciation" || page == "cards")
                return await ResolveCardsUserImageAsync(recordId);

            var fallbackPostImage = await ResolvePostImageAsync(recordId);
            if (!string.IsNullOrWhiteSpace(fallbackPostImage))
            {
                return fallbackPostImage;
            }

            var fallbackVideoImage = await ResolveVideoImageAsync(recordId);
            if (!string.IsNullOrWhiteSpace(fallbackVideoImage))
            {
                return fallbackVideoImage;
            }

            var fallbackCardsImage = await ResolveCardsUserImageAsync(recordId);
            if (!string.IsNullOrWhiteSpace(fallbackCardsImage))
            {
                return fallbackCardsImage;
            }

            var fallbackKafoImage = await ResolveKafoUserImageAsync(recordId);
            if (!string.IsNullOrWhiteSpace(fallbackKafoImage))
            {
                return fallbackKafoImage;
            }

            return await ResolveChatImageAsync(recordId);
        }

        private async Task<string?> ResolvePostImageAsync(int postId)
        {
            var post = await _context.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == postId);

            return post?.ThumpPic ?? post?.OriginalPic;
        }

        private async Task<string?> ResolveVideoImageAsync(int videoId)
        {
            var video = await _context.Videos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == videoId);

            return video?.ThumpPic ?? video?.OriginalPic;
        }

        private async Task<string?> ResolveChatImageAsync(int chatIdOrChannelId)
        {
            // New behavior: recordId is Chat.Id (chat message id)
            var chat = await _context.Chats
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == chatIdOrChannelId);

            if (chat != null)
            {
                if (!string.IsNullOrWhiteSpace(chat.OrignalPic))
                    return chat.OrignalPic;

                if (chat.ChannelId.HasValue)
                {
                    var channel = await _context.Channels
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == chat.ChannelId.Value);
                    return channel?.ThumpPic ?? channel?.OriginalPic;
                }

                return null;
            }

            // Backward compatibility: recordId might be ChannelId (older rows)
            var channelId = chatIdOrChannelId;
            var legacyChatImage = await _context.Chats
                .AsNoTracking()
                .Where(x => x.ChannelId == channelId && !string.IsNullOrWhiteSpace(x.OrignalPic))
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => x.OrignalPic)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(legacyChatImage))
                return legacyChatImage;

            var legacyChannel = await _context.Channels
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == channelId);

            return legacyChannel?.ThumpPic ?? legacyChannel?.OriginalPic;
        }

        private async Task<string?> ResolveCardsUserImageAsync(int cardsUserId)
        {
            var cardsUser = await _context.CardsUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cardsUserId);

            return cardsUser?.OrignalPic;
        }

        private async Task<string?> ResolveKafoUserImageAsync(int kafoUserId)
        {
            var kafoUser = await _context.KafoUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == kafoUserId);

            return kafoUser?.OrignalPic;
        }

    }
}
