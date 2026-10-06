using Core.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models.LdapUsers;
using Nupco.DAL.Models.Users;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    internal class UserProfileRepository : BaseRepository<User>, IUserProfileRepository
    {
        private readonly User_Assembler _Assembler_User = new User_Assembler();
        private readonly Post_Assembler _Assembler_Post = new Post_Assembler();
        private readonly Channel_Assembler _Assembler_Channel = new Channel_Assembler();
        private readonly Comment_Assembler _Assembler_Comment = new Comment_Assembler();
        private readonly Certificate_Assembler _Assembler_Certificate = new Certificate_Assembler();
        //string Token = string.Empty;
        private string pathToSave = "";
        private readonly UserManager<User> _userManager;
        private readonly string _ldapServer;
        private readonly string _ldapAccountlogin;
        private readonly string _ldapPassword;
        private readonly string _ldapDistinguishedName;

        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly ILdapUserProfileRepository _ldapUserProfileRepository;
        private IOptions<LdapSetting> _appSettings;
            
        private readonly UserSetting_Assembler _Assembler = new UserSetting_Assembler();

        private readonly string _serverApiKey;


        public UserProfileRepository(ApplicationDbContext context, ILogger logger, UserManager<User> userManager, ILdapUserProfileRepository ldapUserProfileRepository) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _ldapUserProfileRepository = ldapUserProfileRepository;
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");
            var folderName = "Images/";
            pathToSave = Path.Combine(sharedPath, folderName);
            _serverApiKey = ConfigurationHelper.GetValueWithParam_String("FcmNotification:ServerKey");

        }

        public async Task<OperationOutput> GetUserInfoProfileById(string userId)
        {
            try
            {

                var user = await FindAsync(f => f.Id == userId);
                if (user is null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                var _Users = _Assembler_User.WriteDto(user);
                await SyncUserProfileFromLdapAsync(user, _Users);
                var _info = new
                {
                    User = _Users,
                    Skills = await _context.SkillsUsers.CountAsync(i => i.UserId == userId),
                    CommentCount = await _context.Comments.CountAsync(i => i.CreatedBy == userId),
                    PostsCount = await _context.Posts.CountAsync(i => i.CreatedBy == userId && i.IsDeleted==false),
                    ChannelCount = await _context.ChanalsUsers.CountAsync(i => i.UserId == userId && i.IsDeleted == false),
                    
                    Posts = _Assembler_Post.WriteListDto( await _context.Posts.Where(i => i.CreatedBy == userId && i.IsDeleted == false).ToListAsync()),
                    Channels =_Assembler_Channel.WriteListDto( await _context.ChanalsUsers.Include(i=> i.Channel).Where(i => i.UserId == userId).Select(s=> s.Channel).Where(i =>  i.IsDeleted == false).ToListAsync()),
                    Comments =_Assembler_Comment.WriteListDto( await _context.Comments.Include(i=> i.CommentReplies).Where(i => i.CreatedBy == userId).ToListAsync()),
                    IsGoldMedal = await GetUserMedalTierAsync(userId)

            };

                foreach (var _entity in _info.Posts)
                {
                    var userIsLiked = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId && f.UserId == userId);
                    _entity.IsLiked = userIsLiked != 0 ? true : false;
                    _entity.LikeNo = await _context.InterActions.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);
                    _entity.CommentNo = await _context.Comments.CountAsync(f => f.ItemId == _entity.Id.ToString() && f.EntityId == _entity.EntityId);

                }

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_info, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        private async Task SyncUserProfileFromLdapAsync(User user, UserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                userDto.PositionEntryDate = user.HireDate;
                return;
            }

            var ldapProfile = await _ldapUserProfileRepository.GetByUserNameAsync(user.UserName);

            userDto.PositionEntryDate = user.HireDate;

            if (ldapProfile is null)
            {
                return;
            }

            var hasChanges = ApplyLdapProfileToUser(user, ldapProfile);
            if (hasChanges)
            {
                Update(user);
                await SaveChangesAsync();
            }

            userDto.Code = user.Code;
            userDto.DepartmentName = user.DepartmentName;
            userDto.DivisionName = user.DivisionName;
            userDto.PositionName = user.PositionName;
            userDto.PositionEntryDate = user.HireDate;
        }

        private static bool ApplyLdapProfileToUser(User user, LdapUserProfile ldapProfile)
        {
            var hasChanges = false;

            hasChanges |= SetIfDifferent(value => user.Code = value, user.Code, ldapProfile.EmployeeId);
            hasChanges |= SetIfDifferent(value => user.DepartmentName = value, user.DepartmentName, ldapProfile.Department);
            hasChanges |= SetIfDifferent(value => user.DivisionName = value, user.DivisionName, ldapProfile.Division);
            hasChanges |= SetIfDifferent(value => user.PositionName = value, user.PositionName, ldapProfile.Title);

            return hasChanges;
        }

        private static bool SetIfDifferent(Action<string?> setValue, string? currentValue, string? newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
            {
                return false;
            }

            if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
            {
                return false;
            }

            setValue(newValue);
            return true;
        }

        public async Task<List<LeaderboardEntryDto>> GetTop10LeaderboardAsync(int? medalId = null)
        {
            try
            {
                var userMedalsQuery = _context.MedalUsers
                    .Include(i => i.Medal)
                    .Where(mu => mu.Medal.IsDeleted == false
                                 && mu.IsRevoked == false
                                 && (mu.ExpiryDate == null || mu.ExpiryDate > DateTime.Now)
                                 && (!medalId.HasValue || mu.MedalId == medalId.Value))
                    .GroupBy(mu => new
                    {
                        mu.AssignedBy,
                        mu.AssignedByUser.FirstName,
                        mu.AssignedByUser.LastName,
                        mu.AssignedByUser.PositionName,
                    })
                    .Select(g => new LeaderboardEntryDto
                    {
                        MedalTitleAr = g.Select(x => x.Medal.TitleAr).FirstOrDefault() ?? string.Empty,
                        MedalTitleEn = g.Select(x => x.Medal.TitleEn).FirstOrDefault() ?? string.Empty,
                        UserId = g.Key.AssignedBy,
                        FullName = ((g.Key.FirstName ?? "") + " " + (g.Key.LastName ?? "")).Trim(),
                        Position = g.Key.PositionName ?? string.Empty,
                        MedalsCount = g.Count()
                    });

                var top10DistinctCounts = await userMedalsQuery
                    .Select(x => x.MedalsCount)
                    .Distinct()
                    .OrderByDescending(c => c)
                    .Take(10)
                    .ToListAsync();

                var top10Leaderboard = await userMedalsQuery
                    .Where(x => top10DistinctCounts.Contains(x.MedalsCount))
                    .OrderByDescending(x => x.MedalsCount)
                    .ThenBy(x => x.FullName)
                    .Take(10)
                    .Distinct()
                    .ToListAsync();

                return top10Leaderboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetTop10LeaderboardAsync");
                return null;
            }
        }

        public async Task<List<LeaderboardEntryDto>> GetTop100LeaderboardAsync(int? medalId = null)
        {
            try
            {
                var baseQuery = _context.MedalUsers
                    .Include(i => i.Medal)
                    .Where(mu => mu.Medal.IsDeleted == false
                                 && mu.IsRevoked == false
                                 && (mu.ExpiryDate == null || mu.ExpiryDate > DateTime.Now)
                                 && (!medalId.HasValue || mu.MedalId == medalId.Value))
                    .GroupBy(mu => new
                    {
                        mu.AssignedBy,
                        mu.AssignedByUser.FirstName,
                        mu.AssignedByUser.LastName,
                        mu.AssignedByUser.PositionName
                    })
                    .Select(g => new LeaderboardEntryDto
                    {
                        MedalTitleAr = g.FirstOrDefault().Medal.TitleAr,
                        MedalTitleEn = g.FirstOrDefault().Medal.TitleEn,
                        UserId = g.Key.AssignedBy,
                        FullName = (g.Key.FirstName + " " + g.Key.LastName).Trim(),
                        Position = g.Key.PositionName,
                        MedalsCount = g.Count()
                    })
                    .OrderByDescending(x => x.MedalsCount);

                var leaderboard = await baseQuery
                     .OrderByDescending(x => x.MedalsCount)
                    .ThenBy(x => x.FullName)
                    .Skip(10)
                    .Take(100)
                    .Distinct()
                    .ToListAsync();

                return leaderboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetTop100LeaderboardAsync");
                return null;
            }
        }


        public enum MedalTier
        {
            None,
            Gold,
            Silver
        }

        public async Task<MedalTier> GetUserMedalTierAsync(string userId, int? medalId=null)
        {
            var top10 = await GetTop10LeaderboardAsync(medalId);
            if (top10.Any(x => x.UserId == userId))
            {
                return MedalTier.Gold;
            }

            var top100 = await GetTop100LeaderboardAsync(medalId);
            if (top100.Any(x => x.UserId == userId))
            {
                return MedalTier.Silver;
            }

            return MedalTier.None;
        }
       


        public async Task<OperationOutput> GetUserProfileById(string userId)
        {
            try
            {

                var user = await FindAsync(f => f.Id == userId);
                var _Users = _Assembler_User.WriteDto(user);
                // Backward compatible unread count:
                // - New rows use `NotifiedUser` as the receiver.
                // - Older rows might have `NotifiedUser` as null and store receiver in `UserId`.
                _Users.UnreadNotificationsCount = await _context.NotificationHistories
                    .CountAsync(x =>
                        !x.IsRead &&
                        ((x.NotifiedUser == userId) || (x.NotifiedUser == null && x.UserId == userId)));

                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Users, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> UpdateUserProfileByInternalSite(UserProfileDto entityDto)
        {
            try
            {
                var entity = await _userManager.FindByIdAsync(entityDto.Id);
                if (entity is not null)
                {
                    if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                    {
                        entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                          Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, pathToSave, true, 400, pathToSave) : entity.OriginalPic;

                    }
                    entity.OriginalPic = !String.IsNullOrEmpty(entityDto.OriginalPicBase64) ? entityDto.OriginalPic : entity.OriginalPic;
                    entity.Bio = entityDto.Bio;
                    entity.PhoneNumber = entityDto.PhoneNumber;
                    entity.BirthDate = entityDto.BirthDate;
                    entity.Email = entityDto.Email;
                    entity.NormalizedEmail = entityDto.Email?.ToUpper().ToString();

                    var user = Update(entity);
                    await SaveChangesAsync();
                    user.PositionName = "";
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(user, 1);
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


        public async Task<OperationOutput> UpdateUserProfile(UserProfileDto entityDto)
        {
            try
            {
                var entity = await _userManager.FindByIdAsync(entityDto.Id);
                if (entity is not null)
                {
                    if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                    {
                        entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                          Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, pathToSave, true, 400, pathToSave) : entity.OriginalPic;

                    }
                    entity.FirstName = entityDto.firstName;
                    entity.LastName = entityDto.lastName;
                    entity.OriginalPic = !String.IsNullOrEmpty(entityDto.OriginalPicBase64) ? entityDto.OriginalPic : entity.OriginalPic;
                    entity.Bio = entityDto.Bio;
                    entity.PhoneNumber = entityDto.PhoneNumber;

                    var user = Update(entity);
                    await SaveChangesAsync();
                    user.PositionName = "";
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(user, 1);
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

        public async Task<OperationOutput> UpdateImagUser(UserProfileDto entityDto)
        {
            try
            {
                var entity = await _userManager.FindByIdAsync(entityDto.Id);
                if (entity is not null)
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, pathToSave, true, 400, pathToSave) : entity.OriginalPic;
                    entity.OriginalPic = entityDto.OriginalPic;
                    Update(entity);
                    await SaveChangesAsync();

                    var _Result = ResultOutputData.GenearetResultOutputSuccess();
                    return _Result;
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

        public async Task<OperationOutput> UpdateStatusUser(UserProfileDto entityDto)
        {
            try
            {
                var entity = await FindAsync(f => f.Id == entityDto.Id);
                if (entity is not null)
                {
                    entity.StatusAvailable = entityDto.StatusAvailable;
                    Update(entity);
                    await SaveChangesAsync();

                    var _Result = ResultOutputData.GenearetResultOutputSuccess();
                    return _Result;
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

        public async Task<OperationOutput> GetAllCertificate(string userId)
        {
            try
            {
                var _entities = await _context.Certificates.Where(i => i.UserId == userId).ToListAsync();
                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entities, _entities.Count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> CreateCertificate(CertificateObjDto model)
        {
            try
            {
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                var _model = _Assembler_Certificate.WriteDal(model);
                _model.OrignalFile = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                Images.UploadPdfFileNew(model.OriginalPicBase64, sharedPath + "Files/") : _model.OrignalFile;

                var _entity = await _context.Certificates.AddAsync(_model);
                await SaveChangesAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entity.Entity, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> DeleteCertificate(int id)
        {
            try
            {
                var find = await _context.Certificates.FirstOrDefaultAsync(f => f.Id == id);
                _context.Certificates.Remove(find);
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

        public async Task<OperationOutput> GetUser(string userName)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userName);
                if(user is not null)
                {
                    ResultOutputData result = new ResultOutputData();
                    user.PositionName = "";
                    var _result = result.GenearetResultOutput(user, 1);
                    return _result;

                }
                var Result = ResultOutputData.GenearetResultOutputUserNotExist();
                return Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> SearchLdapUsers(List<User> _users)
        {
            try
            {
                if (_users is not null && _users.Count() > 0)
                {
                    for (int i = 0; i < _users.Count; i++)
                    {
                        _users[i].PositionName = "";
                    }
                    var userDto = _Assembler_User.WriteListDto(_users);

                   var users = await Task.FromResult(userDto);

                    ResultOutputData resultOutout = new ResultOutputData();
                    var _result = resultOutout.GenearetResultOutputUserDevice(users);
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
