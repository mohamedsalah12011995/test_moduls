using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System.Data;
using static Core.Helpers.Enums;


namespace Nupco.EF.Repositories
{
    internal class InterActionRepository : BaseRepository<InterAction>, IInterActionRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly InterAction_Assembler _Assembler;
        private readonly User_Assembler _Assembler_user;

        private string[] stringArray = { "User" ,"Entity"};


        public InterActionRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Assembler = new InterAction_Assembler();
            _Assembler_user = new User_Assembler();
            _logger = logger;
            _context = context;
        }

        public async Task<OperationOutput> CreateOrDeleteInterAction(InterActionObjDto dto, IUnitOfWork unitOfWork, string _serverApiKey)
        {
            try
            {
                if (dto.IsLike is true)
                {
                    var _entity = await FindAsync(i => i.EntityId == dto.EntityId && i.ItemId == dto.ItemId && i.UserId == dto.UserId);
                    if (_entity is null)
                    {
                        dto.CreatedDate = DateTime.Now;
                        var _model = _Assembler.WriteDal(dto);
                        await AddAsync(_model);
                        await SaveChangesAsync();

                        try
                        {
                            if (dto.EntityId.HasValue && int.TryParse(dto.ItemId, out var itemId))
                            {
                                string userId = null;
                                string titleEn = null;
                                var entityType = (Entities)dto.EntityId.Value;

                                switch (entityType)
                                {
                                    case Entities.Spaces:
                                        var post = await _context.Posts.Where(x => x.Id == itemId)
                                            .Select(s => new { s.CreatedBy, s.TitleEn }).FirstOrDefaultAsync();
                                        userId = post?.CreatedBy;
                                        titleEn = post?.TitleEn;
                                        break;

                                    case Entities.News:
                                        var news = await _context.Posts.Where(x => x.Id == itemId)
                                            .Select(s => new { s.CreatedBy, s.TitleEn }).FirstOrDefaultAsync();
                                        userId = news?.CreatedBy;
                                        titleEn = news?.TitleEn;
                                        break;

                                    case Entities.Videos:
                                        var video = await _context.Videos.Where(x => x.Id == itemId)
                                            .Select(s => new { s.CreatedBy, s.TitleEn }).FirstOrDefaultAsync();
                                        userId = video?.CreatedBy;
                                        titleEn = video?.TitleEn;
                                        break;

                                    case Entities.Stories:
                                        var story = await _context.Stories.Where(x => x.Id == itemId)
                                            .Select(s => new { s.CreatedBy, s.TitleEn }).FirstOrDefaultAsync();
                                        userId = story?.CreatedBy;
                                        titleEn = story?.TitleEn;
                                        break;

                                    case Entities.Advertisement:
                                        var ads = await _context.Advertisements.Where(x => x.Id == itemId)
                                            .Select(s => new { s.CreatedBy, s.TitleEn }).FirstOrDefaultAsync();
                                        userId = ads?.CreatedBy;
                                        titleEn = ads?.TitleEn;
                                        break;
                                }

                                if (!string.IsNullOrEmpty(userId))
                                {
                                    var senderName = await _context.Users
                                        .Where(x => x.Id == dto.UserId)
                                        .Select(x => x.FirstName + " " + x.LastName)
                                        .FirstOrDefaultAsync();

                                    string moduleName = entityType.ToString().ToLower();

                                    await Notifications.SendNotificationByUsers(
                                        unitOfWork,
                                        new List<string> { userId },
                                        moduleName,
                                        dto.ItemId,
                                        $"إعجاب جديد من {senderName}",
                                        $"New like from {senderName}",
                                        $"أعجب {senderName} بمنشورك",
                                        $"Liked your {moduleName}: {titleEn}",
                                        _serverApiKey,
                                        UserCreatedBy: dto.UserId,
                                        entityId: dto.EntityId);
                                }
                            }
                        }
                        catch (Exception) { /* Notification fail silent */ }
                    }
                }
                else
                {
                    var _entity = await FindAsync(i => i.EntityId == dto.EntityId && i.ItemId == dto.ItemId && i.UserId == dto.UserId);
                    if (_entity is not null)
                    {
                        Delete(_entity);
                        await SaveChangesAsync();
                    }
                }

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetUsersInterActionByEntityId(int entityId, string itemId)
        {
            try
            {
                var entities = await FindAllAsync(i => i.EntityId == entityId && i.ItemId == itemId, stringArray);
                var users = _Assembler_user.WriteListDto(entities.Select(s => s.User)?.ToList());


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(users, users.Count());
                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }
    }
}
