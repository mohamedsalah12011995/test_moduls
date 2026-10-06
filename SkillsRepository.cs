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
    internal class SkillsRepository : BaseRepository<Skill> , ISkillRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly Skill_Assembler _Assembler;
        private readonly SkillsUser_Assembler _Assembler_SkillUser = new SkillsUser_Assembler();


        private string pathToSave = "";
        private readonly string _serverApiKey;

        public SkillsRepository(ApplicationDbContext context, ILogger logger):base(context,logger)
        {
            _Assembler = new Skill_Assembler();
            _logger = logger;
            _context = context;

            var folderName = "Images/";
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");
            pathToSave = Path.Combine(sharedPath, folderName);
            _serverApiKey = Core.Helpers.ConfigurationHelper.GetValueWithParam_String("FcmNotification:ServerKey");

        }

        public async Task<OperationOutput> ActivateSkill(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsActive = activate;
                Update(find);
                await SaveChangesAsync();

                var _result = ResultOutputData.GenearetResultOutputSuccess();
                return _result;

            }
            catch (Exception)
            {
                var _result = ResultOutputData.GenearetResultOutputCatch();
                return _result;
            }
        }



        public async Task<OperationOutput> CreateSkill(SkillObjDto model)
        {
            try
            {
                var _model = _Assembler.WriteDal(model);
                if (!string.IsNullOrEmpty(model.OriginalPicBase64))
                {
                    _model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                    Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : _model.OriginalPic;

                }
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

        public async Task<OperationOutput> CreateSkillsUser(SkillUserObjDto model)
        {
            try
            {
                var _model = _Assembler_SkillUser.WriteDal(model);
                var _entity = await _context.SkillsUsers.AddAsync(_model);

                await _context.SaveChangesAsync();

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

        public async Task<OperationOutput> DeleteSkill(int id)
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

        public async Task<OperationOutput> DeleteSkillUser(int id, string userId)
        {
            try
            {
                var find = await _context.SkillsUsers.Include(s=> s.Skill).FirstOrDefaultAsync(f => f.SkillId == id && f.UserId == userId);
                _context.SkillsUsers.Remove(find);

                var notificationHistory = await _context.NotificationHistories.FirstOrDefaultAsync(f => f.RecordId == id.ToString() && f.EntityId == find.Skill.EntityId);
                if (notificationHistory is not null)
                    _context.NotificationHistories.Remove(notificationHistory);

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

        public async Task<OperationOutput> EditSkill(SkillObjDto model)
        {
            try
            {
                var _entity = await FindAsync(f => f.Id == model.Id);
                if (_entity is not null)
                {
                    if (!string.IsNullOrEmpty(model.OriginalPicBase64))
                    {
                        model.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                        Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : model.OriginalPic;

                    }
                    _entity.NameAr = model.NameAr;
                    _entity.NameEn = model.NameEn;
                    _entity.DescriptionAr = model.DescriptionAr;
                    _entity.DescriptionEn = model.DescriptionEn;
                    _entity.OriginalPic = !String.IsNullOrEmpty(model.OriginalPicBase64) ? model.OriginalPic : _entity.OriginalPic;
                    _entity.EntityId = model.EntityId;
                    _entity.ReferenceId = model.ReferenceId;
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    var entity = Update(_entity);
                    await SaveChangesAsync();


                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(entity, 1);
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

        public async Task<OperationOutput> GetAllByPagenation(FiltersBy _filter)
        {
            try
            {
                var spec = Specification<Skill>.All.And(new SkillSpecification(_filter));

                var Skills = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                var _Skills = _Assembler.WriteListDto(Skills);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Skills, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllSkills()
        {
            try
            {
                var _entities = await FindAllAsync(i => i.IsDeleted == false);
                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllSkillsUsersByUserId(string userId)
        {
            try
            {
                var _entities = await _context.SkillsUsers.Include(i => i.Skill).Include(i => i.User)
                    .Where(i => i.UserId == userId).Select(s => new SkillUserDto
                    {
                        SkillNameAr = s.Skill.NameAr,
                        SkillNameEn = s.Skill.NameEn,
                        SkillId = s.SkillId,
                        UserId = userId,
                        UserName = s.User.UserName,
                        Id = s.Id
                    }).ToListAsync()
                    ;
                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(_entities, count);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllSkillsWithJoin(string userId)
        {
            try
            {
                List<SkillResult> SkillsList = new List<SkillResult>();
                var _Skill = await _context.Skills.Include(c => c.SkillsUsers).Where(f => f.IsDeleted == false).ToListAsync();
                var Skill = _Skill.Select(s => new SkillDto
                {
                    Id = s.Id,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    DescriptionAr = s.DescriptionAr,
                    DescriptionEn = s.DescriptionEn,
                    OriginalPic = s.OriginalPic,
                    IsJoin = s.SkillsUsers.Any(i => i.UserId == userId) ? true : false
                }).ToList();



                var SkillResult = Skill.Distinct().Select(s => new SkillResult
                {
                    skill = s,
                    Count = _context.SkillsUsers.Count(c => c.SkillId == s.Id)
                }).Distinct().ToList();
                SkillsList.AddRange(SkillResult);


                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(SkillsList, count);
                return _result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetSkillById(int id)
        {
            try
            {
                var Skill = await GetByIdAsync(id);
                var _Skill = _Assembler.WriteDto(Skill);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Skill, 1);
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
