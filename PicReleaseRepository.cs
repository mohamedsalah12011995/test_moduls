using Core.Helpers;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto.Pic;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Core.Helpers.Enums;

namespace Nupco.EF.Repositories
{
    public class PicReleaseRepository : BaseRepository<PicRelease>, IPicReleaseRepository
    {
        private readonly ILogger _logger;
        private readonly PicRelease_Assembler _assembler;

        public PicReleaseRepository(ApplicationDbContext context, ILogger logger)
            : base(context, logger)
        {
            _logger = logger;
            _assembler = new PicRelease_Assembler();
        }

        #region Base CRUD Operations

        public async Task<OperationOutput> AddNewAsync(PicReleaseDto entity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;

                // Process owners - get or create them and collect their IDs
                if (entity.Owners != null && entity.Owners.Any())
                {
                    entity.OwnerIds = await ProcessOwners(entity.Owners);
                }

                var release = _assembler.WriteDal(entity);

                var addedRelease = await AddAsync(release);
                await _context.SaveChangesAsync();

                await UpdateReleaseRelationships(addedRelease.Id, entity.OwnerIds, entity.ModuleIds);
                await _context.SaveChangesAsync();

                // Reload the entity with relationships
                var fullEntity = await GetFullReleaseQuery()
                    .FirstOrDefaultAsync(r => r.Id == addedRelease.Id);

                var dto = _assembler.WriteDto(fullEntity);

                await transaction.CommitAsync();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dto, 1);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding new PicRelease");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        private async Task<List<int>> ProcessOwners(List<PicOwnerDto> ownerDtos)
        {
            var ownerIds = new List<int>();

            foreach (var ownerDto in ownerDtos)
            {
                // Check if owner exists by email
                var existingOwner = await _context.PicOwners
                    .FirstOrDefaultAsync(o => o.Email == ownerDto.Email);

                if (existingOwner != null)
                {
                    // Update existing owner details if needed
                    existingOwner.Name = ownerDto.Name;
                    existingOwner.Department = ownerDto.Department;
                    _context.PicOwners.Update(existingOwner);
                    ownerIds.Add(existingOwner.Id);
                }
                else
                {
                    // Create new owner
                    var newOwner = new PicOwner
                    {
                        Name = ownerDto.Name,
                        Email = ownerDto.Email,
                        Department = ownerDto.Department
                    };

                    _context.PicOwners.Add(newOwner);
                    await _context.SaveChangesAsync(); // Save to get the ID
                    ownerIds.Add(newOwner.Id);
                }
            }

            return ownerIds;
        }

        // The rest of the repository methods remain the same as in the previous implementation
        // [Include all other methods from the previous implementation here]

        #region Helper Methods

        private IQueryable<PicRelease> GetFullReleaseQuery()
        {
            return _context.PicReleases
                .Include(r => r.ReleaseModules)
                .ThenInclude(rm => rm.Module)
                .Include(r => r.ReleaseOwners)
                .ThenInclude(ro => ro.Owner)
                .Include(r => r.ReleaseDocuments)
                .Include(r => r.ScreenShots)
                .Include(r => r.TrainingDocuments)
                .Include(r => r.KnownIssues)
                .Include(r => r.RoadmapFeatures)
                .Where(r => r.IsDeleted == false);
        }

        private async Task UpdateReleaseRelationships(int releaseId, List<int> ownerIds, List<int> moduleIds)
        {
            // Update Owners
            if (ownerIds != null)
            {
                var existingOwners = _context.PicReleaseOwners.Where(ro => ro.PicReleaseId == releaseId);
                _context.PicReleaseOwners.RemoveRange(existingOwners);

                foreach (var ownerId in ownerIds.Distinct())
                {
                    _context.PicReleaseOwners.Add(new PicReleaseOwner
                    {
                        PicReleaseId = releaseId,
                        PicOwnerId = ownerId
                    });
                }
            }

            // Update Modules
            if (moduleIds != null)
            {
                var existingModules = _context.PicReleaseModules.Where(rm => rm.PicReleaseId == releaseId);
                _context.PicReleaseModules.RemoveRange(existingModules);

                foreach (var moduleId in moduleIds.Distinct())
                {
                    _context.PicReleaseModules.Add(new PicReleaseModule
                    {
                        PicReleaseId = releaseId,
                        PicModuleId = moduleId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        public async Task<OperationOutput> UpdateEntity(PicReleaseDto entity)
        {
            try
            {
                var existingEntity = await GetFullReleaseQuery()
                    .FirstOrDefaultAsync(r => r.Id == entity.Id && r.IsDeleted == false);


                if (entity.Owners != null && entity.Owners.Any())
                {
                    entity.OwnerIds = await ProcessOwners(entity.Owners);
                }

                if (existingEntity == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                // Use assembler to map from DTO to existing entity
                _assembler.WriteDal(entity, existingEntity);
                existingEntity.UpdatedDate = DateTime.Now;

                await UpdateReleaseRelationships(existingEntity.Id, entity.OwnerIds, entity.ModuleIds);

                Update(existingEntity);
                await SaveChangesAsync();

                // Reload the entity with relationships
                var updatedEntity = await GetFullReleaseQuery()
                    .FirstOrDefaultAsync(r => r.Id == entity.Id);

                var dto = _assembler.WriteDto(updatedEntity);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dto, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PicRelease with ID {Id}", entity.Id);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> DeleteEntity(int id, string userId)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                if (find == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                find.IsDeleted = true;
                find.DeletedBy = userId;
                find.DeletedDate = DateTime.Now;

                Update(find);
                await SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PicRelease with ID {Id}", id);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                if (find == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                find.IsActive = activate;
                Update(find);
                await SaveChangesAsync();

                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating activation status for PicRelease with ID {Id}", id);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        #endregion

        #region Get Operations

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {
                var entities = await GetFullReleaseQuery().ToListAsync();
                var dtos = _assembler.WriteListDto(entities);

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all PicReleases");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var entity = await GetFullReleaseQuery()
                    .FirstOrDefaultAsync(r => r.Id == id && r.IsDeleted == false);

                if (entity == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var dto = _assembler.WriteDto(entity);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dto, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PicRelease with ID {Id}", id);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy filter)
        {
            try
            {
                var releases = await GetFullReleaseQuery().Where(r => r.PicProductId == filter.PicProductId)
                    .Skip((int)filter.pageNumber * (int)filter.pageSize)
                    .Take((int)filter.pageSize).OrderByDescending(r => r.GoLiveDate ?? DateTime.MinValue)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(releases);
                var count = await GetFullReleaseQuery().Where(r => r.PicProductId == filter.PicProductId).CountAsync();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated PicReleases");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        #endregion

        #region Specialized Queries

        public async Task<OperationOutput> GetUpcomingReleasesAsync(int productId)
        {
            try
            {
                var now = DateTime.Now;
                var entities = await GetFullReleaseQuery()
                    .Where(r => r.IsActive == true &&
                                r.PlannedWindowStart > now &&
                                r.PicProductId == productId)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(entities);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming releases for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetArchivedReleasesAsync(int productId)
        {
            try
            {
                var now = DateTime.Now;
                var entities = await GetFullReleaseQuery()
                    .Where(r => r.PlannedWindowEnd < now &&
                                r.PicProductId == productId)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(entities);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived releases for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetProductRoadMapAsync(int productId)
        {
            try
            {
                var now = DateTime.Now;
                var query = GetFullReleaseQuery().Where(r => r.PicProductId == productId);

                // Current releases
                var current = await query
                    .Where(r => r.PlannedWindowStart <= now && r.PlannedWindowEnd >= now)
                    .ToListAsync();

                // Upcoming releases
                var upcoming = await query
                    .Where(r => r.PlannedWindowStart > now)
                    .ToListAsync();

                // Archived releases
                var archived = await query
                    .Where(r => r.PlannedWindowEnd < now)
                    .ToListAsync();

                var roadMap = new
                {
                    Current = _assembler.WriteListDto(current),
                    Upcoming = _assembler.WriteListDto(upcoming),
                    Archived = _assembler.WriteListDto(archived)
                };

                var totalCount = roadMap.Current.Count + roadMap.Upcoming.Count + roadMap.Archived.Count;
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(roadMap, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roadmap for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetFilteredReleasesAsync(
    int productId,
    DateTime? startDate,
    DateTime? endDate,
    int? moduleId,
    int? ownerId,
    int? status,
    int? impact,
    int? type,
    string? keyword)
        {
            try
            {
                var query = GetFullReleaseQuery().Where(r => r.PicProductId == productId && r.IsApproved == true);

                // 🕓 Date filters
                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(r =>
                        (r.PlannedWindowStart >= startDate && r.PlannedWindowStart <= endDate) ||
                        (r.PlannedWindowEnd >= startDate && r.PlannedWindowEnd <= endDate) ||
                        (r.GoLiveDate >= startDate && r.GoLiveDate <= endDate));
                }

                // 📦 Module filter
                if (moduleId.HasValue)
                {
                    query = query.Where(r => r.ReleaseModules.Any(m => m.PicModuleId == moduleId));
                }

                // 👤 Owner filter
                if (ownerId.HasValue)
                {
                    query = query.Where(r => r.ReleaseOwners.Any(o => o.PicOwnerId == ownerId));
                }

                // 🧩 Status filter
                if (status.HasValue)
                {
                    query = query.Where(r => (int)r.Status == status.Value);
                }

                // 💥 Impact filter
                if (impact.HasValue)
                {
                    query = query.Where(r => (int)r.Impact == impact.Value);
                }

                // 🧪 Type filter
                if (type.HasValue)
                {
                    query = query.Where(r => (int)r.Type == type.Value);
                }

                // 🔍 Keyword search
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    query = query.Where(r =>
                        r.NameEn.ToLower().Contains(keyword) ||
                        r.ReleaseId.ToLower().Contains(keyword) ||
                        r.IntroductionEn.ToLower().Contains(keyword) ||
                        r.BusinessImpactSummaryEn.ToLower().Contains(keyword) ||
                        r.ReleaseModules.Any(m => m.Module.Name.ToLower().Contains(keyword)));
                }

                // 📋 Fetch and map
                var entities = await query
                    .OrderByDescending(r => r.GoLiveDate ?? DateTime.MinValue)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(entities);

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered releases for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> GetFilteredArchivedReleasesAsync(
   int productId,
   int? moduleId,
   int? ownerId,
   int? impact,
   int? type,
   string? keyword)
        {
            try
            {
                var query = GetFullReleaseQuery().Where(r => r.PicProductId == productId && r.IsApproved == true && 
                r.GoLiveDate < DateTime.Now);

                
                // 📦 Module filter
                if (moduleId.HasValue)
                {
                    query = query.Where(r => r.ReleaseModules.Any(m => m.PicModuleId == moduleId));
                }

                // 👤 Owner filter
                if (ownerId.HasValue)
                {
                    query = query.Where(r => r.ReleaseOwners.Any(o => o.PicOwnerId == ownerId));
                }

               

                // 💥 Impact filter
                if (impact.HasValue)
                {
                    query = query.Where(r => (int)r.Impact == impact.Value);
                }

                // 🧪 Type filter
                if (type.HasValue)
                {
                    query = query.Where(r => (int)r.Type == type.Value);
                }

                // 🔍 Keyword search
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower();
                    query = query.Where(r =>
                        r.NameEn.ToLower().Contains(keyword) ||
                        r.ReleaseId.ToLower().Contains(keyword) ||
                        r.IntroductionEn.ToLower().Contains(keyword) ||
                        r.BusinessImpactSummaryEn.ToLower().Contains(keyword) ||
                        r.ReleaseModules.Any(m => m.Module.Name.ToLower().Contains(keyword)));
                }

                // 📋 Fetch and map
                var entities = await query
                    .OrderByDescending(r => r.GoLiveDate ?? DateTime.MinValue)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(entities);

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered releases for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }



        public async Task<OperationOutput> GetAllOwnersAsync()
        {
            try
            {
                var owners = await _context.Set<PicOwner>()
                    .AsNoTracking()
                    .ToListAsync();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(owners, owners.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all owners");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllModulesAsync()
        {
            try
            {
                var modules = await _context.Set<PicModule>()
                    .AsNoTracking()
                    .ToListAsync();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(modules, modules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all modules");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        #endregion

        #region Helper Methods

        //private IQueryable<PicRelease> GetFullReleaseQuery()
        //{
        //    return _context.PicReleases
        //        .Include(r => r.ReleaseModules)
        //        .ThenInclude(rm => rm.Module)
        //        .Include(r => r.ReleaseOwners)
        //        .ThenInclude(ro => ro.Owner)
        //        .Where(r => r.IsDeleted == false);
        //}

        //private async Task UpdateReleaseRelationships(int releaseId, List<int> ownerIds, List<int> moduleIds)
        //{
        //    // Update Owners
        //    if (ownerIds != null)
        //    {
        //        var existingOwners = _context.PicReleaseOwners.Where(ro => ro.PicReleaseId == releaseId);
        //        _context.PicReleaseOwners.RemoveRange(existingOwners);

        //        foreach (var ownerId in ownerIds.Distinct())
        //        {
        //            _context.PicReleaseOwners.Add(new PicReleaseOwner
        //            {
        //                PicReleaseId = releaseId,
        //                PicOwnerId = ownerId
        //            });
        //        }
        //    }

        //    // Update Modules
        //    if (moduleIds != null)
        //    {
        //        var existingModules = _context.PicReleaseModules.Where(rm => rm.PicReleaseId == releaseId);
        //        _context.PicReleaseModules.RemoveRange(existingModules);

        //        foreach (var moduleId in moduleIds.Distinct())
        //        {
        //            _context.PicReleaseModules.Add(new PicReleaseModule
        //            {
        //                PicReleaseId = releaseId,
        //                PicModuleId = moduleId
        //            });
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //}

        #endregion


        public async Task<OperationOutput> GetReleasesTimelineAsync(
      int productId,                // 🔹 Make ProductId required
      int? year = null,
      int? moduleId = null,
      int? typeId = null)
        {
            try
            {
                // 🔹 Require productId
                if (productId <= 0)
                    return ResultOutputData.GenerateResultOutputFailure("ProductId is required.");

                // 🔹 Base query
                var query = GetFullReleaseQuery()
                    .Where(r => r.PicProductId == productId && (bool)r.IsActive && !(bool)r.IsDeleted && r.IsApproved == true);

                // 🔹 Optional filters
                if (year.HasValue)
                    query = query.Where(r => r.GoLiveDate.HasValue && r.GoLiveDate.Value.Year == year.Value);

                if (moduleId.HasValue)
                    query = query.Where(r => r.ReleaseModules.Any(m => m.PicModuleId == moduleId));

                if (typeId.HasValue)
                    query = query.Where(r => r.Type == (ReleaseType)typeId);

                // 🔹 Project releases with full details
                var releases = await query
                    .Where(r => r.GoLiveDate.HasValue)
                    .Select(r => new
                    {
                        r.Id,
                        r.PicProductId,
                        ProductName = r.PicProduct.NameEn,
                        r.ReleaseId,
                        r.NameEn,
                        r.NameAr,
                        r.GoLiveDate,
                        r.PlannedWindowStart,
                        r.PlannedWindowEnd,
                        r.Impact,
                        r.Type,
                        r.Status,
                        r.RiskLevel,
                        r.BusinessPainGainEn,
                        r.BusinessPainGainAr,
                        r.ReleaseScopeEn,
                        r.ReleaseScopeAr,
                        r.BusinessImpactSummaryEn,
                        r.BusinessImpactSummaryAr,
                        r.AffectedUsers,
                        r.DowntimeExpected,
                        r.DowntimeWindow,
                        r.ReleaseNotesUrl,
                        r.TrainingPackUrl,
                        r.IntroductionEn,
                        r.IntroductionAr,

                        // Related entities
                        Owners = r.ReleaseOwners.Select(o => new
                        {
                            o.Id,
                            o.Owner.Name,
                        }),

                        Modules = r.ReleaseModules.Select(m => new
                        {
                            m.PicModuleId,
                            ModuleName = m.Module.Name
                        }),

                        Documents = r.ReleaseDocuments.Select(d => new
                        {
                            d.Id,
                            d.FileName,
                        }),

                        TrainingDocs = r.TrainingDocuments.Select(d => new
                        {
                            d.Id,
                            d.FileName,
                        }),

                        KnownIssues = r.KnownIssues.Select(k => new
                        {
                            k.Id,
                            k.TitleEn,
                            k.TitleAr,
                            k.DescriptionEn,
                            k.DescriptionAr,
                            k.WorkaroundEn,
                            k.WorkaroundAr,
                            k.Status
                        })
                    }).OrderByDescending(r => r.GoLiveDate ?? DateTime.MinValue)
                    .ToListAsync();

                // 🔹 Group by Year + Quarter
                var grouped = releases
                    .GroupBy(r => new
                    {
                        Year = r.GoLiveDate.Value.Year,
                        Quarter = $"Q{((r.GoLiveDate.Value.Month - 1) / 3) + 1}"
                    })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Quarter)
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Quarter,
                        Releases = g.OrderBy(r => r.GoLiveDate)
                    })
                    .ToList();

                // ✅ Wrap in standard output
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(grouped, grouped.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timeline releases");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> GetLatestReleasesAsync(int productId, int count = 3)
        {
            try
            {
                var releases = await GetFullReleaseQuery()
                    .Where(r => r.PicProductId == productId && (bool)r.IsActive && !(bool)r.IsDeleted)
                    .OrderByDescending(r => r.GoLiveDate)
                    .Take(count)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(releases);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest releases for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }
        public async Task<OperationOutput> SearchReleasesAsync(string keyword)
        {
            try
            {
                keyword = keyword?.Trim().ToLower();
                var releases = await GetFullReleaseQuery()
                    .Where(r =>
                        r.NameEn.ToLower().Contains(keyword) ||
                        r.ReleaseId.ToLower().Contains(keyword) ||
                        r.IntroductionEn.ToLower().Contains(keyword) ||
                        r.BusinessImpactSummaryEn.ToLower().Contains(keyword) ||
                        r.ReleaseModules.Any(m => m.Module.Name.ToLower().Contains(keyword)))
                    .OrderByDescending(r => r.GoLiveDate)
                    .ToListAsync();

                var dtos = _assembler.WriteListDto(releases);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching releases with keyword {Keyword}", keyword);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetKnownIssuesByReleaseAsync(int releaseId)
        {
            try
            {
                var issues = await _context.ReleaseKnownIssues
                    .Where(i => i.PicReleaseId == releaseId)
                    .ToListAsync();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(issues, issues.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting known issues for release ID {ReleaseId}", releaseId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> AddReleaseDocs(ReleaseDocumentDtos docs)
        {
            try
            {
                if (docs.ReleaseId != 0)
                {
                    // 🗑️ Step 1: Delete old documents for this release
                    var oldDocs = await _context.ReleaseDocuments
                        .Where(x => x.ReleaseDocumentPicReleaseId == docs.ReleaseId
                                 || x.TrainingDocumentPicReleaseId == docs.ReleaseId)
                        .ToListAsync();

                    if (oldDocs.Any())
                    {
                        _context.ReleaseDocuments.RemoveRange(oldDocs);
                        await _context.SaveChangesAsync();
                    }

                    // 🆕 Step 2: Add new Release Documents
                    if (docs.ReleaseDocuments != null && docs.ReleaseDocuments.Any())
                    {
                        var newReleaseDocs = _assembler.WriteDocsListDal(docs.ReleaseDocuments);

                        foreach (var doc in newReleaseDocs)
                        {
                            doc.Id = 0;
                            doc.ReleaseDocumentPicReleaseId = docs.ReleaseId;
                        }

                        await _context.ReleaseDocuments.AddRangeAsync(newReleaseDocs);
                    }

                    // 🆕 Step 3: Add new Training Documents
                    if (docs.TrainingDocuments != null && docs.TrainingDocuments.Any())
                    {
                        var newTrainingDocs = _assembler.WriteDocsListDal(docs.TrainingDocuments);

                        foreach (var doc in newTrainingDocs)
                        {
                            doc.Id = 0;
                            doc.TrainingDocumentPicReleaseId = docs.ReleaseId;
                        }

                        await _context.ReleaseDocuments.AddRangeAsync(newTrainingDocs);
                    }

                    if (docs.ScreenShots != null && docs.ScreenShots.Any())
                    {
                        var screenShotDocs = _assembler.WriteDocsListDal(docs.ScreenShots);

                        foreach (var doc in screenShotDocs)
                        {
                            doc.Id = 0;
                            doc.ScreenShotPicReleaseId = docs.ReleaseId;
                        }

                        await _context.ReleaseDocuments.AddRangeAsync(screenShotDocs);
                    }

                    // 💾 Step 4: Save all changes
                    await _context.SaveChangesAsync();
                }

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(docs, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving documents for release ID {ReleaseId}", docs.ReleaseId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetReleaseSummary(int productId)
        {
            try
            {
                var releases = await _context.PicReleases
                    .Include(r => r.ReleaseModules)
                    .Where(r => r.PicProductId == productId && (r.IsDeleted == null || r.IsDeleted == false))
                    .ToListAsync();

                // 🟢 1. Delivered releases count (Live)
                int deliveredCount = releases.Count(r => r.Status == ReleaseStatus.Live);

                // 🟢 2. Impacted modules (count of unique modules across all releases)
                int impactedModulesCount = releases
                    .Where(r => r.ReleaseModules != null)
                    .SelectMany(r => r.ReleaseModules.Select(m => m.PicModuleId))
                    .Distinct()
                    .Count();

                // 🟢 Optional: total releases
                int totalReleases = releases.Count;

                // 🟢 Build the summary object
                var summary = new
                {
                    ProductId = productId,
                    TotalReleases = totalReleases,
                    DeliveredReleases = deliveredCount,
                    ImpactedModules = impactedModulesCount
                };

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(summary, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting release summary for product ID {ProductId}", productId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


        public async Task<OperationOutput> ApproveEntity(int id, string userId , bool? approve = true)
        {
            try
            {
                var find = await FindAsync(f => f.Id == id);
                if (find == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                find.IsApproved = approve;
                find.ApprovedBy = userId;
                find.UpdatedDate = DateTime.Now;

                Update(find);


                var users = await _context.GroupUsers
                    .Where(r => r.GroupId == find.NotificationGroupId ).Include(r => r.User)
                    .ToListAsync();

               

                await SaveChangesAsync(); 
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(users, 1);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PicRelease with ID {Id}", id);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


    }
}