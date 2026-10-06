using Core.Helpers;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Api.Gax.ResourceNames;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.upload;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Models;
using OfficeOpenXml;

//using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;



namespace Nupco.EF.Repositories
{
    internal class UploadRepository : IUploadRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly Country_Assembler _Country_Assembler;

        private string[] stringArray = { "Country" };
        private string pathToSave = "";



        public UploadRepository()
        {

        }

        public OperationOutput UploadFileOrImageWithThump(ImageParam model)
        {
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            var folderNameImages = "Images/";
            var folderNameVideos = "Videos/";

            var fileName = "";
            int? width = 0;
            int? height = 0;

            if (model.fileType?.ToLower() == "image")
            {
                pathToSave = Path.Combine(sharedPath, folderNameImages);

                fileName = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : String.Empty;

                width = Images.GetWidthAndHight(fileName).GetAwaiter().GetResult().width;
                height = Images.GetWidthAndHight(fileName).GetAwaiter().GetResult().height;

            }

            else if (model.fileType?.ToLower() == "video")
            {
                pathToSave = Path.Combine(sharedPath, folderNameVideos);

                fileName = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                Images.UploadVideoFileNew(model.OriginalPicBase64, "Videos/") : String.Empty;
            }

            var obj = new
            {
                height = height,
                width = width,
                fileName = fileName,
            };

            ResultOutputData resultOutput = new ResultOutputData();
            var _result = resultOutput.GenearetResultOutput(obj, 1);
            return _result;
        }


        public OperationOutput UploadFileOrImage(FileOrImage model)
        {
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

            var folderName = @"Images\";
            pathToSave = Path.Combine(sharedPath, folderName);
            var fileName = "";
            int? width = 0;
            int? height = 0;
            if (model.type?.ToLower() == "image")
            {
                fileName = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                            Images.SaveSingleImageOnServer(model.OriginalPicBase64, 1024, pathToSave, false, 0, pathToSave) : String.Empty;

                width = Images.GetWidthAndHight(fileName).GetAwaiter().GetResult().width;
                height = Images.GetWidthAndHight(fileName).GetAwaiter().GetResult().height;

            }
            else if (model.type?.ToLower() == "file")
            {
                fileName = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                Images.UploadPdfFileNew(model.OriginalPicBase64, sharedPath + @"Files\") : String.Empty;
            }
            else if (model.type?.ToLower() == "mp4")
            {
                fileName = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                Images.UploadVideoFileNew(model.OriginalPicBase64, sharedPath + @"Videos\") : String.Empty;
            }
            else if (model.type?.ToLower() == "mp3")
            {
                fileName = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(model.OriginalPicBase64) ?
                Images.UploadVoiceFileNew(model.OriginalPicBase64, sharedPath + @"Voices\") : String.Empty;

            }

            var obj = new
            {
                height = height,
                width = width,
                fileName = fileName,
            };

            ResultOutputData resultOutput = new ResultOutputData();
            var _result = resultOutput.GenearetResultOutput(obj, 1);
            return _result;
        }

        public async Task<OperationOutput> GetDataFromExcelFileUploaded(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ResultOutputData.GenearetResultOutputNoDataReturned();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            var data = new List<Dictionary<string, object>>();

            int rows = worksheet.Dimension.Rows;
            int cols = worksheet.Dimension.Columns;

            var headers = new List<string>();
            for (int col = 1; col <= cols; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text);
            }

            for (int row = 2; row <= rows; row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= cols; col++)
                {
                    rowData[headers[col - 1]] = worksheet.Cells[row, col].Text;
                }
                data.Add(rowData);
            }

            var objectList = data
                        .Select(dict => JsonSerializer.Deserialize<UserSheet>(JsonSerializer.Serialize(dict)))
                        .ToList();
            var _listGroupUsers = objectList.Select(s => new
            {
                UserName = s.UserName,
                GroupName = s.GroupName
            }).ToList();

            ResultOutputData resultOutput = new ResultOutputData();
            var _result = resultOutput.GenearetResultOutput(_listGroupUsers, _listGroupUsers.Count());
            return _result;
        }



        public async Task<OperationOutput> UploadAttachment(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ResultOutputData.GenearetResultOutputNoDataReturned();

            try
            {
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                string subFolder = "Docs";
                var contentType = file.ContentType.ToLower();

                if (contentType.StartsWith("image/"))
                {
                    subFolder = "Stories";
                }
                else if (contentType.StartsWith("video/") || contentType.EndsWith("mp4"))
                {
                    subFolder = "Stories";
                }
                else if (contentType.StartsWith("audio/") || contentType.EndsWith("mp3"))
                {
                    subFolder = "Voices";
                }

                var uploadsFolder = Path.Combine(sharedPath, subFolder);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                //var pathSaved = Path.Combine(subFolder, uniqueFileName);

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(uniqueFileName, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputNoDataReturned();
            }
        }
        public async Task<OperationOutput> UploadAttachmentWithType(IFormFile file, string? uploadType = null)
        {
            if (file == null || file.Length == 0)
                return ResultOutputData.GenearetResultOutputNoDataReturned();

            if (!IsFileAllowed(file.FileName))
                return ResultOutputData.GenearetResultOutputCatch();

            try
            {
                var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");

                string subFolder = uploadType?.ToLower() switch
                {
                    "story" => "Stories",
                    "doc" => "Docs",
                    _ => GetAutoFolderByContentType(file.ContentType)
                };

                var uploadsFolder = Path.Combine(sharedPath, subFolder);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string extension = Path.GetExtension(file.FileName).ToLower();
                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                ResultOutputData resultOutput = new ResultOutputData();
                return resultOutput.GenearetResultOutput(uniqueFileName, 1);
            }
            catch (Exception)
            {
                return ResultOutputData.GenearetResultOutputNoDataReturned();
            }
        }

        private string GetAutoFolderByContentType(string contentType)
        {
            contentType = contentType.ToLower();

            if (contentType.StartsWith("image/")) return "Images";
            if (contentType.StartsWith("video/")) return "Videos";
            if (contentType.StartsWith("audio/")) return "Voices";

            return "";
        }

        private bool IsFileAllowed(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            var allowedExtensions = new[]
            {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".mp4", ".mov", ".avi", ".webm",
        ".mp3", ".wav", ".aac",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt"
    };

            return allowedExtensions.Contains(extension);
        }
    }


}
