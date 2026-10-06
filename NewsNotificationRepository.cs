using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto.NewsNotifications;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models.NewsNotifications;
using System.Data;

namespace Nupco.EF.Repositories
{
    public class NewsNotificationRepository : BaseRepository<NewsNotification>, INewsNotificationRepository
    {
        private readonly IConfiguration _configuration;
        private readonly NewsNotification_Assembler _Assembler;
        private readonly ILDAPRepository _iLDAPRepository;

        private readonly string _ldapServer;
        private readonly string _ldapAccountlogin;
        private readonly string _ldapPassword;
        private readonly string _ldapDistinguishedName;


        public NewsNotificationRepository(ApplicationDbContext context, ILogger logger, IConfiguration configuration, ILDAPRepository iLDAPRepository) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _Assembler = new NewsNotification_Assembler();
            _configuration = configuration;
            _iLDAPRepository = iLDAPRepository;

            _ldapServer = configuration["LdapSetting:LdapServer"] ?? string.Empty;
            _ldapAccountlogin = configuration["LdapSetting:LdapAccountLogin"] ?? string.Empty;
            _ldapPassword = configuration["LdapSetting:LdapPassword"] ?? string.Empty;
            _ldapDistinguishedName = configuration["LdapSetting:LdapDistinguishedName"] ?? string.Empty;

        }


        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsActive = activate;
                Update(find);
                await SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewAsync(NewsNotificationDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entity);
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



        public async Task<OperationOutput> DeleteEntity(int id, string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsDeleted = true;
                Update(find);
                await SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var NewsNotifications = new List<NewsNotification>();

                var _NewsNotifications = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                NewsNotifications = _NewsNotifications.Where(i => i.IsDeleted == false).ToList();
                var _NewsNotificationsDto = _Assembler.WriteListDto(NewsNotifications);
                var counts = Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_NewsNotificationsDto, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {
                var entities = await FindAllAsync(i => i.IsDeleted == false);
                var _entities = _Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var NewsNotification = await FindAsync(f => f.Id == id);
                if (NewsNotification != null)
                {
                    var _NewsNotification = _Assembler.WriteDto(NewsNotification);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_NewsNotification, 1);
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
        public async Task<OperationOutput> SetPublic(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsPublic = activate;
                Update(find);
                await SaveChangesAsync();


                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> UpdateEntity(NewsNotificationDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.CreatedDate = DateTime.Now;
                    _entity.NameAr = entity.NameAr;
                    _entity.DescriptionAr = entity.DescriptionAr;
                    _entity.NameEn = entity.NameEn;
                    _entity.DescriptionEn = entity.DescriptionEn;
                    _entity.GroupId = entity.GroupId;
                    _entity.UserId = entity.UserId;
                    _entity.Username = entity.Username;
                    _entity.Email = entity.Email;
                    var _model = Update(_entity);
                    await SaveChangesAsync();


                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_model, 1);
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


        public async Task<OperationOutput> GetAllByUser(NewsNotificationUserFilterDto _filter)
        {
            try
            {
                List<int?> _ids = new List<int?>();
                List<int?> userLdapIds = new List<int?>();
                List<NewsNotificationDto> newsNotificationKafoDtos = new List<NewsNotificationDto>();
                var daysString = _configuration["NewsNotificationJobs:KafoDuration"] ?? "7";
                var daysNumber = int.Parse(daysString);
                var userGroups = await _context.GroupUsers.Where(i => i.UserId == _filter.UserId).ToListAsync();
                if (userGroups != null && userGroups.Count > 0)
                {
                    var groupIds = userGroups.Select(s => s.GroupId).ToList();
                    if (groupIds != null && groupIds.Count > 0)
                        _ids.AddRange((IEnumerable<int?>)groupIds);
                }
                var user = await _context.Users.FirstOrDefaultAsync(i => i.Id == _filter.UserId
                || (i.UserName == _filter.Username && i.UserName != null)
                || (i.Email == _filter.Email && i.Email != null)
                );
                if (user != null)
                {
                    var userLdapGroups = _iLDAPRepository.GetUserGroups(user.UserName, _ldapServer, _ldapAccountlogin, _ldapPassword, _ldapDistinguishedName);
                    userLdapIds = await _context.GroupsLDAP.Where(i => userLdapGroups.Contains(i.NameEn)).Select(g => (int?)g.Id).ToListAsync();


                    string[] stringArray = { "SenderUser", "ReceiverUser", "KafoImage", "KafoMessage" };
                    var KafoUsers = new List<KafoUsers>();

                    var _KafoUsersList = await _context.KafoUsers
                            .Where(i => i.CreatedDate <= DateTime.Now && i.CreatedDate >= DateTime.Now.AddDays(-daysNumber) && i.ReceiverId == user.Id)
                            .Include(i => i.SenderUser)
                            .Include(i => i.ReceiverUser)
                            .Include(i => i.KafoImage)
                            .Include(i => i.KafoMessage)
                            .ToListAsync();
                    if (_KafoUsersList.Any())
                    {
                        var messageAr = "  تم ارسال بطاقة كفو  من قبل  ";
                        var messageEn = "You have recieved Kafo From ";
                        newsNotificationKafoDtos = _KafoUsersList.Select(u => new NewsNotificationDto
                        {
                            DescriptionAr = messageAr + u.SenderUser.FirstName + " " + u.SenderUser.LastName,
                            DescriptionEn = messageEn + u.SenderUser.FirstName + " " + u.SenderUser.LastName,
                            NameAr = messageAr + u.SenderUser.UserName + " " + u.SenderUser.LastName,
                            NameEn = messageEn + u.SenderUser.UserName + " " + u.SenderUser.LastName,
                            TypeLink = "KafoList",
                            TypeMessageTemplateAr = messageAr + u.SenderUser.UserName + " " + u.SenderUser.LastName,
                            TypeMessageTemplateEn = messageEn + u.SenderUser.UserName + " " + u.SenderUser.LastName,
                            TypeNameAr = messageAr + u.SenderUser.UserName + " " + u.SenderUser.LastName,
                            TypeNameEn = messageEn + u.SenderUser.UserName + " " + u.SenderUser.LastName,
                            Username = user.UserName,
                            Email = user.Email,
                            UserId = user.Id,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedDate = u.CreatedDate,
                        }).ToList();

                    }
                }
                var NewsNotifications = new List<NewsNotification>();
                var _NewsNotifications = await _context.NewsNotifications.Where(i => i.IsDeleted == false
                                        && i.IsActive == true
                                        && (
                                        (i.UserId == _filter.UserId && i.UserId != null)
                                        || (i.Username == _filter.Username && i.Username != null)
                                        || (i.Email == _filter.Email && i.Email != null)
                                        || (i.IsPublic == true)
                                        || (_ids.Contains(i.GroupId) && i.GroupId != null)
                                        || (userLdapIds.Contains(i.GroupLDAPId) && i.GroupLDAPId != null))
                                        ).Include(u => u.Type).ToListAsync();

                NewsNotifications = _NewsNotifications.Where(i =>
                                                    ( (i.TypeId == 5 || i.TypeId == 4)// monthly over time type 
                                                    && i.CreatedDate <= DateTime.Now
                                                    && i.CreatedDate >= DateTime.Now.AddDays(-daysNumber))
                                                    || (i.TypeId != 5 && i.TypeId != 4 && i.CreatedDate.Value.Date == DateTime.Now.Date)
                                                    ).ToList();
                var _NewsNotificationsDto = _Assembler.WriteListDto(NewsNotifications);
                if (newsNotificationKafoDtos != null && newsNotificationKafoDtos.Count > 0)
                {
                    _NewsNotificationsDto.AddRange(newsNotificationKafoDtos);
                }
                var counts = _NewsNotificationsDto.Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_NewsNotificationsDto, counts);
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
