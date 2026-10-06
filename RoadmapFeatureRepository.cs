using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Pic;
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
using static Nupco.Core.Helpers.JWTHelper;
using Comment = Nupco.DAL.Models.Comment;

namespace Nupco.EF.Repositories
{
    public class RoadmapFeatureRepository : BaseRepository<RoadmapFeature>, IRoadmapFeatureRepository
    {
        private readonly IConfiguration _configuration;
        private readonly RoadmapFeature_Assembler _Assembler;

        public RoadmapFeatureRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _Assembler = new RoadmapFeature_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(RoadmapFeatureDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entity);
                if (_model.PicReleaseId == 0)
                    _model.PicReleaseId = null;

                if (_model.PicProductId == 0)
                    _model.PicProductId = null;
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
                var RoadmapFeatures = new List<RoadmapFeature>();
                var _RoadmapFeatures = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize);
                RoadmapFeatures = _RoadmapFeatures.Where(i => i.IsDeleted == false).ToList();
                var _RoadmapFeaturesDto = _Assembler.WriteListDto(RoadmapFeatures);
                var counts = Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_RoadmapFeaturesDto, counts);
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
                var RoadmapFeature = await FindAsync(f => f.Id == id);
                if (RoadmapFeature != null)
                {
                    var _RoadmapFeature = _Assembler.WriteDto(RoadmapFeature);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_RoadmapFeature, 1);
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

        public async Task<OperationOutput> UpdateEntity(RoadmapFeatureDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {

                    _Assembler.WriteDal(entity, _entity);

                    _entity.UpdatedDate = DateTime.Now;
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

        public RoadmapFeatureRepository(ApplicationDbContext context, ILogger<RoadmapFeatureRepository> logger)
            : base(context, logger)
        {
            _logger = logger;
            _Assembler = new RoadmapFeature_Assembler();
        }

        public async Task<OperationOutput> GetRoadmapByQuarterAsync(int year, int quarter)
        {
            try
            {
                var startDate = new DateTime(year, (quarter - 1) * 3 + 1, 1);
                var endDate = startDate.AddMonths(3).AddDays(-1);

                var features = await GetFullFeatureQuery()
                    .Where(f => f.PlannedReleaseDate >= startDate && f.PlannedReleaseDate <= endDate)
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap for year {Year} quarter {Quarter}", year, quarter);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetFeaturesByModuleAsync(int moduleId)
        {
            try
            {
                var features = await GetFullFeatureQuery()
                    .Where(f => f.PicModuleId == moduleId)
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting features for module ID {ModuleId}", moduleId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> GetFeaturesByReleaseAsync(int ReleaseId)
        {
            try
            {
                var features = await GetFullFeatureQuery()
                    .Where(f => f.PicReleaseId == ReleaseId)
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting features for Release Id {ReleaseId}", ReleaseId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> GetFeaturesByProdcutAsync(int ProductId)
        {
            try
            {
                var features = await GetFullFeatureQuery()
                    .Where(f => f.PicRelease!.PicProductId == ProductId || f.PicProductId == ProductId)
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting features for Product Id {ProductId}", ProductId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetFeaturesByStatusAsync(FeatureStatus status)
        {
            try
            {
                var features = await GetFullFeatureQuery()
                    .Where(f => f.Status == status)
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting features with status {Status}", status);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetDependenciesAsync(int featureId)
        {
            try
            {
                var feature = await GetFullFeatureQuery()
                    .FirstOrDefaultAsync(f => f.Id == featureId);

                if (feature == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var dependencies = new List<RoadmapFeature>();

                // Get features that depend on this feature
                var dependentFeatures = await GetFullFeatureQuery()
                    .Where(f => f.DependencyFeatureId == featureId)
                    .ToListAsync();

                // Get features that this feature depends on
                if (feature.DependencyFeatureId.HasValue)
                {
                    var dependencyFeature = await GetFullFeatureQuery()
                        .FirstOrDefaultAsync(f => f.Id == feature.DependencyFeatureId.Value);

                    if (dependencyFeature != null)
                        dependencies.Add(dependencyFeature);
                }

                var result = new
                {
                    Dependencies = _Assembler.WriteListDto(dependencies),
                    DependentFeatures = _Assembler.WriteListDto(dependentFeatures)
                };

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(result, dependencies.Count + dependentFeatures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dependencies for feature ID {FeatureId}", featureId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetRoadmapTimelineAsync(int? moduleId = null, int? quarter = null, int? year = null)
        {
            try
            {
                var query = GetFullFeatureQuery();

                if (moduleId.HasValue)
                {
                    query = query.Where(f => f.PicModuleId == moduleId.Value);
                }

                if (year.HasValue && quarter.HasValue)
                {
                    var startDate = new DateTime(year.Value, (quarter.Value - 1) * 3 + 1, 1);
                    var endDate = startDate.AddMonths(3).AddDays(-1);
                    query = query.Where(f => f.PlannedReleaseDate >= startDate && f.PlannedReleaseDate <= endDate);
                }

                var features = await query
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                // Group by module for timeline view
                var timelineData = features
                    .GroupBy(f => f.Module?.Name ?? "Unknown")
                    .Select(g => new
                    {
                        Module = g.Key,
                        Features = _Assembler.WriteListDto(g.ToList())
                    })
                    .ToList();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(timelineData, features.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap timeline");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        #region Helper Methods

        private IQueryable<RoadmapFeature> GetFullFeatureQuery()
        {
            return _context.RoadmapFeatures
                .Include(f => f.Module)
                .Include(f => f.PicRelease)
                .Include(f => f.DependencyModule)
                .Include(f => f.DependencyFeature)
                .Include(f => f.Documents)
                .Where(f => f.IsDeleted == false);
        }

        #endregion



        public async Task<OperationOutput> SearchFeaturesAsync(string keyword)
        {
            try
            {
                keyword = keyword?.Trim().ToLower();
                var query = GetFullFeatureQuery()
                    .Where(f =>
                        f.NameEn.ToLower().Contains(keyword) ||
                        f.NameAr.ToLower().Contains(keyword) ||
                        f.Module.Name.ToLower().Contains(keyword) ||
                        (f.DependencyFeature != null && f.DependencyFeature.NameEn.ToLower().Contains(keyword)));

                var features = await query.OrderBy(f => f.PlannedReleaseDate).ToListAsync();
                var dtos = _Assembler.WriteListDto(features);

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching features with keyword {Keyword}", keyword);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> GetUpcomingReleasesAsync()
        {
            try
            {
                var today = DateTime.Now;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var features = await GetFullFeatureQuery()
                    .Where(f =>
                        (f.PlannedUATDate >= startOfMonth && f.PlannedUATDate <= endOfMonth) ||
                        (f.PlannedProductionDate >= startOfMonth && f.PlannedProductionDate <= endOfMonth))
                    .OrderBy(f => f.PlannedUATDate)
                    .ThenBy(f => f.PlannedProductionDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);
                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming UAT or production releases");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> GetRoadmapSummaryAsync(int? year = null, int? quarter = null)
        {
            try
            {
                // Build base query
                var query = GetFullFeatureQuery();

                // Apply year filter if provided
                if (year.HasValue)
                {
                    query = query.Where(f => f.PlannedProductionDate!.Value.Year == year.Value);
                }

                // Apply quarter filter only if specified
                if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
                {
                    var startDate = new DateTime(year ?? DateTime.Now.Year, (quarter.Value - 1) * 3 + 1, 1);
                    var endDate = startDate.AddMonths(3).AddDays(-1);
                    query = query.Where(f => f.PlannedReleaseDate >= startDate && f.PlannedReleaseDate <= endDate);
                }

                var features = await query.ToListAsync();

                // ✅ Detailed features list
                var detailedList = features.Select(f => new
                {
                    f.Id,
                    f.NameEn,
                    f.NameAr,
                    f.FeatureId,
                    f.PicModuleId,
                    ModuleName = f.Module?.Name,
                    f.Status,
                    f.PlannedReleaseDate,
                    f.BusinessObjectiveAr,
                    f.BusinessObjectiveEn,
              
                    f.CreatedDate
                }).ToList();

                // ✅ Summary grouped by status
                var summary = features
                    .GroupBy(f => f.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToList();

                // ✅ Combine both
                var result = new
                {
                    Summary = summary,
                    Total = features.Count,
                    Details = detailedList
                };

                ResultOutputData output = new ResultOutputData();
                return output.GenearetResultOutput(result, features.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap summary for {Year} Q{Quarter}", year, quarter);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetCrossModuleDependenciesAsync()
        {
            try
            {
                var dependencies = await GetFullFeatureQuery()
                    .Where(f => f.DependencyFeatureId.HasValue && f.DependencyModuleId.HasValue)
                    .Select(f => new
                    {
                        Feature = f.NameEn,
                        Module = f.Module.Name,
                        DependsOnFeature = f.DependencyFeature.NameEn,
                        DependsOnModule = f.DependencyModule.Name
                    })
                    .ToListAsync();

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(dependencies, dependencies.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cross-module dependencies");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetFilteredRoadMapFeatures(int productId, int? moduleId = null, int? quarter = null, FeatureStatus? status = null, string searchKeyword = null, int? releaseId = null)
        {
            try
            {
                // Build base query with product filter
                var query = GetFullFeatureQuery()
                    .Where(f => f.PicRelease != null && f.PicRelease.PicProductId == productId);

                // Apply additional filters
                if (moduleId.HasValue)
                {
                    query = query.Where(f => f.PicModuleId == moduleId.Value);
                }

                if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
                {
                    var currentYear = DateTime.Now.Year;
                    var startDate = new DateTime(currentYear, (quarter.Value - 1) * 3 + 1, 1);
                    var endDate = startDate.AddMonths(3).AddDays(-1);
                    query = query.Where(f => f.PlannedReleaseDate >= startDate && f.PlannedReleaseDate <= endDate);
                }

                if (status.HasValue)
                {
                    query = query.Where(f => f.Status == status.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                {
                    searchKeyword = searchKeyword.Trim().ToLower();
                    query = query.Where(f =>
                        f.NameEn.ToLower().Contains(searchKeyword) ||
                        f.NameAr.ToLower().Contains(searchKeyword) ||
                        f.BusinessObjectiveEn.ToLower().Contains(searchKeyword) ||
                        f.BusinessObjectiveAr.ToLower().Contains(searchKeyword) ||
                        (f.Module != null && f.Module.Name.ToLower().Contains(searchKeyword)));
                }

                if (releaseId.HasValue)
                {
                    query = query.Where(f => f.PicReleaseId == releaseId.Value);
                }

                // Execute query
                var features = await query
                    .OrderBy(f => f.PlannedReleaseDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(features);

                // Generate summary
                var summary = new
                {
                    InDevelopment = features.Count(f => f.Status == FeatureStatus.InDevelopment),
                    InUAT = features.Count(f => f.Status == FeatureStatus.InUAT),
                    ReadyForProduction = features.Count(f => f.Status == FeatureStatus.ReadyForProduction),
                    All = features.Count
                };

                // Combine results
                var result = new
                {
                    Features = dtos,
                    Summary = summary,
                    TotalCount = features.Count
                };

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(result, features.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered roadmap features for product {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> AddRoadMapFeutureeDocs(List<RoadmapFeatureDocDto> docs)
        {
            try
            {
                if(docs != null && docs.Count > 0)
                {
                    var OldDocs = _context.RoadmapFeatureDocs.Where(R => R.RoadmapFeatureId == docs[0].RoadmapFeatureId);
                     _context.RoadmapFeatureDocs.RemoveRange(OldDocs);

                }
                var newfeaturesDocs = _Assembler.WriteDocsListDal(docs);

                await _context.RoadmapFeatureDocs.AddRangeAsync(newfeaturesDocs);

                // 💾 Step 4: Save all changes
                await _context.SaveChangesAsync();
           

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(docs, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving documents for release ID {ReleaseId}", docs);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
    }
}
