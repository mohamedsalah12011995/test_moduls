using Core.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
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
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Core.Helpers.Enums;
using static Nupco.Core.Helpers.JWTHelper;
using Comment = Nupco.DAL.Models.Comment;
using RequestStatus = Nupco.DAL.Models.RequestStatus;

namespace Nupco.EF.Repositories
{
    public class FeatureRequestRepository : BaseRepository<FeatureRequest>, IFeatureRequestRepository
    {
        private readonly IConfiguration _configuration;
        private readonly FeatureRequest_Assembler _Assembler;

        public FeatureRequestRepository(ApplicationDbContext context, ILogger logger) : base(context, logger)
        {
            _logger = logger;
            _context = context;
            _Assembler = new FeatureRequest_Assembler();
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

        public async Task<OperationOutput> AddNewAsync(FeatureRequestDto entity)
        {
            try
            {
                entity.IsActive = true;
                entity.IsDeleted = false;
                entity.CreatedDate = DateTime.Now;
                var _model = _Assembler.WriteDal(entity);
                var _entity = await AddAsync(_model);
                await _context.SaveChangesAsync();

                UpdateFeatreRequestsRelationships(_entity.Id, entity.ExpectedValues!);


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

        private async Task UpdateFeatreRequestsRelationships(int featureRequestId, List<int> ExpectedValsIds)
        {
            // Update Owners

            try
            {
                if (ExpectedValsIds != null)
                {
                    var existingPicExpectedValues = _context.PicExpectedValuesFeatureRequests.Where(ro => ro.FeatureRequestId == featureRequestId);
                    _context.PicExpectedValuesFeatureRequests.RemoveRange(existingPicExpectedValues);

                    foreach (var EId in ExpectedValsIds.Distinct())
                    {
                        _context.PicExpectedValuesFeatureRequests.Add(new PicExpectedValuesFeatureRequest
                        {
                            FeatureRequestId = featureRequestId,
                            ExpectedValueId = EId
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
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
                var FeatureRequests = new List<FeatureRequest>();
                var _FeatureRequests = await GetFullFeatureQuery().Where(f => f.PicProductId == _filter.PicProductId || _filter.PicProductId == null)
                .Skip((int)_filter.pageNumber * (int)_filter.pageSize)
                .Take((int)_filter.pageSize)
                .ToListAsync();
               // var _FeatureRequests = await FindAllAsync(i => i.IsDeleted == false, (int)_filter.pageNumber * (int)_filter.pageSize, (int)_filter.pageSize, "PicExpectedValuesFeatureRequests");
                FeatureRequests = _FeatureRequests.Where(i => i.IsDeleted == false).ToList();
                var _FeatureRequestsDto = _Assembler.WriteListDto(FeatureRequests);
                var counts = Count(i => i.IsDeleted == false);
                ResultOutputData result = new ResultOutputData();
                var _result = result.GenearetResultOutput(_FeatureRequestsDto, counts);
                return _result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }

        private IQueryable<FeatureRequest> GetFullFeatureQuery()
        {
            return _context.FeatureRequests
                .Include(r => r.Module).Include(r => r.ExpectedValues)
                .Where(r => r.IsDeleted == false);
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
                //var FeatureRequest = await FindAsync(f => f.Id == id);
                var query = GetFullRequestQuery();
                var FeatureRequest = await query.FirstOrDefaultAsync(f => f.Id == id);
                if (FeatureRequest != null)
                {
                    var _FeatureRequest = _Assembler.WriteDto(FeatureRequest);
                    ResultOutputData result = new ResultOutputData();
                    var _result = result.GenearetResultOutput(_FeatureRequest, 1);
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

        public async Task<OperationOutput> UpdateEntity(FeatureRequestDto entity)
        {
            try
            {

                var _entity = await FindAsync(i => i.Id == entity.Id);
                if (_entity is not null)
                {
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
        public async Task<OperationOutput> GetRequestsByFilterAsync(FeatureRequestFilter filter)
        {
            try
            {
                var query = GetFullRequestQuery();


                if (filter.PicProductId != null )
                {
                    query = query.Where(r => filter.PicProductId == r.PicProductId);
                }
                // Apply filters
                if (filter.ModuleIds != null && filter.ModuleIds.Any())
                {
                    query = query.Where(r => filter.ModuleIds.Contains(r.PicModuleId.Value));
                }

                if (filter.Statuses != null && filter.Statuses.Any())
                {
                    query = query.Where(r => filter.Statuses.Contains((DAL.Models.RequestStatus)r.Status));
                }

                if (filter.Categories != null && filter.Categories.Any())
                {
                    query = query.Where(r => filter.Categories.Contains((RequestCategory)r.Category));
                }

                //if (filter.ExpectedValues != null && filter.ExpectedValues.Any())
                //{
                //    query = query.Where(r => filter.ExpectedValues.Contains((ExpectedValue)r.ExpectedValue));
                //}

                if (!string.IsNullOrEmpty(filter.AssignedToUserId))
                {
                    query = query.Where(r => r.AssignedToUserId == filter.AssignedToUserId);
                }

                if (!string.IsNullOrEmpty(filter.SearchText))
                {
                    query = query.Where(r =>
                        r.Title.Contains(filter.SearchText) ||
                        r.Description.Contains(filter.SearchText) ||
                        r.BusinessContext.Contains(filter.SearchText));
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(r => r.SubmissionDate >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(r => r.SubmissionDate <= filter.ToDate.Value);
                }

                var requests = await query
                    .OrderByDescending(r => r.SubmissionDate)
                    .ToListAsync();

                var dtos = _Assembler.WriteListDto(requests);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dtos, dtos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature requests by filter");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> BulkUploadAsync(Stream fileStream, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Read Excel file using a library like EPPlus or ClosedXML
                // This is a simplified implementation - you'll need to implement Excel reading
                var requests = await ReadRequestsFromExcel(fileStream, userId);

                foreach (var request in requests)
                {
                    // Generate Request ID
                    request.RequestId = GenerateRequestId(request);
                    request.SubmissionDate = DateTime.Now;
                    request.Status = DAL.Models.RequestStatus.Received;

                    await AddAsync(request);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput($"Successfully imported {requests.Count} requests", requests.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error bulk uploading feature requests");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> UpdateRequestStatusAsync(int requestId, DAL.Models.RequestStatus status, string notes, string userId)
        {
            try
            {
                var request = await GetFullRequestQuery()
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                    return ResultOutputData.GenearetResultOutputNoDataReturned();

                request.Status = status;
                request.ResolutionNotes = notes;
                request.ReviewedDate = DateTime.Now;
                request.AssignedToUserId = userId;

                Update(request);
                await SaveChangesAsync();

                var dto = _Assembler.WriteDto(request);
                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(dto, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for request ID {RequestId}", requestId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetRequestStatisticsAsync()
        {
            try
            {
                var query = GetFullRequestQuery();

                var statistics = new
                {
                    TotalRequests = await query.CountAsync(),
                    ByStatus = await query
                        .GroupBy(r => r.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    ByCategory = await query
                        .GroupBy(r => r.Category)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    ByModule = await query
                        .GroupBy(r => r.Module.Name)
                        .Select(g => new { Module = g.Key, Count = g.Count() })
                        .ToListAsync(),
                    RecentRequests = await query
                        .OrderByDescending(r => r.SubmissionDate)
                        .Take(10)
                        .Select(r => new { r.Id, r.Title, r.Status, r.SubmissionDate })
                        .ToListAsync()
                };

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(statistics, statistics.TotalRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature request statistics");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetFilteredFeatureRequestsAsync(
    string? keyword = null,
    int? moduleId = null,
    RequestStatus? status = null,
    RequestCategory? category = null,
    int pageNumber = 1,
    int pageSize = 10)
        {
            try
            {
                var query = GetFullRequestQuery();

                // ✅ Apply filters dynamically
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(r =>
                        (r.Title != null && r.Title.Contains(keyword)) ||
                        (r.Description != null && r.Description.Contains(keyword)) ||
                        (r.BusinessContext != null && r.BusinessContext.Contains(keyword)));
                }

                if (moduleId.HasValue)
                {
                    query = query.Where(r => r.PicModuleId == moduleId.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(r => r.Status == status.Value);
                }

                if (category.HasValue)
                {
                    query = query.Where(r => r.Category == category.Value);
                }

                // ✅ Get total count before pagination
                var totalCount = await query.CountAsync();

                // ✅ Apply pagination
                var pagedData = await query
                    .OrderByDescending(r => r.SubmissionDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // ✅ Map to DTOs
                var dtos = _Assembler.WriteListDto(pagedData);

                // ✅ Wrap in OperationOutput
                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(dtos, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering Feature Requests");
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        public async Task<OperationOutput> GetRequestStatusSummaryAsync()
        {
            try
            {
                var query = GetFullRequestQuery();

                // ✅ Group by status and count
                var statusCounts = await query
                    .GroupBy(r => r.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                // ✅ Compute total
                var total = statusCounts.Sum(x => x.Count);

                // ✅ Include "All" in the summary
                var summary = new List<object>
        {
            new { Status = "All", Count = total }
        };

                summary.AddRange(statusCounts);

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(summary, summary.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request status summary");
                return ResultOutputData.GenearetResultOutputCatch();
            }

        }

        public async Task<OperationOutput> SubmitProductOwnerReviewAsync(ProductOwnerReviewDto dto, string userId)
        {
            var entity = await _context.FeatureRequests
                .FirstOrDefaultAsync(f => f.Id == dto.FeatureRequestId);

            if (entity == null)
                throw new Exception("Feature Request not found");

            // Update fields
            entity.AssignedToUserId = dto.AssignedToUserId;
            entity.ReviewedDate = dto.ReviewedDate;
            entity.ResolutionNotes = dto.ResolutionNotes;
            entity.Tags = dto.Tags;
            entity.RequestFeedback = dto.RequestFeedback;
            entity.Status = dto.Status;
            entity.UpdatedBy = userId;
            entity.UpdatedDate = DateTime.UtcNow;

            // If attachments included
            //if (dto.PMAttachments != null && dto.PMAttachments.Count > 0)
            //{
            //    entity.PMAttachments = string.Join(";", dto.PMAttachments);
            //}

            await _context.SaveChangesAsync();

            ResultOutputData resultOutputData = new ResultOutputData();
            return resultOutputData.GenearetResultOutput(entity, 1);
        }



        #region Helper Methods

        private IQueryable<FeatureRequest> GetFullRequestQuery()
        {
            return _context.FeatureRequests
                .Include(r => r.Module)
                .Include(r => r.SubmitterAttachments)
                .Include(r => r.PMAttachments)
                .Where(r => r.IsDeleted == false);
        }

        private async Task<List<FeatureRequest>> ReadRequestsFromExcel(Stream fileStream, string userId)
        {
            var requests = new List<FeatureRequest>();

            // TODO: Implement Excel reading logic using EPPlus or ClosedXML
            // This is a placeholder implementation

            // Example structure:
            /*
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var request = new FeatureRequest
                {
                    Title = worksheet.Cells[row, 1].Value?.ToString(),
                    Description = worksheet.Cells[row, 2].Value?.ToString(),
                    BusinessContext = worksheet.Cells[row, 3].Value?.ToString(),
                    Category = Enum.Parse<RequestCategory>(worksheet.Cells[row, 4].Value?.ToString()),
                    ExpectedValue = Enum.Parse<ExpectedValue>(worksheet.Cells[row, 5].Value?.ToString()),
                    TimeCriticality = Enum.Parse<TimeCriticality>(worksheet.Cells[row, 6].Value?.ToString()),
                    SubmitterUserId = userId,
                    SubmissionDate = DateTime.Now
                };
                
                requests.Add(request);
            }
            */

            await Task.CompletedTask; // Remove this when implementing actual Excel reading

            return requests;
        }

        private string GenerateRequestId(FeatureRequest request)
        {
            var now = DateTime.Now;
            var year = now.Year;
            var quarter = (now.Month - 1) / 3 + 1;
            var moduleCode = request.Module?.Name?.Substring(0, 3).ToUpper() ?? "GEN";
            var random = RandomNumberGenerator.GetInt32(1000, 10000);

            return $"FRQ-{year}-Q{quarter}-{moduleCode}-{random}";
        }


        public async Task<OperationOutput> AddFeatureDocs(RequestDocumentDtos docs)
        {
            try
            {
                if (docs.FeatureRequestId != 0)
                {
                    // 🗑️ Step 1: Delete old documents for this release
                    //var oldDocs = await _context.RequestAttachments
                    //    .Where(x => x.PMFeatureRequestId == docs.FeatureRequestId
                    //             || x.SubmitterFeatureRequestId == docs.FeatureRequestId)
                    //    .ToListAsync();

                    //if (oldDocs.Any())
                    //{
                    //    _context.RequestAttachments.RemoveRange(oldDocs);
                    //    await _context.SaveChangesAsync();
                    //}

                    // 🆕 Step 2: Add new Release Documents
                    if (docs.SubmitterAttachments != null && docs.SubmitterAttachments.Any())
                    {
                        var newReleaseDocs = _Assembler.WriteDocsListDal(docs.SubmitterAttachments);

                        foreach (var doc in newReleaseDocs)
                        {
                            doc.Id = 0;
                            doc.SubmitterFeatureRequestId = docs.FeatureRequestId;
                        }

                        await _context.RequestAttachments.AddRangeAsync(newReleaseDocs);
                    }

                    // 🆕 Step 3: Add new Training Documents
                    if (docs.PMAttachments != null && docs.PMAttachments.Any())
                    {
                        var newPMAttachments = _Assembler.WriteDocsListDal(docs.PMAttachments);

                        foreach (var doc in newPMAttachments)
                        {
                            doc.Id = 0;
                            doc.PMFeatureRequestId = docs.FeatureRequestId;
                        }

                        await _context.RequestAttachments.AddRangeAsync(newPMAttachments);
                    }

                    // 💾 Step 4: Save all changes
                    await _context.SaveChangesAsync();
                }

                ResultOutputData resultOutputData = new ResultOutputData();
                return resultOutputData.GenearetResultOutput(docs, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving documents for FeatureRequest ID {FeatureRequestId}", docs.FeatureRequestId);
                return ResultOutputData.GenearetResultOutputCatch();
            }
        }

        #endregion
    }
}
