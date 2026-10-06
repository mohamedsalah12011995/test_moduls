using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Kafo;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models.Kafo;
using SixLabors.ImageSharp;
using System.Data;
using System.Globalization;

namespace Nupco.EF.Repositories
{
    internal class KafoRepository :  IKafoRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly User_Assembler _Assembler_user;
        private string pathToSave = "";
        private string pathToSaveKafo = "";


        private string[] stringArray = { "User" ,"Entity"};

        private readonly UserSetting_Assembler _Assembler_UserSetting = new UserSetting_Assembler();
        private readonly KafoUsers_Assembler _Assembler_KafoUsers = new KafoUsers_Assembler();
        private readonly GeneralMessage_Assembler _GeneralMessages_Assembler = new GeneralMessage_Assembler();


        public KafoRepository(ApplicationDbContext context, ILogger logger) 
        {
            _Assembler_user = new User_Assembler();
            _logger = logger;
            _context = context;
            var folderName = "Images/";
            var folderNameKafo = "Images/Kafo/";
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            pathToSave = System.IO.Path.Combine(sharedPath, folderName);
            pathToSaveKafo = System.IO.Path.Combine(sharedPath, folderNameKafo);

        }

        public async Task<OperationOutput> CheckKafoCard(string userId)
        {
            try
            {
                bool IsExeist = false;

                var kafoUsersList = await _context.KafoUsers.Where(i => i.CreatedDate.Value.Month == DateTime.Now.Month && i.SenderId == userId).ToArrayAsync();
                if (kafoUsersList.Any())
                {
                    var _valueSettings = _context.Settings.SingleOrDefaultAsync(i => i.Key == "kafoCards")?.Result?.Value;
                    IsExeist = kafoUsersList?.Count() >= int.Parse(_valueSettings);
                }

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(IsExeist, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> CreateOrEditKafoIcon(KafoIconDto model)
        {
            try
            {
                KafoIcon _entity = null;
                if (model.Id == 0 || model.Id is null)
                {



                    var _model = new KafoIcon();
                    _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                    Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, true, 0, pathToSave) : model.OriginalPic;

                    _entity = _context.KafoIcons.AddAsync(_model).Result.Entity;
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.OriginalPicBase64))
                    {
                        var _model = new KafoIcon();
                        _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                        Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;
                        var _Kafo = await _context.KafoIcons.FirstOrDefaultAsync(f => f.Id == model.Id);
                        _Kafo.OrignalPic = _model.OrignalPic;
                        var entity = _context.KafoIcons.Update(_Kafo);
                        _entity = entity.Entity;
                    }
                    else
                    {
                        _entity = await _context.KafoIcons.FirstOrDefaultAsync(f => f.Id == model.Id);

                    }


                }

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


        public async Task<OperationOutput> CreateOrEditKafoImage(KafoImageDto model)
        {
            try
            {
                KafoImage _entity = null;
                if (model.Id == 0 || model.Id is null)
                {



                    var _model = new KafoImage();
                    _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                    Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;




                    _entity = _context.KafoImages.AddAsync(_model).Result.Entity;
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.OriginalPicBase64))
                    {
                        var _model = new KafoImage();
                        _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                        Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;
                        var _Kafo = await _context.KafoImages.FirstOrDefaultAsync(f => f.Id == model.Id);
                        _Kafo.OrignalPic = _model.OrignalPic;
                        var entity = _context.KafoImages.Update(_Kafo);
                        _entity = entity.Entity;
                    }
                    else
                    {
                        _entity = await _context.KafoImages.FirstOrDefaultAsync(f => f.Id == model.Id);

                    }


                }

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

        public async Task<OperationOutput> CreateOrEditMessages(KafoMessageDto model)
        {
            try
            {
                KafoMessage _entity = null;
                if (model.Id == 0 || model.Id is null)
                {
                    var _model = new KafoMessage()
                    {
                        MessageAr = model.messageAr,
                        MessageEn = model.messageEn
                    };


                    _entity = _context.KafoMessages.AddAsync(_model).Result.Entity;
                }
                else
                {
                    var _Kafo = await _context.KafoMessages.FirstOrDefaultAsync(f => f.Id == model.Id);
                    _Kafo.MessageAr = model.messageAr;
                    _Kafo.MessageEn = model.messageEn;

                    var entity = _context.KafoMessages.Update(_Kafo);
                    _entity = entity.Entity;
                }

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

        public async Task<OperationOutput> DeleteIcon(int id)
        {

            var item = await _context.KafoIcons.FirstOrDefaultAsync(f => f.Id == id);

            if (item is not null)
            {
                _context.KafoIcons.Remove(item);
                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }

        }

        public async Task<OperationOutput> DeleteImage(int id)
        {

            var item = await _context.KafoImages.FirstOrDefaultAsync(f => f.Id == id);

            if (item is not null)
            {
                _context.KafoImages.Remove(item);
                await _context.SaveChangesAsync();
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }

        }

        public async Task<OperationOutput> DeleteMessage(int id)
        {
            var item = await _context.KafoMessages.FirstOrDefaultAsync(f => f.Id == id);

            if (item is not null)
            {
                _context.KafoMessages.Remove(item);
                await _context.SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            else
            {
                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllByPagenation(KafoFiltersBy _filter)
        {
            try
            {
                var startDate = Dates.ConvertStringToDate(_filter.CreatedDate);

                string[] stringArray = { "SenderUser", "ReceiverUser", "KafoImage", "KafoMessage" };
                var spec = Specification<KafoUsers>.All.And(new KafoUserspecification(_filter));
                var KafoUsers = new List<KafoUsers>();
                if (!String.IsNullOrEmpty(_filter.CreatedDate))
                {
                    var _KafoUsersList = await _context.KafoUsers
                        .Where(spec.ToExpression())
                        .Where(i=> i.CreatedDate.Value.Date == startDate.Value.Date && i.CreatedDate.Value.Day == startDate.Value.Day)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.KafoImage)
                        .Include(i => i.KafoMessage)
                        .OrderByDescending(o=> o.Id)
                        .ToListAsync();
                    KafoUsers = _KafoUsersList.ToList();
                }
               else if (!String.IsNullOrEmpty(_filter.SenderId) || !String.IsNullOrEmpty(_filter.ReceiverId) && String.IsNullOrEmpty(_filter.CreatedDate))
                {
                    var _KafoUsersList = await _context.KafoUsers
                        .Where(spec.ToExpression())
                        .Include(i=> i.SenderUser)
                        .Include(i=> i.ReceiverUser)
                        .Include(i=> i.KafoImage)
                        .Include(i=> i.KafoMessage)
                        .OrderByDescending(o => o.Id)
                        .ToListAsync();
                    KafoUsers = _KafoUsersList.ToList();
                }
                else
                {
                    var _KafoUsersList = await _context.KafoUsers.Where(spec.ToExpression())
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.KafoImage)
                        .Include(i => i.KafoMessage)
                        .OrderByDescending(o => o.Id)
                        .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                        .ToListAsync();


                    KafoUsers = _KafoUsersList.ToList();
                }

                var _KafoUsers = _Assembler_KafoUsers.WriteListDto(KafoUsers);
                var counts = _context.KafoUsers.Count();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_KafoUsers, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllCardsByUserId(KafoUserSenderReceiverId kafoUser)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "KafoImage", "KafoMessage" };
                var KafoUsers = new List<KafoUsers>();
                if (!String.IsNullOrEmpty(kafoUser.receiverId))
                {
                    var _KafoUsersList = await _context.KafoUsers
                        .Where(i => i.ReceiverId == kafoUser.receiverId)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.KafoImage)
                        .Include(i => i.KafoMessage)
                        .ToListAsync();
                    KafoUsers = _KafoUsersList.ToList();
                }
                else
                {
                    var _KafoUsersList = await _context.KafoUsers
                        .Where(i => i.SenderId == kafoUser.senderId)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.KafoImage)
                        .Include(i => i.KafoMessage)
                        .ToListAsync();
                    KafoUsers = _KafoUsersList.ToList();
                }



                var _KafoUsers = _Assembler_KafoUsers.WriteListDto(KafoUsers);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_KafoUsers, _KafoUsers.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetAllCardsByUserIdByPagenation(KafoFiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "KafoImage", "KafoMessage" };
                var spec = Specification<KafoUsers>.All.And(new KafoUserspecification(_filter));

                var _KafoUsersList = await _context.KafoUsers.Where(spec.ToExpression())
                    .Include(i => i.SenderUser)
                    .Include(i => i.ReceiverUser)
                    .Include(i => i.KafoImage)
                    .Include(i => i.KafoMessage)
                    .OrderByDescending(i => i.Id)
                    .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                    .Take((int)_filter.pageSize)
                    .ToListAsync();
                var _KafoUsersDto = _Assembler_KafoUsers.WriteListDto(_KafoUsersList);

                var _countAll = 0;
                var _countOfMonth = 0;
                if (!String.IsNullOrEmpty(_filter.SenderId) && String.IsNullOrEmpty(_filter.ReceiverId))
                {
                    _countAll = _context.KafoUsers.Count(i=> i.SenderId==_filter.SenderId);
                    _countOfMonth = _context.KafoUsers.Count(i => i.SenderId == _filter.SenderId&& i.CreatedDate.Value.Month ==DateTime.Now.Month);
                    //_countOfMonth = _context.KafoUsers.Count(i => i.SenderId == _filter.SenderId && i.CreatedDate.Value.Date == DateTime.Now.Date);
                }
               else if (!String.IsNullOrEmpty(_filter.ReceiverId)&& String.IsNullOrEmpty(_filter.SenderId))
                {
                    _countAll = _context.KafoUsers.Count(i => i.ReceiverId == _filter.ReceiverId);
                    _countOfMonth = _context.KafoUsers.Count(i => i.ReceiverId == _filter.ReceiverId && i.CreatedDate.Value.Month == DateTime.Now.Month);
                }
                else
                {
                    _countAll = _context.KafoUsers.Count();
                    _countOfMonth = _context.KafoUsers.Count(i => i.CreatedDate.Value.Month == DateTime.Now.Month);

                }

                var _date = new
                {
                    listKafo = _KafoUsersDto,
                    countAll = _countAll,
                    countOfMonth = _countOfMonth
                };

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_date, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllCardsUserByMonthAndYear(KafoFiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "KafoImage", "KafoMessage" };
                var KafoUsers = new List<KafoUsers>();

                var _KafoUsersList = await _context.KafoUsers
                        .Where(i => i.CreatedDate.Value.Month == _filter.Month && i.CreatedDate.Value.Year == _filter.Year && i.ReceiverId == _filter.ReceiverId)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.KafoImage)
                        .Include(i => i.KafoMessage)
                        .ToListAsync();
                KafoUsers = _KafoUsersList.ToList();


                var _KafoUsers = _Assembler_KafoUsers.WriteListDto(KafoUsers);
                var counts = _context.KafoUsers.Count();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_KafoUsers, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

      
        public async Task<List<kafoIcon>> GetAllKafoIconsToUserProfile()
        {
            try
            {

                var _KafoList = await _context.KafoIcons.ToListAsync();
                var _KafoListDto = _KafoList.Select(s =>
                new kafoIcon
                (s.Id, s.OrignalPic, Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult())).ToList();
                return _KafoListDto.ToList();
            }
            catch (Exception)
            {
                return new List<kafoIcon>();
            }

        }


        public async Task<OperationOutput> GetAllKafoIcons()
        {
            try
            {
                var _KafoList = await _context.KafoIcons.ToListAsync();
                var _KafoListDto = _KafoList.Select(s => new
                {
                    Id = s.Id,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),


                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_KafoListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> GetAllKafoImages()
        {
            try
            {
                var _KafoList = await _context.KafoImages.ToListAsync();
                var _KafoListDto = _KafoList.Select(s => new
                {
                    Id = s.Id,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),


                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_KafoListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllKafoMessages()
        {
            try
            {
                var _KafoList = await _context.KafoMessages.ToListAsync();
                var _KafoListDto = _KafoList.Select(s => new
                {
                    Id = s.Id,
                    MessageAr = s.MessageAr,
                    MessageEn = s.MessageEn

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_KafoListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllKafoMessagesAndImages()
        {
            try
            {
                var _KafoMessages = await _context.KafoMessages.ToListAsync();
                var __KafoMessagesDto = _KafoMessages.Select(s => new
                {
                    Id = s.Id,
                    MessageAr = s.MessageAr,
                    MessageEn = s.MessageEn
                    
                }).ToList();

                var _KafoImages = await _context.KafoImages.ToListAsync();
                var __KafoImagesDto = _KafoImages.Select(s => new
                {
                    Id = s.Id,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),

                }).ToList();

                var Data = new
                {
                    _KafoImages = __KafoImagesDto,
                    _KafoMessages = __KafoMessagesDto
                };

                ResultOutputData result = new ResultOutputData();


                var _result = result.GenearetResultOutput(Data, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllKafoUsers()
        {
            try
            {

                var users = new
                {
                    SenderUsers = await _context.KafoUsers
                    .Include(i => i.SenderUser)
                    .Select(s => new { s.SenderUser.Id, s.SenderUser.UserName }).Distinct().ToListAsync(),

                    ReceiverUser = await _context.KafoUsers
                    .Include(i => i.ReceiverUser)
                    .Select(s => new { s.ReceiverUser.Id, s.ReceiverUser.UserName }).Distinct().ToListAsync()
                };




                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(users, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetTopUsersOfMonth(KafoFiltersByYearAndMonth _filter)
        {
            try
            {
                ResultOutputData result = new ResultOutputData();
                var data = await GetTopUsersOfMonthResult(_filter);
                if (data.Item2 > 0)
                {
                    var _result = result.GenearetResultOutput(data.Item1, data.Item2);
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

        public async Task<Tuple<List<KafoUsersOfMonthResultDto>, int>> GetTopUsersFromKafoUsersOfMonths(KafoFiltersByYearAndMonth _filter)
        {
            try
            {

                if (_filter.Month is not null && _filter.Year is not null)
                {
                    int month = _filter.Month.Value;
                    int year = _filter.Year.Value;


                    var _KafoUsersOfMonths = await _context.KafoUsersOfMonths
                        .Where(i => i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year)
                        .Include(i => i.User)
                        .AsNoTrackingWithIdentityResolution()
                        .ToListAsync();

                    if (_KafoUsersOfMonths.Count > 0)
                    {

                        var TopUsersOfMonth = _KafoUsersOfMonths.GroupBy(
                            p => p.UserId,
                            p => p,
                            (UserId, cards) => new KafoUsersOfMonthResultDto
                            {
                                Id = cards.FirstOrDefault().Id,
                                UserId = cards.FirstOrDefault().UserId,
                                Count = cards.Count(),
                                UserName = cards.FirstOrDefault().User.UserName,
                                Ordering = cards.FirstOrDefault().Ordering,
                                CreatedDate = cards.FirstOrDefault().CreatedDate
                            }).OrderBy(o => o.Ordering)
                            .Select((user, index) => new KafoUsersOfMonthResultDto
                            {
                                Id = user.Id,
                                UserId = user.UserId,
                                ReceiverId = user.UserId,
                                Count = user.Count,
                                ReceiverUserName = user.UserName,
                                Ordering = index + 1, // Setting the order index (starting from 1)
                                CreatedDate = user.CreatedDate

                            })
                            .ToList();

                        var TopUsersPaged = TopUsersOfMonth
                         .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                         .Take((int)_filter.pageSize)

                         .ToList();
                        return new Tuple<List<KafoUsersOfMonthResultDto>, int>(TopUsersPaged, TopUsersOfMonth.Count());

                    }
                    else
                    {
                        var _KafoUsersList = await _context.KafoUsers
                                                .Where(i => i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year)
                                                .Include(i => i.ReceiverUser)
                                                .AsNoTrackingWithIdentityResolution()
                                                .ToListAsync();
                        var TopUsers = _KafoUsersList.GroupBy(
                        p => p.ReceiverId,
                        p => p,
                        (UserId, cards) => new
                        {
                            Id = cards.FirstOrDefault().Id,
                            ReceiverId = cards.FirstOrDefault().ReceiverId,
                            Count = cards.Count(),
                            UserName = cards.FirstOrDefault().ReceiverUser.UserName,
                            ReceiverUserName = cards.FirstOrDefault().ReceiverUser.UserName,
                            Ordring = 0,
                            CreatedDate = cards.FirstOrDefault().CreatedDate
                        }).OrderByDescending(u => u.Count) // Sorting by Count (descending order)
                            .Select((user, index) => new KafoUsersOfMonthResultDto
                            {
                                Id = user.Id,
                                ReceiverId = user.ReceiverId,
                                Count = user.Count,
                                UserName = user.UserName,
                                ReceiverUserName = user.ReceiverUserName,
                                Ordering = index + 1, // Setting the order index (starting from 1)
                                CreatedDate = user.CreatedDate
                            }).Take(3)
                            .ToList();
                        return new Tuple<List<KafoUsersOfMonthResultDto>, int>(TopUsers, TopUsers.Count());
                    }



                }
                return new Tuple<List<KafoUsersOfMonthResultDto>, int>(new List<KafoUsersOfMonthResultDto>(), 0);

            }

            catch (Exception)
            {
                return new Tuple<List<KafoUsersOfMonthResultDto>, int>(new List<KafoUsersOfMonthResultDto>(), 0);

            }
        }

        public async Task<Tuple<List<KafoUsersOfMonthResultDto>, int>> GetTopUsersFromKafoUsers(KafoFiltersByYearAndMonth _filter)
        {
            try
            {

                if (_filter.Month is not null && _filter.Year is not null)
                {
                    int month = _filter.Month.Value;
                    int year = _filter.Year.Value;

                    var kafoUsersOfMonth_list = await _context.KafoUsersOfMonths.Where(i => i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year).ToListAsync();
                    var _KafoUsersList = await _context.KafoUsers
                        .Where(i => i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year)
                        .Include(i => i.ReceiverUser)
                        .AsNoTrackingWithIdentityResolution()
                        .ToListAsync();

                    var TopUsers = _KafoUsersList.GroupBy(
                    p => p.ReceiverId,
                    p => p,
                    (UserId, cards) => new
                    {
                        Id = cards.FirstOrDefault().Id,
                        ReceiverId = cards.FirstOrDefault().ReceiverId,
                        Count = cards.Count(),
                        UserName = cards.FirstOrDefault().ReceiverUser.UserName,
                        ReceiverUserName = cards.FirstOrDefault().ReceiverUser.UserName,
                        Ordring = 0,
                        CreatedDate = cards.FirstOrDefault().CreatedDate

                    }).OrderByDescending(u => u.Count) // Sorting by Count (descending order)
                        .Select((user, index) => new KafoUsersOfMonthResultDto
                        {
                            Id = user.Id,
                            ReceiverId = user.ReceiverId,
                            Count = user.Count,
                            UserName = user.UserName,
                            ReceiverUserName = user.ReceiverUserName,
                            Ordering = index + 1, // Setting the order index (starting from 1)
                            CreatedDate = user.CreatedDate

                        })
                        .ToList();

                    var TopUsersPaged = new List<KafoUsersOfMonthResultDto>();


                    foreach (var item in kafoUsersOfMonth_list)
                    {
                        TopUsers = TopUsers.Where(i => i.ReceiverId != item.UserId && i.CreatedDate.Value.Month == item.CreatedDate.Value.Month && i.CreatedDate.Value.Year == item.CreatedDate.Value.Year).ToList();
                    }



                    if (kafoUsersOfMonth_list.Count == 1)
                        TopUsersPaged = TopUsers.Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take(2).ToList();
                    if (kafoUsersOfMonth_list.Count == 2)
                        TopUsersPaged = TopUsers.Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take(1).ToList();


                    try
                    {
                        foreach (var item in TopUsersPaged)
                        {
                            var obj = new KafoUsersOfMonth();
                            obj.Ordering = item.Ordering;
                            obj.Count = item.Count;
                            obj.CreatedDate = item.CreatedDate;
                            obj.UserId = item.ReceiverId;
                            await _context.KafoUsersOfMonths.AddAsync(obj);

                        }
                        if (kafoUsersOfMonth_list.Count() < 3)
                            await _context.SaveChangesAsync();

                    }
                    catch (Exception)
                    {

                    }

                    return await GetTopUsersFromKafoUsersOfMonths(_filter);
                }





                return new Tuple<List<KafoUsersOfMonthResultDto>, int>(new List<KafoUsersOfMonthResultDto>(), 0);



            }

            catch (Exception)
            {
                return new Tuple<List<KafoUsersOfMonthResultDto>, int>(new List<KafoUsersOfMonthResultDto>(), 0);

            }
        }


        public async Task<Tuple<List<KafoUsersOfMonthResultDto>,int>> GetTopUsersOfMonthResult(KafoFiltersByYearAndMonth _filter)
        {
            try
            {

                var _result_KafoUsersOfMonths = await GetTopUsersFromKafoUsersOfMonths(_filter);
                var TopUsersPaged_KafoUsersOfMonths = _result_KafoUsersOfMonths.Item1;
                var TopUsers_KafoUsersOfMonths = _result_KafoUsersOfMonths.Item2;
                if (TopUsersPaged_KafoUsersOfMonths.Count > 0 && TopUsersPaged_KafoUsersOfMonths.Count==3)
                {
                    return new Tuple<List<KafoUsersOfMonthResultDto>, int>(TopUsersPaged_KafoUsersOfMonths, TopUsers_KafoUsersOfMonths);

                }

                else
                {

                    var _result_KafoUsers = await GetTopUsersFromKafoUsers(_filter);
                    var TopUsersPaged = _result_KafoUsers.Item1;
                    var TopUsers = _result_KafoUsers.Item2;

                    return new Tuple<List<KafoUsersOfMonthResultDto>, int>(TopUsersPaged, TopUsers);
                }
            }

            catch (Exception)
            {
                return new Tuple<List<KafoUsersOfMonthResultDto>, int>(new List<KafoUsersOfMonthResultDto>(), 0);

            }
        }


        public async Task<OperationOutput> GetTopUsersRecievedCards(KafoFiltersByYearAndMonth _filter)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "KafoImage", "KafoMessage" };
                ResultOutputData result = new ResultOutputData();

                if (_filter.Month is not null && _filter.Year is not null)
                {
                    int month = _filter.Month.Value;
                    int year = _filter.Year.Value;

                    var _KafoUsersList = await _context.KafoUsers
                        .Where(i => i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.KafoImage)
                        .Include(i => i.KafoMessage)
                        .ToListAsync();


                    var TopCardRecieved = _KafoUsersList.GroupBy(
                        p => p.ReceiverId,
                        p => p,
                        (ReceiverId, cards) => new
                        {
                            ReceiverId = cards.FirstOrDefault().ReceiverId,
                            Count = cards.Count(),
                            ReceiverUserName = cards.FirstOrDefault().ReceiverUser.UserName,
                            Cards = _Assembler_KafoUsers.WriteListDto(cards)
                        }).OrderByDescending(o => o.Count).ToList();

                    var TopCardRecievedPaged = TopCardRecieved
                     .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                     .Take((int)_filter.pageSize)
                     .ToList();

                    var _result = result.GenearetResultOutput(TopCardRecievedPaged, TopCardRecieved.Count());
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

        public async Task<OperationOutput> SubmitKafo(KafoUsersObjDto model,IUnitOfWork _unitOfWork , string _serverApiKey)
        {
            try
            {
                bool IsExeist = false;

                var kafoUsersList = await _context.KafoUsers.Where(i => i.CreatedDate.Value.Month == DateTime.Now.Month && i.SenderId == model.SenderId).ToArrayAsync();
                if (kafoUsersList.Any())
                {
                    var _valueSettings = _context.Settings.SingleOrDefaultAsync(i => i.Key == "kafoCards")?.Result?.Value;
                    IsExeist = kafoUsersList?.Count() >= int.Parse(_valueSettings);
                }
                if (IsExeist == false)
                {


                    var _model = new KafoUsers()
                    {
                        CreatedDate = DateTime.Now,
                        KafoImageId = model.KafoImageId,
                        KafoMessageId = model.KafoMessageId,
                        OrignalPic = model.OrignalPic,
                        ReceiverId = model.ReceiverId,
                        SenderId = model.SenderId,

                    };

                    _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                    Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSaveKafo, false, 0, pathToSaveKafo) : model.OrignalPic;



                    var _entity = _context.KafoUsers.AddAsync(_model).Result.Entity;

                    await _context.SaveChangesAsync();
                    var _KafoUser = _Assembler_KafoUsers.WriteDto(_entity);

                    var usersIds = new List<string?>();
                    usersIds.Add(model.ReceiverId);


                    var _settingsKafo = await _context.GeneralMessages.Where(f => f.Key == "kafo").Select(s => new
                    {
                        children = _GeneralMessages_Assembler.WriteListDto(_context.GeneralMessages.Where(f => f.ParentId == s.Id).ToList())
                    }).ToListAsync();

                    var obj = new
                    {

                        notificationTitleAr = _settingsKafo.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationTitle")?.ValueAr,
                        notificationTitleEn = _settingsKafo.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationTitle")?.ValueEn,

                        notificationBodyAr = _settingsKafo.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationBody")?.ValueAr,
                        notificationBodyEn = _settingsKafo.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationBody")?.ValueEn,
                    };

                    var senderName = await _context.Users
                        .Where(x => x.Id == model.SenderId)
                        .Select(x => x.FirstName + " " + x.LastName)
                        .FirstOrDefaultAsync();
                    var kafoMessage = await _context.KafoMessages
                        .Where(x => x.Id == model.KafoMessageId)
                        .Select(x => new { x.MessageAr, x.MessageEn })
                        .FirstOrDefaultAsync();


                    await Notifications.SendNotificationByUsers(_unitOfWork, usersIds, "kafo", _entity.Id.ToString(), $"{obj.notificationTitleAr} من {senderName}", $"{obj.notificationTitleEn} from {senderName}", kafoMessage?.MessageAr ?? obj.notificationBodyAr, kafoMessage?.MessageEn ?? obj.notificationBodyEn, _serverApiKey, UserCreatedBy: model.SenderId, entityId: 27);

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_KafoUser, 1);
                    return _result;
                }
                else
                {
                    var _result = ResultOutputData.GenearetResultOutputLimit();
                    return _result;
                    
                }
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> SubmitUsersOfMonth(List<KafoUsersOfMonthObj> dtos)
        {
            try
            {

                var ListUsersOfMonth = _Assembler_KafoUsers.WriteListDal(dtos).ToList();
                foreach (var item in ListUsersOfMonth)
                {
                    int month = item.CreatedDate.Value.Month;
                    int year = item.CreatedDate.Value.Year;



                    var find =await _context.KafoUsersOfMonths.FirstOrDefaultAsync(i=> i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year && i.UserId == item.UserId);
                    if (find is not null)
                    {
                        find.Ordering = item.Ordering;
                        find.Count = item.Count;
                        find.UserId = item.UserId;
                        _context.KafoUsersOfMonths.Update(find);

                    }
                    else
                    {
                        await _context.KafoUsersOfMonths.AddAsync(item);

                    }

                }
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

        public async Task<OperationOutput> SubmitSingelUserOfMonth(KafoUsersOfMonthObj dto)
        {
            try
            {

                var find = await _context.KafoUsersOfMonths.FirstOrDefaultAsync(i => i.Id== dto.Id);
                if (find is not null)
                {
                    find.Ordering = dto.Ordering;
                    find.Count = dto.Count;
                    find.UserId = dto.UserId;
                    _context.KafoUsersOfMonths.Update(find);
                    await _context.SaveChangesAsync();


                    var Result = ResultOutputData.GenearetResultOutputSuccess();
                    return Result;
                }

                var _Result = ResultOutputData.GenearetResultOutputCatch();
                return _Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetTopKafoUsersOfMonths(KafoFiltersByTopYearAndMonth _filter)
        {
            try
            {
                ResultOutputData result = new ResultOutputData();
                if (_filter.Year is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                string[] monthNames = DateTimeFormatInfo.CurrentInfo.MonthNames;

                int year = _filter.Year.Value;
                int? month = _filter.Month;

                var query = _context.KafoUsers
                    .Where(i => i.CreatedDate.Value.Year == year);

                if (month.HasValue)
                {
                    query = query.Where(i => i.CreatedDate.Value.Month == month.Value);
                }

                var topUsersData = await query
                    .Include(i => i.ReceiverUser)
                    .GroupBy(i => i.ReceiverId)
                    .Select(g => new KafoUsersTopOfMonthResultDto
                    {
                        ReceiverUserName = g.FirstOrDefault().ReceiverUser.UserName,
                        Count = g.Count(),
                        CreatedDate = g.Max(x => x.CreatedDate),
                        //MonthNumber = g.Max(x => x.CreatedDate).Value.Month
                    })
                    .OrderByDescending(u => u.Count)
                    .Take(5)
                    .AsNoTrackingWithIdentityResolution()
                    .ToListAsync();

                for (int i = 0; i < topUsersData.Count; i++)
                {
                    topUsersData[i].Ordering = i + 1;
                }

                //for (int i = 0; i < topUsersData.Count; i++)
                //{
                //    topUsersData[i].Ordering = i + 1;

                //    int mNum = topUsersData[i].MonthNumber;
                //    if (mNum >= 1 && mNum <= 12)
                //    {
                //        topUsersData[i].MonthName = monthNames[mNum - 1];
                //    }
                //}

                return result.GenearetResultOutput(topUsersData, topUsersData.Count);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
