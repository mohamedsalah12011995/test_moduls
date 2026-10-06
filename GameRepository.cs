using Core.Helpers;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto.Game;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;

namespace Nupco.EF.Repositories
{
    internal class GameRepository : BaseRepository<Game>, IGameRepository
    {
        private  readonly ApplicationDbContext _context;
        private  readonly ILogger _logger;
        private readonly Game_Assembler _Game_Assembler;
        private string pathToSave = "";

        private string[] stringArray = { "Entity", "CreatedByUser", "UpdatedByUser", "DeletedByUser" };


        public GameRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _Game_Assembler = new Game_Assembler();
            _logger = logger;
            _context = context;

            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            var folderName = "Images/";
            pathToSave = Path.Combine(sharedPath, folderName);

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

        public async Task<OperationOutput> AddNewAsync(GameDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
              Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : entity.OriginalPic;

                var _model = _Game_Assembler.WriteDal(entity);
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

        public async Task<OperationOutput> DeleteEntity(int id,string userId)
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
                var spec = Specification<Game>.All.And(new GameSpecification(_filter));

                var Game = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize,stringArray);
                var _Game = _Game_Assembler.WriteListDto(Game);
                var counts = await CountAsync(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_Game, counts);
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

                var entities = await FindAllAsync(i => i.IsDeleted == false&&i.IsActive!=false, stringArray);
                var _entities = _Game_Assembler.WriteListDto(entities);

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
                var Game = await GetByIdAsync(id);
                if (Game != null)
                {
                    var _Game = _Game_Assembler.WriteDto(Game);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_Game, 1);
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

        public async Task<OperationOutput> UpdateEntity(GameDto entity)
        {
            try
            {


                var _entity = await FindAsync(i => i.Id == entity.Id);

                if (!string.IsNullOrEmpty(entity.OriginalPicBase64))
                {
                    _entity.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(entity.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(entity.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : entity.OriginalPic;
                }

                if (_entity is not null)
                {
                    _entity.IsActive = true;
                    _entity.IsDeleted = false;
                    _entity.NameAr = entity.NameAr;
                    _entity.NameEn = entity.NameEn;
                    _entity.DescriptionAr = entity.DescriptionAr;
                    _entity.DescriptionEn = entity.DescriptionEn;
                    _entity.UpdatedBy = entity.UpdatedBy;
                    _entity.UpdatedDate = entity.UpdatedDate;
                    _entity.EntityId = entity.EntityId;

                    var _model =  Update(_entity);
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


        /// <summary>  /// </summary>

        public Task<OperationOutput> UpdateEntityAsync(GameDto t, object key)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> GetAllByUserIdAsync(string userId)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> DeleteEntityRange(IEnumerable<GameDto> entities)
        {
            throw new NotImplementedException();
        }
        public Task<OperationOutput> AddNewRangeAsync(IEnumerable<GameDto> entities)
        {
            throw new NotImplementedException();
        }
    }
}
