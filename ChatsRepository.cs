using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class ChatsRepository : BaseRepository<Chat>, IChatsRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Chat_Assembler _Chat_Assembler;

        public ChatsRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Chat_Assembler = new Chat_Assembler();
            _logger = logger;
            _context = context;

        }

        public async Task<OperationOutput> CreateChat(ChatDto chatDto)
        {
            try
            {
                var _model = _Chat_Assembler.WriteDal(chatDto);
                var _entity = await _context.Chats.AddAsync(_model);
                var _channel = await _context.Channels.FirstOrDefaultAsync(f => f.Id == _model.ChannelId);
                if (_channel is not null)
                {
                    _channel.UpdatedDate = DateTime.Now;
                    _context.Channels.Update(_channel);

                }
                await _context.SaveChangesAsync();

                var usersWithChannels = await _context.ChanalsUsers.Include(i => i.User).Include(i => i.Channel)
                    .Where(f => f.ChanalId == _model.ChannelId && !string.IsNullOrEmpty(f.User.NotificationToken))
                    .Select(s => new
                    {
                        ChannelNameAr = s.Channel.TitleAr,
                        ChannelNameEn = s.Channel.TitleEn,
                        userId = s.User.Id
                    }).ToListAsync();

                var usersIds = usersWithChannels.Select(s => s.userId).ToList();


                var createdChat = _Chat_Assembler.WriteDto(_entity.Entity);
                var resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(createdChat, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<dynamic> Export(int channelId)
        {
            try
            {
                string[] stringArray = { "User", "Channel", "Entity" };

                var chats = new List<Chat>();
                if (channelId > 0)
                {
                    var entities = await FindAllAsync(i => i.ChannelId == channelId, stringArray);
                    chats = entities.ToList();

                }
                else
                {
                    chats = await _context.Chats.Include(i => i.Channel).Include(i => i.User).ToListAsync();

                }

                var _chats = _Chat_Assembler.WriteListEportDto(chats);

                var Data = _chats.Select(s => new ChatExport
                {
                    Id = s.Id,
                    ChannelNameAr = s.ChannelNameAr,
                    ChannelNameEn = s.ChannelNameEn,
                    CreatedByName = s.CreatedByName,
                    Message = s.Message,
                    UserName = s.UserName,
                    CreatedDate = s.CreatedDate,
                }).ToList();



                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(Data, Data.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllByPagenation(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "User", "Channel", "Entity" };
                var spec = Specification<Chat>.All.And(new ChatSpecification(_filter));
                var Chats = new List<Chat>();
                if (_filter.ChannelId > 0)
                {
                    var _Chat = await FindAllAsync(spec.ToExpression(), stringArray);
                    Chats = _Chat.ToList();
                }
                else
                {
                    var _Chat = await FindAllPagenationAsync(spec.ToExpression(), stringArray, (int)_filter.pageSize * (int)_filter.pageNumber, (int)_filter.pageSize);
                    Chats = _Chat.ToList();
                }


                var _Chats = _Chat_Assembler.WriteListDto(Chats);
                var counts = Count();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Chats, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetChatByChatId(int ChatId)
        {
            try
            {
                string[] stringArray = { "User", "Channel", "Entity" };

                var _entities = await FindAllAsync(i => i.Id == ChatId, stringArray);
                var _Chat = _Chat_Assembler.WriteListDto(_entities);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Chat, 1);
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
