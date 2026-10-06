using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.OutSourceEmployees;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.Core.Models.OutSource_Employees;
using Nupco.DAL;
using Nupco.DAL.Models;
using Nupco.DAL.Models.OutSourceEmployees;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    public class CompanyRepository : BaseRepository<DAL.Models.OutSourceEmployees.Company>, ICompanyRepository
    {
        private readonly Company_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "CompanyData";


        public CompanyRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new Company_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewCompany(CompanyDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entityDto);


                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);

                var _entityDto = _Assembler.WriteDto(_entity);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewAsync(CompanyDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;


                var _model = _Assembler.WriteDal(entityDto);

                var _entity = await AddAsync(_model);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);
                var _entityDto = _Assembler.WriteDto(_entity);


                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entityDto, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<CompanyDto> entitiesDto)
        {
            try
            {
                var _entities = _Assembler.WriteListDal(entitiesDto);
                var _entitiesDto = await AddRangeAsync(_entities);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(_entitiesDto, 1);
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


        public async Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            try
            {
                var Companys = await FindAllAsync(i => i.IsDeleted == false);
                var _Companys = _Assembler.WriteListDto(Companys);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Companys, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(CompanyDto entityDto)
        {
            try
            {

                var _entity = _Assembler.WriteDal(entityDto);

                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = Update(_entity);
                await SaveChangesAsync();

                _cache.Remove(cacheKey);


                var objDto = _Assembler.WriteDto(_entity);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(objDto, 1);

                return _result;

            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> UpdateEntityAsync(CompanyDto entityDto, object key)
        {
            try
            {
                var _Company = await GetByIdAsync((int)entityDto.Id);


                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _Company);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<CompanyDto> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    var find = await FindAsync(f => f.Id == item.Id);
                    find.IsDeleted = true;
                    Update(find);
                }

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





        public async Task<OperationOutput> GetAllByPagenationAsync(FiltersBy _filter)
        {
            try
            {
                string[] stringArray = { "CreatedByUser" };
                var Companys = new List<DAL.Models.OutSourceEmployees.Company>();

                var _CompanyList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                Companys = _CompanyList.Where(i => i.IsDeleted == false).ToList();

                var _CompanysDto = _Assembler.WriteListDto(Companys);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_CompanysDto, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> GetDataByIdAsync(int id)
        {
            try
            {

                var _Company = await FindAsync(f => f.Id == id);
                if (_Company is not null)
                {
                    var _entity = _Assembler.WriteDto(_Company);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_entity, 1);
                    return _result;
                }
                else
                {
                    var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                    return Result;
                }
            }
            catch (Exception ex)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }

        public async Task<OperationOutput> GetCompanyWithEmployees(int companyId)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Employees)
                    .FirstOrDefaultAsync(c => c.Id == companyId);

                if (company == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                var _entity = _Assembler.WriteDtoWithEmployees(company);

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(_entity, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company with employees");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetCompaniesByOutSourceEmployee(int outSourceEmployeeId)
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => c.OutSourceEmployees.Any(e => e.Id == outSourceEmployeeId))
                    .ToListAsync();

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(companies, companies.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies by outsource employee");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetPrimaryContact(int companyId)
        {
            try
            {
                var contact = await _context.CompanyEmployees
                    .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.IsPrimaryContact);

                if (contact == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(contact, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary contact");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetEmployeesByPosition(int companyId, string position)
        {
            try
            {
                var employees = await _context.CompanyEmployees
                    .Where(e => e.CompanyId == companyId && e.Role == position)
                    .ToListAsync();

                ResultOutputData result = new ResultOutputData();
                return result.GenearetResultOutput(employees, employees.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by position");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }


    }
}
