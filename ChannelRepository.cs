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
using static Core.Helpers.Enums;

namespace Nupco.EF.Repositories
{
    internal class ChannelRepository : BaseRepository<Channel>, IChannelRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Channel_Assembler _Assembler;
        private readonly User_Assembler _Assembler_User = new User_Assembler();

        public ChannelRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Assembler = new Channel_Assembler();
            _logger = logger;
            _context = context;

        }

        public async Task<OperationOutput> ActivateChannel(int id, bool activate)
        {
            try
            {
                var find = await _context.Channels.FirstOrDefaultAsync(f => f.Id == id);
                find.IsActive = activate;
                _context.Channels.Update(find);
                await _context.SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> CreateChannel(ChannelObjDto model,string pathToSave)
        {
            try
            {
                model.IsActive = true;
                model.IsDeleted = false;
                model.CreatedDate = DateTime.Now;

                model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
               Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;
               var _model = _Assembler.WriteDal(model);


                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

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

        public async Task<OperationOutput> DeleteChannel(int id)
        {
            try
            {
                var find = await _context.Channels.FirstOrDefaultAsync(f => f.Id == id);
                find.IsDeleted = true;
                _context.Channels.Update(find);
                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> EditChannel(ChannelObjDto model, string pathToSave)
        {
            try
            {
                if (model.isPicChanged == true)
                {
                    model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;
                }

                var _model = _Assembler.WriteDal(model);
                _model.UpdatedDate = DateTime.Now;
                _model.IsActive = true;
                _model.IsDeleted = false;
                _model.CreatedDate = DateTime.Now;
                var _entity = Update(_model);
                await SaveChangesAsync();


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

        public async Task<OperationOutput> GetAllByPagenation(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "Interests" };
                var spec = Specification<Channel>.All.And(new ChannelSpecification(_filter));
                var Channels = new List<Channel>();
                if (_filter.InterestsId > 0)
                {
                    var _channel = await FindAllAsync(spec.ToExpression(), stringArray);
                    Channels = _channel.Where(i => i.IsDeleted == false).ToList();
                }
                else
                {
                    var _channels = await FindAllPagenationAsync(spec.ToExpression(), stringArray, (int)_filter.pageSize * (int)_filter.pageNumber ,(int)_filter.pageSize);
                    Channels = _channels.Where(i => i.IsDeleted == false).ToList();
                }


                var entities = _Assembler.WriteListDto(Channels);
                var counts = _context.Channels.Count(i => i.IsDeleted == false);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllChannels()
        {

            try
            {
                var _entities = await _context.Channels.ToListAsync();
                var count = await _context.Channels.CountAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllChannelsByInterestId(int interestId)
        {
            try
            {
                var _ChanalsUsers = await _context.ChanalsUsers.ToListAsync();

                var _entities = await _context.Channels.Where(f => f.InterestsId == interestId && f.IsDeleted == false).ToListAsync();
                var entities = _entities.Select(s => new ChannelResult
                {
                    Channel = _Assembler.WriteDto(s),
                    Count = _ChanalsUsers.Count(c => c.ChanalId == s.Id)
                }).Distinct().ToList();

                var count = await _context.Channels.CountAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllChannelsByInterstsUserId(string userId)
        {
            try
            {
                var _ChanalsUsers = await _context.ChanalsUsers.ToListAsync();
                List<ChannelResult> entities = new List<ChannelResult>();
                var _chanlasByIntersts = await _context.Channels
                    .Where(b => b.IsActive == true)
                    .Include(i => i.Interests)
                    .Where(f => _context.ChanalsUsers.Any(c => c.ChanalId == f.Id && c.UserId == userId))
                    .OrderByDescending(o=> o.UpdatedDate)
                    .ToListAsync();

                var chanlasByIntersts = _chanlasByIntersts.Select(s => new ChannelResult
                {
                    Channel = _Assembler.WriteDto(s),
                    Count = _ChanalsUsers.Count(c => c.ChanalId == s.Id)
                }).Distinct().ToList();

                entities.AddRange(chanlasByIntersts);
                foreach (var item in entities)
                {
                    item.Channel.IsJoin = true;
                }

                var count = await _context.Channels.CountAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllChannelsByUserId(string userId)
        {
            try
            {
                var entities = await _context.Channels.Where(f => f.CreatedBy == userId && f.IsDeleted == false).ToListAsync();
                var count = await _context.Channels.CountAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllChannelsByUserJoind(string userId)
        {
            try
            {
                var _ChanalsUsers = await _context.ChanalsUsers.ToListAsync();

                var _entities = await _context.Channels
                    .Where(f => f.IsDeleted == false && f.IsActive == true)
                    .Include(i => i.Interests)
                    .Select(s => new ChannelDto
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    OriginalPic = s.OriginalPic,
                    InterstsTitleAr = s.Interests.TitleAr,
                    InterstsTitleEn = s.Interests.TitleEn,
                    InterestsId = s.InterestsId,
                    CreatedDate = s.CreatedDate,
                    IsJoin = _context.ChanalsUsers.Any(c => c.ChanalId == s.Id && c.UserId == userId)

                }).ToListAsync();

                var entities = _entities.OrderByDescending(s => s.IsJoin).DistinctBy(s => s.Id).Select(s => new ChannelResult
                {
                    Channel = s,
                    Count = _ChanalsUsers.Count(c => c.ChanalId == s.Id)

                });

                var count = await _context.Channels.CountAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllChannelsRecomendedUserId(string userId)
        {
            try
            {
                List<ChannelDto> channelsDto = new List<ChannelDto>();
                List<ChannelResult> channels = new List<ChannelResult>();
                var _interestsByUser = await _context.InterestsUsers
                    .Where(f => f.UserId == userId).Include(i => i.Interests).ToListAsync();
                var _ChanalsUsers = await _context.ChanalsUsers.ToListAsync();

                foreach (var item in _interestsByUser.Select(s => s.InterestsId).ToList())
                {

                    var chanlasByInterst = await _context.Channels.Where(a => a.IsActive == true)
                        .Where(f => f.InterestsId == item && f.IsDeleted == false)
                        .ToListAsync();

                    chanlasByInterst = chanlasByInterst.DistinctBy(i => i.Id).ToList();
                    foreach (var channel in chanlasByInterst)
                    {
                        var _Mychannel = _ChanalsUsers.FirstOrDefault(i => i.ChanalId == channel.Id && i.UserId == userId);

                        if (_Mychannel != null)
                        {
                            continue;
                        }
                        else
                        {
                            var _dto = _Assembler.WriteDto(channel);
                            _dto.IsJoin = false;
                            channelsDto.Add(_dto);
                        }
                    }
                }


                var entities = channelsDto.Select(s => new ChannelResult
                {
                    Channel = s,
                    Count = _ChanalsUsers.Count(c => c.ChanalId == s.Id)
                }).OrderByDescending(o=> o.Channel.UpdatedDate).DistinctBy(s => s.Channel.Id).ToList();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, entities.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetChannelById(int id)
        {
            try
            {
                var Channel = await _context.Channels.FirstOrDefaultAsync(f=> f.Id==id);
                var _entity = _Assembler.WriteDto(Channel);
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

        public async Task<OperationOutput> GetChannelJoindById(int channelId)
        {
            try
            {
                var entities = await _context.ChanalsUsers
                    .Where(i => i.ChanalId == channelId)
                    .Include(i => i.User)
                    .Include(i => i.Channel)
                    .ThenInclude(i => i.Entity)
                    .Select(s => new
                    {
                        Email = s.User.Email,
                        userName = s.User.UserName,
                        FullName = s.User.FirstName + " " + s.User.LastName,
                        entityAr = s.Channel.Entity.NameAr,
                        entityEn = s.Channel.Entity.NameEn,
                        ChannelAr = s.Channel.TitleAr,
                        ChannelEn = s.Channel.TitleEn,
                        BriefeContentAr = s.Channel.BriefeContentAr,
                        BriefeContentEn = s.Channel.BriefeContentEn

                    }).ToListAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, entities.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetListUserByChannelId(int? channelId)
        {
            try
            {
                var entities = await _context.ChanalsUsers.Where(i => i.ChanalId == channelId).Include(i => i.User).Select(s => s.User).ToListAsync();
                var _usersListDto = _Assembler_User.WriteListDto(entities);
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(entities, entities.Count());
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> JoinChannelByUser(ChannelUserObj model)
        {
            try
            {
                // var _channel = _Assembler.WriteDal(model);
                var _channel = new ChanalsUser()
                {
                    ChanalId = model.ChanalId,
                    UserId = model.UserId,
                    IsDeleted = false
                };

                await _context.ChanalsUsers.AddAsync(_channel);
                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> LeaveChannelByUser(ChannelUserObj model)
        {
            try
            {
                var _channel = await _context.ChanalsUsers.FirstOrDefaultAsync(i => i.ChanalId == model.ChanalId && i.UserId == model.UserId);
                if (_channel is not null)
                {
                    _context.ChanalsUsers.Remove(_channel);
                    await _context.SaveChangesAsync();
                    var _Result = ResultOutputData.GenearetResultOutputSuccess();
                    return _Result;
                }

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> SuggestChannel(ChannelObjDto model, string pathToSave)
        {
            try
            {
                model.IsActive = false;
                model.IsDeleted = false;
                model.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(model);

                _model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
               Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : _model.OriginalPic;

                var _entity = await _context.Channels.AddAsync(_model);
                await _context.SaveChangesAsync();

                //await Notifications.SendNotification(_unitOfWork, "channels", "", "مجتمع جديد", "New community", model.BriefeContentAr, model.BriefeContentEn, _serverApiKey);


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

        public async Task<OperationOutput> GetChannelsByAnswerUserId(string userId)
        {
            try
            {
                var channels = await _context.ChanalsUsers.Include(i => i.Channel).Where(i => i.UserId == userId).Select(s => s.Channel).ToListAsync();
                if (channels.Count > 0)
                {


                    var obj = new
                    {
                        channels = channels,
                        channelsCount = channels.Count()
                    };
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(obj, 1);
                    return _result;
                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

    }
}
