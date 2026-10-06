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
    internal class AreasRepository : BaseRepository<Area>, IAreasRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Area_Assembler _Area_Assembler;

        public AreasRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Area_Assembler = new Area_Assembler();
            _logger = logger;
            _context = context;

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
        private async Task UpdateAreaGroups(int areaId, List<int>? groupIds)
        {
            if (groupIds == null)
                return;

            // remove old relations
            var existingGroups = _context.GroupAreas
                .Where(x => x.AreaId == areaId);

            _context.GroupAreas.RemoveRange(existingGroups);

            // add new relations
            foreach (var groupId in groupIds.Distinct())
            {
                _context.GroupAreas.Add(new GroupArea
                {
                    AreaId = areaId,
                    GroupId = groupId,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }
        public async Task<OperationOutput> AddNewAsync(AreaDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDelete = false;
                entity.CreatedDate = DateTime.Now;

                var _model = _Area_Assembler.WriteDal(entity);
                var _entity = await AddAsync(_model);

                await _context.SaveChangesAsync();

                // handle many-to-many
                await UpdateAreaGroups(_entity.Id, entity.GroupIds);

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
        public async Task<OperationOutput> DeleteEntity(int id,string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                find.IsDelete = true;
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
                string[] stringArray = { "City" , "GroupAreas" };
                var spec = Specification<Area>.All.And(new AreaSpecification(_filter));
                var Areas = new List<Area>();
                if (_filter.CityId > 0)
                {
                    var _Area = await FindAllAsync(spec.ToExpression(), stringArray);
                    Areas = _Area.Where(i => i.IsDelete == false).ToList();
                }
                else
                {

                    var _Areas = await FindAllAsync(i => i.IsDelete == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize, stringArray);
                    Areas = _Areas.Where(i => i.IsDelete == false).ToList();
                }
                var _AreasDto = _Area_Assembler.WriteListDto(Areas);
                var counts = Count(i => i.IsDelete == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_AreasDto, counts);
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
                var entities = await FindAllAsync(i => i.IsDelete == false);
                var _entities = _Area_Assembler.WriteListDto(entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var count = await CountAsync(i => i.IsDelete == false);
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
                var Area =await _context.Areas
                .Include(r => r.GroupAreas)
                .ThenInclude(rm => rm.Group).FirstAsync(a => a.Id == id);
                if (Area != null)
                {
                    var _Area = _Area_Assembler.WriteDto(Area);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Area, 1);
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
        public async Task<OperationOutput> UpdateEntity(AreaDto entity)
        {
            try
            {
                var _entity = await FindAsync(i => i.Id == entity.Id);

                if (_entity is not null)
                {
                    _entity.UpdatedDate = DateTime.Now;
                    _entity.IsActive = true;
                    _entity.IsDelete = false;
                    _entity.CreatedDate = DateTime.Now;
                    _entity.NameAr = entity.NameAr;
                    _entity.NameEn = entity.NameEn;
                    _entity.Code = entity.Code;
                    _entity.Latitude = entity.Latitude;
                    _entity.Longitude = entity.Longitude;
                    _entity.Diameter = entity.Diameter;
                    _entity.CityId = entity.CityId;
                    _entity.HasAttendanceSystem = entity.HasAttendanceSystem;
                    _entity.IsForAllEmployees = entity.IsForAllEmployees;

                    var _model = Update(_entity);

                    await SaveChangesAsync();

                    // update many-to-many
                    await UpdateAreaGroups(_entity.Id, entity.GroupIds);
                    var _Area = _Area_Assembler.WriteDto(_model);

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(_Area, 1);
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
        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(AreaDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<AreaDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<AreaDto> entities)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationOutput> AssignGroupsToArea(int areaId, List<int> groupIds)
        {
            try
            {
                // Check if area exists
                var area = await FindAsync(f => f.Id == areaId);
                if (area == null)
                {
                    return ResultOutputData.GenearetResultOutputNoDataReturned();
                }

                // Update the groups
                await UpdateAreaGroups(areaId, groupIds);

                // Update the IsForAllEmployees flag if needed
                if (groupIds == null || groupIds.Count == 0)
                {
                    area.IsForAllEmployees = true;
                }
                else
                {
                    area.IsForAllEmployees = false;
                }

                Update(area);
                await SaveChangesAsync();

                var Result = ResultOutputData.GenearetResultOutputSuccess();
                return Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning groups to area");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
    }
}
