using Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core.Assembler;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models.Users;

namespace Nupco.EF.Repositories
{
    internal class UserSettingsRepository : BaseRepository<UserSetting>, IUserSettingsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserSetting_Assembler _Assembler = new UserSetting_Assembler();

        private string pathToSave = "";
        private readonly string _serverApiKey;

        public UserSettingsRepository(ApplicationDbContext context, ILogger logger):base(context,logger)
        {
            _logger = logger;
            _context = context;
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            var folderName = "Images/";
            pathToSave = Path.Combine(sharedPath, folderName);
            _serverApiKey = Core.Helpers.ConfigurationHelper.GetValueWithParam_String("FcmNotification:ServerKey");

        }

        public async Task<OperationOutput> CreateUserSetting(UserSettingDto model)
        {
            try
            {
                var _entity = await FindAsync(f => f.UserId == model.UserId);
                if (_entity is null)
                {
                    var _model = _Assembler.WriteDal(model);
                    _entity = await AddAsync(_model);
                }
                else
                {
                    _entity.AllowAppNotification = model.AllowAppNotification;
                    _entity.AllowPushNotification = model.AllowPushNotification;
                    _entity.AllowSkills = model.AllowSkills;
                    _entity.AllowMessages = model.AllowMessages;
                    _entity.AllowInterests = model.AllowInterests;
                    _entity.AllowCertificates = model.AllowCertificates;
                    _entity.Temp1 = model.Temp1;
                    _entity.Temp2 = model.Temp2;
                    _entity.Temp3 = model.Temp3;
                    _entity.Temp4 = model.Temp4;
                    _entity.Temp5 = model.Temp5;
                    _entity.Language = model.Language;
                    _entity.UserId = model.UserId;

                    var entity = Update(_entity);
                }
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

        public async Task<OperationOutput> GetUserSettingById(string UserId)
        {
            try
            {
                var _entities = await FindAllAsync(i => i.UserId == UserId);
                var _UserSettings = _Assembler.WriteListDto(_entities);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_UserSettings, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<UserSettingDto> GetUserSettingsByUserId(string UserId,string Language)
        {
            try
            {
                var userSettings = await FindAsync(i => i.UserId == UserId);

                if (userSettings is null)
                {
                   var _entity = new UserSetting();
                    _entity.AllowAppNotification = true;
                    _entity.AllowPushNotification = true;
                    _entity.AllowSkills = true;
                    _entity.AllowMessages = true;
                    _entity.AllowInterests = true;
                    _entity.AllowCertificates = true;
                    _entity.Temp1 = true;
                    _entity.Temp2 = true;
                    _entity.Temp3 = true;
                    _entity.Temp4 = true;
                    _entity.Temp5 = true;
                    _entity.Language = Language;
                    _entity.UserId = UserId;

                    var entity = AddAsync(_entity);
                    await SaveChangesAsync();

                    userSettings = _entity;

                }
                var _UserSettings = _Assembler.WriteDto(userSettings);
                return _UserSettings;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
