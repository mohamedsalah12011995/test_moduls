using Core.Helpers;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Cards;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Google.Api.Gax.ResourceNames;
using System.Diagnostics;
using static Nupco.Core.Helpers.Images;
using DocumentFormat.OpenXml.Vml.Office;

namespace Nupco.EF.Repositories
{
    internal class CardsRepository :  ICardsRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly User_Assembler _Assembler_user;
        private string pathToSave = "";
        private string pathToSaveCards = "";


        private string[] stringArray = { "User" ,"Entity"};

        private readonly UserSetting_Assembler _Assembler_UserSetting = new UserSetting_Assembler();
        private readonly CardsUsers_Assembler _Assembler_CardsUsers = new CardsUsers_Assembler();
        private readonly GeneralMessage_Assembler _GeneralMessages_Assembler = new GeneralMessage_Assembler();


        public CardsRepository(ApplicationDbContext context, ILogger logger) 
        {
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            _Assembler_user = new User_Assembler();
            _logger = logger;
            _context = context;
            var folderName = "Images/";
            var folderNameCards = "Images/Kafo/";

            pathToSave = System.IO.Path.Combine(sharedPath, folderName);
            pathToSaveCards = System.IO.Path.Combine(sharedPath, folderNameCards);

        }

        public async Task<OperationOutput> CheckCardsCard(string userId)
        {
            try
            {
                bool IsExeist = false;

                var CardsUsersList = await _context.CardsUsers.Where(i => i.CreatedDate.Value.Month == DateTime.Now.Month && i.SenderId == userId).ToArrayAsync();
                if (CardsUsersList.Any())
                {
                    var _valueSettings = _context.Settings.SingleOrDefaultAsync(i => i.Key == "CardsCards")?.Result?.Value;
                    IsExeist = CardsUsersList?.Count() >= int.Parse(_valueSettings);
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


        public async Task<OperationOutput> CreateOrEditCardsImage(CardsImageDto model)
        {
            try
            {
                CardsImage _entity = null;
                if (model.Id == 0 || model.Id is null)
                {



                    var _model = new CardsImage();
                    _model.EntityId = model.entityId;
                    _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                    Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;




                    _entity = _context.CardsImages.AddAsync(_model).Result.Entity;
                }
                else
                {
                    if (!string.IsNullOrEmpty(model.OriginalPicBase64))
                    {
                        var _model = new CardsImage();
                        _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                        Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;
                        var _Cards = await _context.CardsImages.FirstOrDefaultAsync(f => f.Id == model.Id);
                        _Cards.OrignalPic = _model.OrignalPic;
                        _Cards.EntityId = model.entityId;
                        var entity = _context.CardsImages.Update(_Cards);
                        _entity = entity.Entity;
                    }
                    else
                    {
                        _entity = await _context.CardsImages.FirstOrDefaultAsync(f => f.Id == model.Id);
                        _entity.EntityId = model.entityId;
                        var entity = _context.CardsImages.Update(_entity);
                        _entity = entity.Entity;

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

        public async Task<OperationOutput> CreateOrEditMessages(CardsMessageDto model)
        {
            try
            {
                CardsMessage _entity = null;
                if (model.Id == 0 || model.Id is null)
                {
                    var _model = new CardsMessage()
                    {
                        MessageAr = model.messageAr,
                        MessageEn = model.messageEn,
                        EntityId = model.entityId

                    };


                    _entity = _context.CardsMessages.AddAsync(_model).Result.Entity;
                }
                else
                {
                    var _Cards = await _context.CardsMessages.FirstOrDefaultAsync(f => f.Id == model.Id);
                    _Cards.MessageAr = model.messageAr;
                    _Cards.MessageEn = model.messageEn;
                    _Cards.EntityId = model.entityId;


                    var entity = _context.CardsMessages.Update(_Cards);
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


        public async Task<OperationOutput> DeleteImage(int id)
        {

            var item = await _context.CardsImages.FirstOrDefaultAsync(f => f.Id == id);

            if (item is not null)
            {
                _context.CardsImages.Remove(item);
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
            var item = await _context.CardsMessages.FirstOrDefaultAsync(f => f.Id == id);

            if (item is not null)
            {
                _context.CardsMessages.Remove(item);
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

        public async Task<OperationOutput> GetAllByPagenation(CardsFiltersBy _filter)
        {
            try
            {
                var startDate = Dates.ConvertStringToDate(_filter.CreatedDate);

                string[] stringArray = { "SenderUser", "ReceiverUser", "CardsImage", "CardsMessage" };
                var spec = Specification<CardsUsers>.All.And(new CardsUserSpecification(_filter));
                var CardsUsers = new List<CardsUsers>();
                if (!String.IsNullOrEmpty(_filter.CreatedDate))
                {
                    var _CardsUsersList = await _context.CardsUsers
                        .Where(spec.ToExpression())
                        .Where(i=> i.CreatedDate.Value.Date == startDate.Value.Date && i.CreatedDate.Value.Day == startDate.Value.Day)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.CardsImage)
                        .Include(i => i.CardsMessage)
                        .OrderByDescending(o=> o.Id)
                        .ToListAsync();
                    CardsUsers = _CardsUsersList.ToList();
                }
               else if (!String.IsNullOrEmpty(_filter.SenderId) || !String.IsNullOrEmpty(_filter.ReceiverId) && String.IsNullOrEmpty(_filter.CreatedDate))
                {
                    var _CardsUsersList = await _context.CardsUsers
                        .Where(spec.ToExpression())
                        .Include(i=> i.SenderUser)
                        .Include(i=> i.ReceiverUser)
                        .Include(i=> i.CardsImage)
                        .Include(i=> i.CardsMessage)
                        .OrderByDescending(o => o.Id)
                        .ToListAsync();
                    CardsUsers = _CardsUsersList.ToList();
                }
                else
                {
                    var _CardsUsersList = await _context.CardsUsers.Where(spec.ToExpression())
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.CardsImage)
                        .Include(i => i.CardsMessage)
                        .OrderByDescending(o => o.Id)
                        .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                        .ToListAsync();


                    CardsUsers = _CardsUsersList.ToList();
                }

                var _CardsUsers = _Assembler_CardsUsers.WriteListDto(CardsUsers);
                var counts = _context.CardsUsers.Count();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsUsers, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllCardsByUserId(CardsUserSenderReceiverId CardsUser)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "CardsImage", "CardsMessage" };
                var CardsUsers = new List<CardsUsers>();
                if (!String.IsNullOrEmpty(CardsUser.receiverId))
                {
                    var _CardsUsersList = await _context.CardsUsers
                        .Where(i => i.ReceiverId == CardsUser.receiverId)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.CardsImage)
                        .Include(i => i.CardsMessage)
                        .ToListAsync();
                    CardsUsers = _CardsUsersList.ToList();
                }
                else
                {
                    var _CardsUsersList = await _context.CardsUsers
                        .Where(i => i.SenderId == CardsUser.senderId)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.CardsImage)
                        .Include(i => i.CardsMessage)
                        .ToListAsync();
                    CardsUsers = _CardsUsersList.ToList();
                }



                var _CardsUsers = _Assembler_CardsUsers.WriteListDto(CardsUsers);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsUsers, _CardsUsers.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetAllCardsByUserIdByPagenation(CardsFiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "CardsImage", "CardsMessage" };
                var spec = Specification<CardsUsers>.All.And(new CardsUserSpecification(_filter));

                var _CardsUsersList = await _context.CardsUsers.Where(spec.ToExpression())
                    .Include(i => i.SenderUser)
                    .Include(i => i.ReceiverUser)
                    .Include(i => i.CardsImage)
                    .Include(i => i.CardsMessage)
                    .OrderByDescending(i => i.Id)
                    .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                    .Take((int)_filter.pageSize)
                    .ToListAsync();
                var _CardsUsersDto = _Assembler_CardsUsers.WriteListDto(_CardsUsersList);

                var _countAll = 0;
                var _countOfMonth = 0;
                if (!String.IsNullOrEmpty(_filter.SenderId) && String.IsNullOrEmpty(_filter.ReceiverId))
                {
                    _countAll = _context.CardsUsers.Count(i=> i.SenderId==_filter.SenderId);
                    _countOfMonth = _context.CardsUsers.Count(i => i.SenderId == _filter.SenderId&& i.CreatedDate.Value.Month ==DateTime.Now.Month);
                    //_countOfMonth = _context.CardsUsers.Count(i => i.SenderId == _filter.SenderId && i.CreatedDate.Value.Date == DateTime.Now.Date);
                }
               else if (!String.IsNullOrEmpty(_filter.ReceiverId)&& String.IsNullOrEmpty(_filter.SenderId))
                {
                    _countAll = _context.CardsUsers.Count(i => i.ReceiverId == _filter.ReceiverId);
                    _countOfMonth = _context.CardsUsers.Count(i => i.ReceiverId == _filter.ReceiverId && i.CreatedDate.Value.Month == DateTime.Now.Month);
                }
                else
                {
                    _countAll = _context.CardsUsers.Count();
                    _countOfMonth = _context.CardsUsers.Count(i => i.CreatedDate.Value.Month == DateTime.Now.Month);

                }

                var _date = new
                {
                    listCards = _CardsUsersDto,
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

        public async Task<OperationOutput> GetAllCardsUserByMonthAndYear(CardsFiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "CardsImage", "CardsMessage" };
                var CardsUsers = new List<CardsUsers>();

                var _CardsUsersList = await _context.CardsUsers
                        .Where(i => i.CreatedDate.Value.Month == _filter.Month && i.CreatedDate.Value.Year == _filter.Year && i.ReceiverId == _filter.ReceiverId)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.CardsImage)
                        .Include(i => i.CardsMessage)
                        .ToListAsync();
                CardsUsers = _CardsUsersList.ToList();


                var _CardsUsers = _Assembler_CardsUsers.WriteListDto(CardsUsers);
                var counts = _context.CardsUsers.Count();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsUsers, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllCardsImagesWithEntityId(int EntityId)
        {
            try
            {
                var _CardsList = await _context.CardsImages.Where(i=> i.EntityId ==EntityId).ToListAsync();
                var _CardsListDto = _CardsList.Select(s => new
                {
                    Id = s.Id,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),
                    EntityId=s.EntityId

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }



        public async Task<OperationOutput> GetAllCardsImages()
        {
            try
            {
                var _CardsList = await _context.CardsImages.ToListAsync();
                var _CardsListDto = _CardsList.Select(s => new
                {
                    Id = s.Id,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),


                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetAllCardsMessagesWithEntityId( int EntityId)
        {
            try
            {
                var _CardsList = await _context.CardsMessages.Where(i=> i.EntityId ==EntityId).ToListAsync();
                var _CardsListDto = _CardsList.Select(s => new
                {
                    Id = s.Id,
                    MessageAr = s.MessageAr,
                    MessageEn = s.MessageEn,
                    EntityId = s.EntityId

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }


        public async Task<OperationOutput> GetAllCardsMessages()
        {
            try
            {
                var _CardsList = await _context.CardsMessages.ToListAsync();
                var _CardsListDto = _CardsList.Select(s => new
                {
                    Id = s.Id,
                    MessageAr = s.MessageAr,
                    MessageEn = s.MessageEn

                }).ToList();
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CardsListDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllCardsMessagesAndImages()
        {
            try
            {
                var _CardsMessages = await _context.CardsMessages.ToListAsync();
                var __CardsMessagesDto = _CardsMessages.Select(s => new
                {
                    Id = s.Id,
                    MessageAr = s.MessageAr,
                    MessageEn = s.MessageEn
                    
                }).ToList();

                var _CardsImages = await _context.CardsImages.ToListAsync();
                var __CardsImagesDto = _CardsImages.Select(s => new
                {
                    Id = s.Id,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),

                }).ToList();

                var Data = new
                {
                    _CardsImages = __CardsImagesDto,
                    _CardsMessages = __CardsMessagesDto
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

        public async Task<OperationOutput> GetAllCardsMessagesAndImagesWithEntity(int entityId)
        {
            try
            {
                var _CardsMessages = await _context.CardsMessages.Where(i=> i.EntityId==entityId).ToListAsync();
                var __CardsMessagesDto = _CardsMessages.Select(s => new
                {
                    Id = s.Id,
                    EntityId = s.EntityId,
                    MessageAr = s.MessageAr,
                    MessageEn = s.MessageEn

                }).ToList();

                var _CardsImages = await _context.CardsImages.Where(i => i.EntityId == entityId).ToListAsync();
                var __CardsImagesDto = _CardsImages.Select(s => new
                {
                    Id = s.Id,
                    EntityId=s.EntityId,
                    OrignalPic = s.OrignalPic,
                    OriginalPicWidthAndHight = Images.GetWidthAndHight(s.OrignalPic).GetAwaiter().GetResult(),

                }).ToList();

                var Data = new
                {
                    _CardsImages = __CardsImagesDto,
                    _CardsMessages = __CardsMessagesDto
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

        public async Task<OperationOutput> GetAllCardsUsers()
        {
            try
            {

                var users = new
                {
                    SenderUsers = await _context.CardsUsers
                    .Include(i => i.SenderUser)
                    .Select(s => new { s.SenderUser.Id, s.SenderUser.UserName }).Distinct().ToListAsync(),

                    ReceiverUser = await _context.CardsUsers
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


        public async Task<OperationOutput> GetTopUsersRecievedCards(CardsFiltersByYearAndMonth _filter)
        {
            try
            {
                string[] stringArray = { "SenderUser", "ReceiverUser", "CardsImage", "CardsMessage" };
                ResultOutputData result = new ResultOutputData();

                if (_filter.Month is not null && _filter.Year is not null)
                {
                    int month = _filter.Month.Value;
                    int year = _filter.Year.Value;

                    var _CardsUsersList = await _context.CardsUsers
                        .Where(i => i.CreatedDate.Value.Month == month && i.CreatedDate.Value.Year == year)
                        .Include(i => i.SenderUser)
                        .Include(i => i.ReceiverUser)
                        .Include(i => i.CardsImage)
                        .Include(i => i.CardsMessage)
                        .ToListAsync();


                    var TopCardRecieved = _CardsUsersList.GroupBy(
                        p => p.ReceiverId,
                        p => p,
                        (ReceiverId, cards) => new
                        {
                            ReceiverId = cards.FirstOrDefault().ReceiverId,
                            Count = cards.Count(),
                            ReceiverUserName = cards.FirstOrDefault().ReceiverUser.UserName,
                            Cards = _Assembler_CardsUsers.WriteListDto(cards)
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

        public async Task<OperationOutput> SubmitCards(CardsUsersObjDto model,IUnitOfWork _unitOfWork , string _serverApiKey)
        {
            try
            {
                bool IsExeist = false;

                var CardsUsersList = await _context.CardsUsers.Where(i => i.CreatedDate.Value.Month == DateTime.Now.Month && i.SenderId == model.SenderId).ToArrayAsync();
                if (CardsUsersList.Any())
                {
                    var _valueSettings = _context.Settings.FirstOrDefaultAsync(i => i.Key == "AppreciationCards" || i.Key == "EidCards")?.Result?.Value;
                    string _value = !string.IsNullOrEmpty(_valueSettings) ? _valueSettings : "0";
                    IsExeist = CardsUsersList?.Count() >=  int.Parse(_value);
                }
                if (IsExeist == false)
                {


                    var _model = new CardsUsers()
                    {
                        CreatedDate = DateTime.Now,
                        CardsImageId = model.CardsImageId,
                        CardsMessageId = model.CardsMessageId,
                        OrignalPic = model.OrignalPic,
                        ReceiverId = model.ReceiverId,
                        SenderId = model.SenderId,
                        EntityId = model.EntityId,

                    };

                    _model.OrignalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                    Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSaveCards, false, 0, pathToSaveCards) : model.OrignalPic;



                    var _entity = _context.CardsUsers.AddAsync(_model).Result.Entity;

                    await _context.SaveChangesAsync();
                    var _CardsUser = _Assembler_CardsUsers.WriteDto(_entity);

                    var usersIds = new List<string?>();
                    usersIds.Add(model.ReceiverId);

                    string entityName = model.EntityId == 41 ? "Eid": model.EntityId == 42 ? "Appreciation": ""; 

                    var _settingsCards = await _context.GeneralMessages.Where(f => f.Key == entityName).Select(s => new
                    {
                        children = _GeneralMessages_Assembler.WriteListDto(_context.GeneralMessages.Where(f => f.ParentId == s.Id).ToList())
                    }).ToListAsync();

                    var obj = new
                    {

                        notificationTitleAr = _settingsCards.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationTitle")?.ValueAr,
                        notificationTitleEn = _settingsCards.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationTitle")?.ValueEn,

                        notificationBodyAr = _settingsCards.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationBody")?.ValueAr,
                        notificationBodyEn = _settingsCards.FirstOrDefault()?.children?.FirstOrDefault(f => f.Key == "notificationBody")?.ValueEn,
                    };

                    var senderName = await _context.Users
                        .Where(x => x.Id == model.SenderId)
                        .Select(x => x.FirstName + " " + x.LastName)
                        .FirstOrDefaultAsync();
                    var cardsMessage = await _context.CardsMessages
                        .Where(x => x.Id == model.CardsMessageId)
                        .Select(x => new { x.MessageAr, x.MessageEn })
                        .FirstOrDefaultAsync();


                    await Notifications.SendNotificationByUsers(_unitOfWork, usersIds, entityName, _entity.Id.ToString(), $"{obj.notificationTitleAr} من {senderName}", $"{obj.notificationTitleEn} from {senderName}", cardsMessage?.MessageAr ?? obj.notificationBodyAr, cardsMessage?.MessageEn ?? obj.notificationBodyEn, _serverApiKey, UserCreatedBy: model.SenderId, entityId: model.EntityId);

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_CardsUser, 1);
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

    }
}
