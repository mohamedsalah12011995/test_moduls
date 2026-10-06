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
    internal class InterestsRepository : BaseRepository<Interest>,IInterestsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly Interest_Assembler _Assembler;
        private string pathToSave = "";
        private readonly string _serverApiKey;

        public InterestsRepository(ApplicationDbContext context, ILogger logger):base(context,logger)
        {
            _Assembler = new Interest_Assembler();
            _logger = logger;
            _context = context;
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            var folderName = "Images/";
            pathToSave = Path.Combine(sharedPath, folderName);
            _serverApiKey = ConfigurationHelper.GetValueWithParam_String("FcmNotification:ServerKey");
        }


        public async Task<OperationOutput> GetAllDataAsync()
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
        public async Task<OperationOutput> SaveInterestUserByDevice(InterestUserObjDto model)
        {
            try
            {
                var _interstList = await _context.InterestsUsers.Where(i => i.UserId == model.UserId).ToListAsync();
                if (_interstList.Count > 0)
                    _context.InterestsUsers.RemoveRange(_interstList);
                await _context.SaveChangesAsync();


                List<InterestsUser> InterestsUserList = new List<InterestsUser>();
                foreach (var item in model.InterestIds)
                {
                    InterestsUser interestsUser = new()
                    {
                        InterestsId = item,
                        UserId = model.UserId,
                        IsDeleted = false
                    };
                    InterestsUserList.Add(interestsUser);

                }

                await _context.InterestsUsers.AddRangeAsync(InterestsUserList);
                await _context.SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> GetAllInterestsByUserId(string userId)
        {
            try
            {
                var _entities = await FindAllAsync(f => f.CreatedBy == userId && f.IsDeleted == false);
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
        public async Task<OperationOutput> GetAllInterestsByInterstsUserId(string userId)
        {

            try
            {
                List<InterestResult> InterestsList = new List<InterestResult>();
                var _Intersts = await _context.Interests.Include(c => c.InterestsUsers).Where(f => f.IsDeleted == false).ToListAsync();
                var Intersts = _Intersts.Select(s => new InterestDto
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    BriefeContentAr = s.BriefeContentAr,
                    BriefeContentEn = s.BriefeContentEn,
                    OriginalPic = s.OriginalPic,
                    ReferenceId = s.ReferenceId,
                    EntityId = s.EntityId,
                    IsJoin = s.InterestsUsers.Any(i => i.UserId == userId) ? true : false
                }).ToList();



                var InterstsResult = Intersts.Distinct().Select(s => new InterestResult
                {
                    Interest = s,
                    Count = _context.InterestsUsers.Count(c => c.InterestsId == s.Id)
                }).Distinct().ToList();
                InterestsList.AddRange(InterstsResult);


                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDeleted == false);
                var _result = resultOutput.GenearetResultOutput(InterestsList, count);
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
                var Interest = await GetByIdAsync(id);
                var _Interest = _Assembler.WriteDto(Interest);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Interest, 1);
                return _result;
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
                var spec = Specification<Interest>.All.And(new InterestSpecification(_filter));

                var Interests =await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize,null);
                var _Interests = _Assembler.WriteListDto(Interests.ToList());
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Interests, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> AddNewAsync(InterestDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;

                entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
               Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : entity.OriginalPic;

                var _entity = _Assembler.WriteDal(entity);

                var _model = await AddAsync(_entity);
                await SaveChangesAsync();

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_model, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<InterestDto> entities)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> UpdateEntity(InterestDto entity)
        {
            try
            {
                if (!string.IsNullOrEmpty(entity.OriginalPicBase64))
                {
                    entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : entity.OriginalPic;
                }

                var _entity = _Assembler.WriteDal(entity);
                _entity.UpdatedDate = DateTime.Now;
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var _model = Update(_entity);
                await SaveChangesAsync();


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_model, 1);

                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public Task<OperationOutput> UpdateEntityAsync(InterestDto t, object key)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> DeleteEntity(int id,string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsDeleted = true;
                Update(find);
               
                var notificationHistory = await _context.NotificationHistories.Where(f => f.RecordId == id.ToString() && f.EntityId == find.EntityId).ToListAsync();
                if (notificationHistory.Any())
                    _context.NotificationHistories.RemoveRange(notificationHistory);

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

        public Task<OperationOutput> DeleteEntityRange(IEnumerable<InterestDto> entities)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
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
    }
}
