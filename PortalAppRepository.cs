using Core.Helpers;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.PortalApps;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nupco.EF.Repositories
{
    internal class PortalAppRepository : BaseRepository<PortalApp>, IPortalAppRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly PortalApp_Assembler _assembler;

        public PortalAppRepository(ApplicationDbContext context, ILogger logger)
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
            _assembler = new PortalApp_Assembler();
        }

        public async Task<OperationOutput> GetAllDataAsync()
        {
            try
            {
                var entities = await FindAllAsync(i => i.IsDeleted == false);
                var dtos = _assembler.WriteListDto(entities);
                var count = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(dtos, count);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity != null && entity.IsDeleted == false)
                {
                    var dto = _assembler.WriteDto(entity);
                    ResultOutputData result = new ResultOutputData();
                    return result.GenearetResultOutput(dto, 1);
                }
                return ResultOutputData.GenearetResultOutputNoDataReturned();
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                var entities = await FindAllPagenationAsync(
                    i => i.IsDeleted == false,
                    null,
                    (int)_filter.pageNumber * (int)_filter.pageSize,
                    (int)_filter.pageSize);
                var dtos = _assembler.WriteListDto(entities);
                var count = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(dtos, count);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> AddNewAsync(PortalAppDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var model = _assembler.WriteDal(entity);
                var added = await AddAsync(model);
                await _context.SaveChangesAsync();
                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(added, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> UpdateEntity(PortalAppDto entity)
        {
            try
            {
                var existing = await FindAsync(i => i.Id == entity.Id);
                if (existing is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                existing.AppNameAr = entity.AppNameAr;
                existing.AppNameEn = entity.AppNameEn;
                existing.AppLogo = entity.AppLogo;
                existing.AppUrl = entity.AppUrl;
                existing.AdGroups = entity.AdGroups;
                existing.IsActive = entity.IsActive;
                existing.UpdatedDate = DateTime.Now;
                existing.UpdatedBy = entity.UpdatedBy;

                var updated = Update(existing);
                await SaveChangesAsync();
                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(updated, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> DeleteEntity(int id, string userId = null)
        {
            try
            {
                var entity = await FindAsync(f => f.Id == id);
                if (entity is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                entity.IsDeleted = true;
                entity.DeletedDate = DateTime.Now;
                entity.DeletedBy = userId;
                Update(entity);
                await SaveChangesAsync();
                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> ActivatedOrUnActivated(int id, bool activate)
        {
            try
            {
                var entity = await FindAsync(f => f.Id == id);
                if (entity is null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                entity.IsActive = activate;
                Update(entity);
                await SaveChangesAsync();
                return ResultOutputData.GenearetResultOutputSuccess();
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public Task<OperationOutput> UpdateEntityAsync(PortalAppDto t, object key) => throw new NotImplementedException();
        public Task<OperationOutput> GetAllByUserIdAsync(string userId) => throw new NotImplementedException();
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<PortalAppDto> entities) => throw new NotImplementedException();
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<PortalAppDto> entities) => throw new NotImplementedException();
    }
}
