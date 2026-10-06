using Core.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
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

namespace Nupco.EF.Repositories
{
    public class SliderRepository : BaseRepository<Slider>, ISliderRepository
    {
        private readonly Slider_Assembler _Assembler;
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "SliderData";


        public SliderRepository(ApplicationDbContext context, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _Assembler = new Slider_Assembler();
            _logger = logger;
            _context = context;
            _cache = cache;


        }

        public async Task<OperationOutput> CreateNewSlider(SliderDto entityDto)
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

        public async Task<OperationOutput> AddNewAsync(SliderDto entityDto)
        {
            try
            {
                entityDto.IsActive = true;
                entityDto.IsDeleted = false;
                entityDto.CreatedDate = DateTime.Now;

                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }
                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64En))
                {
                    entityDto.OriginalPicEn = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64En) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64En, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPicEn;
                }
                var _model = _Assembler.WriteDal(entityDto);
                _model.OriginalPic = entityDto.OriginalPic;

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

        public async Task<OperationOutput> AddNewRangeAsync(IEnumerable<SliderDto> entitiesDto)
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
                var Sliders = await FindAllAsync(i => i.IsDeleted == false);
                var _Sliders = _Assembler.WriteListDto(Sliders);

                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Sliders, 1);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        public async Task<OperationOutput> UpdateEntity(SliderDto entityDto)
        {
            try
            {
                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64))
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }

                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64En))
                {
                    entityDto.OriginalPicEn = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64En) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64En, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPicEn;
                }

                var _entity = _Assembler.WriteDal(entityDto);
                _entity.OriginalPic = entityDto.OriginalPic;
                _entity.OriginalPicEn = entityDto.OriginalPicEn;
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

        public async Task<OperationOutput> UpdateEntityAsync(SliderDto entityDto, object key)
        {
            try
            {
                var _Slider = await GetByIdAsync(entityDto.Id.Value);
                if (entityDto.IsActive == true)
                {
                    entityDto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPic;
                }
                if (!String.IsNullOrEmpty(entityDto.OriginalPicBase64En))
                {
                    entityDto.OriginalPicEn = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entityDto.OriginalPicBase64En) ?
                            Images.SaveSingleImageOnServer(entityDto.OriginalPicBase64En, 1024, entityDto.pathToSave, false, 0, entityDto.pathToSave) : entityDto.OriginalPicEn;
                }

                var _entity = _Assembler.WriteDal(entityDto);
                _entity.IsActive = true;
                _entity.IsDeleted = false;
                _entity.CreatedDate = DateTime.Now;
                var entity = UpdateAsync(_entity, _Slider);
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

        public async Task<OperationOutput> DeleteEntityRange(IEnumerable<SliderDto> entities)
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
                var Sliders = new List<Slider>();

                var _SliderList = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber, (int)_filter.pageSize, stringArray);
                Sliders = _SliderList.Where(i => i.IsDeleted == false).ToList();

                var _SlidersDto = _Assembler.WriteListDto(Sliders);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_SlidersDto, counts);
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
                string[] stringArray = { "CreatedByUser" };

                var _Slider = await FindAsync(f => f.Id == id, stringArray);
                if (_Slider is not null)
                {
                    var _entity = _Assembler.WriteDto(_Slider);
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


    }
}
