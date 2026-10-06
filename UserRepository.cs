
using Core.Helpers;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Office;
using DocumentFormat.OpenXml.Wordprocessing;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nupco.Core;
using Nupco.Core.Assembler;
using Nupco.Core.Consts;
using Nupco.Core.Dto;
using Nupco.Core.Dto.Kafo;
using Nupco.Core.Dto.User;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Extensions;
using Nupco.DAL.Model;
using Nupco.DAL.Models.Users;
using Nupco.EF.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Nupco.Core.Helpers.JWTHelper;

namespace Nupco.EF.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly User_Assembler _User_Assembler;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string pathToSave = "";

        public UserRepository(ApplicationDbContext context, ILogger logger, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
            : base(context, logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _User_Assembler = new User_Assembler();
            var sharedPath = ConfigurationHelper.GetValueWithParam_String("SiteSettings:sharedPath");
            var folderName = "Images/";
            pathToSave = Path.Combine(sharedPath, folderName);

        }


        #region UserViews
        public async Task<OperationOutput> AddUserViewAsync(UserViewDto dto)
        {
            try
            {
                OperationOutput Result = new OperationOutput();
                var _user = await _userManager.FindByIdAsync(dto.UserId);
                if (_user is not null && _user.IsDeleted == false)
                {
                    UserView userView = new UserView();
                    userView.UserId = dto.UserId;
                    userView.EntityId = dto.EntityId;
                    userView.ItemId = dto.ItemId;
                    userView.CreatedAt = DateTime.Now;
                    userView.Platform = dto.Platform;

                    await _context.UserViews.AddAsync(userView);
                    await _context.SaveChangesAsync();

                    var userViewDto = _User_Assembler.WriteDto(userView);

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(userViewDto, 1);
                    return _result;
                }

                var __Result = ResultOutputData.GenearetResultOutputUserNotExist();
                return __Result;


            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }

        }

        #endregion


        public async Task<UserDto?> CreateUserFromImportSheet(User userInfo)
        {
            User user = new();
            user.Id = Guid.NewGuid().ToString();
            user.FirstName = userInfo.FirstName;
            user.LastName = userInfo.LastName;
            user.Code = userInfo.Code;
            user.PhoneNumber = userInfo.PhoneNumber;
            user.DepartmentName = userInfo.DepartmentName;
            user.PositionName = userInfo.PositionName;
            user.Email = userInfo.Email;
            user.NormalizedEmail = userInfo.Email;
            user.UserName = userInfo.UserName;
            user.NormalizedUserName = userInfo.UserName;
            user.EmailConfirmed = true;
            user.CreatedDate = DateTime.Now;
            user.HireDate = userInfo.HireDate;
            user.DeviceType = "web";
            var result = await _userManager.CreateAsync(user, "NewUser@123");

            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();

                var userDto = _User_Assembler.WriteDto(await _userManager.FindByIdAsync(user.Id));
                return userDto;
            }
            else
            {
                return null;
            }

        }

        public async Task<UserDto?> CreateUserDirectAsync(User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return _User_Assembler.WriteDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateUserDirectAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<OperationOutput> AddEntityAsync(UserObjDto dto)
        {
            try
            {
                OperationOutput Result = new OperationOutput();
                //var _email = await _userManager.FindByEmailAsync(dto.Email);
                var _email = await _context.Users.FirstOrDefaultAsync(i=>i.Email == dto.Email);
                if (_email is not null && _email.IsDeleted == false)
                {

                    var __Result = ResultOutputData.GenearetResultOutputEmailIsExists();
                    return __Result;
                }

                var user = _User_Assembler.WriteObjDal(dto);
                user.Id = Guid.NewGuid().ToString();
                user.NormalizedEmail = dto.Email;
                user.NormalizedUserName = dto.UserName;
                user.Code = dto.Code;
                user.IdentityNo = dto.IdentityNo;
                user.PhoneNumber = dto.PhoneNumber;
                user.EmailConfirmed = true;
                user.GenderId = 1;
                user.CreatedDate = DateTime.Now;
                user.DeviceType = "web";

                //user.UserName = dto.UserName;

                //user.CreatedAt = DateTime.Now;
                //user.UpdatedAt = DateTime.Now;

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (result.Succeeded)
                {
                    foreach (var _entity in dto.Entities)
                    {
                        UsersEntity usersEntity = new UsersEntity();
                        usersEntity.UserId = user.Id;
                        usersEntity.EntityId = _entity.EntityId;
                        await _context.UsersEntities.AddAsync(usersEntity);

                    }
                    await _context.SaveChangesAsync();

                    var userDto = _User_Assembler.WriteDto(await _userManager.FindByIdAsync(user.Id));

                    ResultOutputData resultOutput = new ResultOutputData();
                    var _result = resultOutput.GenearetResultOutput(userDto, 1);
                    return _result;

                }

                var _Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return _Result;
            }
            catch (Exception)
            {
                var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return Result;
            }

        }

        public async Task<OperationOutput> ActivateUser(string id, bool activate)
        {
            //var result = await _userManager.FindByIdAsync(id);
            var result = await _context.Users.FirstOrDefaultAsync(i => i.Id == id);


            if (result != null)
            {
                result.IsActive = activate;
                Update(result);
                await _context.SaveChangesAsync();
                var _result = ResultOutputData.GenearetResultOutputSuccess();
                return _result;
            }
            var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return Result;
        }
        public async Task<OperationOutput> DeleteUser(string id)
        {
            //var result = await _userManager.FindByIdAsync(id);
            var result = await _context.Users.FirstOrDefaultAsync(i => i.Id == id);


            if (result != null)
            {
                result.IsDeleted = true;
                Update(result);
                await _context.SaveChangesAsync();
                var _result = ResultOutputData.GenearetResultOutputSuccess();
                return _result;
            }
            var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return Result;
        }


        public async Task<OperationOutput> DeleteRole(string roleName)
        {

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var result = await _userManager.GetUsersInRoleAsync(role.Name);

                if (!result.Any())
                {
                    await _roleManager.DeleteAsync(role);
                    await _context.SaveChangesAsync();
                    var _result = ResultOutputData.GenearetResultOutputSuccess();
                    return _result;
                }
            }
            var Result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return Result;
        }

        public async Task<OperationOutput> EditEntity(UserObjDto dto)
        {
            try
            {
                //var _user = await _userManager.FindByIdAsync(dto.Id);
                //var _userByEmail = await _userManager.FindByEmailAsync(dto.Email);

                var _user = await _context.Users.FirstOrDefaultAsync(i => i.Id == dto.Id);
                var _userByEmail = await _context.Users.FirstOrDefaultAsync(i => i.Email == dto.Email);

                if (_user is not null && _user.IsDeleted == false && _user.Email == dto.Email || _user is not null && _user.IsDeleted == false && _userByEmail is null)
                {
                    if (dto.OriginalPicBase64 != null)
                    {
                        dto.OriginalPic = !Strings.CheckStringNullOrEmptyOrWhiteSpaceOrZero(dto.OriginalPicBase64) ?
                               Images.SaveSingleImageOnServer(dto.OriginalPicBase64, 1024, pathToSave, true, 400, pathToSave) : dto.OriginalPic;
                    }
                }

                ResultOutputData resultOutput = new ResultOutputData();

                var result = await _userManager.FindByIdAsync(dto.Id);
                if (result != null)
                {

                    var entity = _User_Assembler.WriteObjDal(dto);
                    entity.Id = dto.Id;
                    entity.Code = dto.Code;
                    entity.IsCheckHQ = dto.IsCheckHQ;
                    entity.ReferenceId = 1;

                    if (!String.IsNullOrEmpty(dto.Password))
                    {
                        entity.PasswordHash = _userManager.PasswordHasher.HashPassword(result, dto.Password);
                    }
                    else
                    {
                        entity.PasswordHash = result.PasswordHash;
                    }
                    var _usersEntityIds = await _context.UsersEntities.Where(f => f.UserId == entity.Id).ToListAsync();
                    if (_usersEntityIds.Count > 0 && dto.Entities != null)
                    {
                        _context.UsersEntities.RemoveRange(_usersEntityIds);
                        await _context.SaveChangesAsync();
                    }
                    if (dto.Entities != null)
                    {
                        foreach (var _entity in dto.Entities)
                        {
                            UsersEntity usersEntity = new UsersEntity();
                            usersEntity.UserId = dto.Id;
                            usersEntity.EntityId = _entity.EntityId;
                            await _context.UsersEntities.AddAsync(usersEntity);

                        }
                    }
                    // entity.DeviceType = "web";

                    await UpdateAsync(entity, result);

                    await _context.SaveChangesAsync();

                    var user = _User_Assembler.WriteDto(await _userManager.FindByIdAsync(dto.Id));

                    if (user.IsDeleted == false)
                    {
                        user.UserPermissions = _context.UsersEntities.Include(x => x.Entity).Include(x => x.PermissionsEntities).Where(u => u.UserId == user.Id)
                             .Select(s => new UserEntityDto
                             {
                                 Id = s.Id,
                                 EntityId = s.Entity.Id,
                                 NameAr = s.Entity.NameAr,
                                 NameEn = s.Entity.NameEn,
                                 url = s.Entity.CmsIdentity,
                                 UserId = s.UserId,
                                 //  PermissionsEntities = s.PermissionsEntities.Select(s => new Tuple<string, int>(s.PermissionLevel.NameAr, s.PermissionsLevelId)).ToList()
                             }).ToList();

                        var ___result = resultOutput.GenearetResultOutput(user, 1);
                        return ___result;
                    }



                    var __result = resultOutput.GenearetResultOutput(user, 1);
                    return __result;

                }
                var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return _result;
            }
            catch (Exception)
            {
                var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return _result;
            }
        }

        public async Task<OperationOutput> EditUserCode(UserObjDto dto)
        {
            try
            {
                ResultOutputData resultOutput = new ResultOutputData();
               // var result = await _userManager.FindByIdAsync(dto.Id);
                var result = await _context.Users.FirstOrDefaultAsync(i => i.Id == dto.Id);

                if (result != null)
                {
                    result.IsCheckHQ = dto.IsCheckHQ;
                    result.Code = dto.Code;

                    await UpdateAsync(result, result);

                    await _context.SaveChangesAsync();

                    var user = _User_Assembler.WriteDto(await _context.Users.FirstOrDefaultAsync(i => i.Id == dto.Id));

                    var __result = resultOutput.GenearetResultOutput(user, 1);
                    return __result;

                }
                var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return _result;
            }
            catch (Exception)
            {
                var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return _result;
            }

        }
        public async Task<OperationOutput> GetAllUserList()
        {
            var _Users = await _context.Users.Where(u => u.IsDeleted == false).ToListAsync();
            var _UsersDto = _User_Assembler.WriteListDto(_Users);

            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(_UsersDto, 1);
            return result;
        }
        public async Task<OperationOutput> GetAllUsers()
        {
            var _Users = await _context.Users.Where(u => u.IsDeleted == false && !string.IsNullOrEmpty(u.NotificationToken)).ToListAsync();
            var _UsersDto = _User_Assembler.WriteListDto(_Users);

            foreach (var usr in _UsersDto)
            {
                usr.UserPermissions = _context.UsersEntities.Include(x => x.Entity).Include(x => x.PermissionsEntities).Where(u => u.UserId == usr.Id)
                     .Select(s => new UserEntityDto
                     {
                         Id = s.Id,
                         EntityId = s.Entity.Id,
                         NameAr = s.Entity.NameAr,
                         NameEn = s.Entity.NameEn,
                         url = s.Entity.CmsIdentity,
                         UserId = s.UserId,
                         PermissionsEntities = s.PermissionsEntities.Select(s => new Tuple<string, int>(s.PermissionLevel.NameAr, s.PermissionsLevelId)).ToList()
                     }).ToList();
            }

            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(_UsersDto, 1);
            return result;
        }



        public async Task<OperationOutput> GetAllUsersByTypeId(int id)
        {
            var _Users = await _context.Users.Where(u => u.UserTypeId == id && u.IsDeleted == false).ToListAsync();
            var _UsersDto = _User_Assembler.WriteListDto(_Users);

            foreach (var usr in _UsersDto)
            {
                usr.UserPermissions = _context.UsersEntities.Include(x => x.Entity).Include(x => x.PermissionsEntities).Where(u => u.UserId == usr.Id)
                     .Select(s => new UserEntityDto
                     {
                         Id = s.Id,
                         EntityId = s.Entity.Id,
                         NameAr = s.Entity.NameAr,
                         NameEn = s.Entity.NameEn,
                         url = s.Entity.CmsIdentity,
                         UserId = s.UserId,
                         PermissionsEntities = s.PermissionsEntities.Select(s => new Tuple<string, int>(s.PermissionLevel.NameAr, s.PermissionsLevelId)).ToList()
                     }).ToList();
            }
            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(_UsersDto, 1);
            return result;
        }

        public async Task<OperationOutput> GetUser(string id)
        {
            //var _user = await _userManager.FindByIdAsync(id);
            var _user = await _context.Users.FirstOrDefaultAsync(i => i.Id == id);

            var user = _User_Assembler.WriteDto(_user);
            if (user.IsDeleted == false)
            {
                user.UserPermissions = _context.UsersEntities.Include(x => x.Entity).Include(x => x.PermissionsEntities).OrderBy(o => o.Entity.EntityOrder).Where(u => u.UserId == user.Id && u.Entity.IsActive == true)
                     .Select(s => new UserEntityDto
                     {
                         Id = s.Id,
                         EntityId = s.Entity.Id,
                         NameAr = s.Entity.NameAr,
                         NameEn = s.Entity.NameEn,
                         url = s.Entity.CmsIdentity,
                         UserId = s.UserId,
                         //  PermissionsEntities=s.PermissionsEntities.Select(s=> new Tuple<string, int>(s.PermissionLevel.NameAr, s.PermissionsLevelId)).ToList()
                     }).ToList();



                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(user, 1);
                return _result;
            }

            var __result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return __result;
        }

        public async Task<OperationOutput> GetListUserByUserName(string userName)
        {
            var users = _User_Assembler.WriteListDto(await _context.Users.Where(i => i.UserName.Contains(userName) && !i.IsDeleted).ToListAsync());

            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(users, users.Count());
            return result;
        }



        public async Task<OperationOutput> GetUserByUserName(string userName)
        {
            var user = _User_Assembler.WriteDto(await _context.Users.FirstOrDefaultAsync(i => i.UserName == userName));
            if (user != null && user.IsDeleted == false)
            {
                user.UserPermissions = _context.UsersEntities.Include(x => x.Entity).Include(x => x.PermissionsEntities).Where(u => u.UserId == user.Id)
                     .Select(s => new UserEntityDto
                     {
                         Id = s.Id,
                         EntityId = s.Entity.Id,
                         NameAr = s.Entity.NameAr,
                         NameEn = s.Entity.NameEn,
                         url = s.Entity.CmsIdentity,
                         UserId = s.UserId,
                         //  PermissionsEntities=s.PermissionsEntities.Select(s=> new Tuple<string, int>(s.PermissionLevel.NameAr, s.PermissionsLevelId)).ToList()
                     }).ToList();


                //var roles = await _userManager.GetRolesAsync(_User_Assembler.WriteDal(user));
                //if (roles.Any())
                //    user.RoleName = roles[0];
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(user, 1);
                return result;
            }

            var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return _result;
        }

        public async Task<OperationOutput> GetUserObj(string id)
        {
           // var exist = await _userManager.FindByIdAsync(id);
            var exist = await _context.Users.FirstOrDefaultAsync(i => i.Id == id);

            var entity = _User_Assembler.WriteObjDto(exist);
            if (exist != null && entity.IsDeleted == false)
            {
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(entity, 1);
                return result;
            }

            var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return _result;
        }


        public async Task<OperationOutput> GetUserClaim(UserDto user, string claimType)
        {
            var userClaim = await _context.UserClaims.Where(x => x.ClaimType == claimType && x.UserId == user.Id).FirstOrDefaultAsync();
            if (userClaim != null)
            {
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(userClaim.ClaimValue.ToString(), 1);
                return result;
            }

            var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return _result;
        }

        public async Task<OperationOutput> GetRoleClaim(UserDto usr, string claimType)
        {
            var ur = await _userManager.GetRolesAsync(_User_Assembler.WriteDal(usr));
            if (ur.Count > 0)
            {
                var rol = await _roleManager.FindByNameAsync(ur[0]);
                var roleClaim = await _context.RoleClaims.FirstOrDefaultAsync(x => x.ClaimType == claimType && x.RoleId == rol.Id);

                if (roleClaim != null)
                {
                    ResultOutputData resultOutput = new ResultOutputData();
                    var result = resultOutput.GenearetResultOutput(roleClaim.ClaimValue.ToString(), 1);
                    return result;
                }
            }

            var _result = ResultOutputData.GenearetResultOutputNoDataReturned();
            return _result;
        }

        public async Task<OperationOutput> GetApplicationUsers(FilterByPagenation _filter)
        {
            var _UsersDtos = new List<UserDto>();

            var _Users = await _context.Users.Where(i => i.UserName.Contains(_filter.searchBy) || i.FirstName.Contains(_filter.searchBy) || i.LastName.Contains(_filter.searchBy))
                                                 .Where(u => u.IsDeleted == false && (u.DeviceType == "android" || u.DeviceType == "ios"))
                                                 .Include(u => u.UserType)
                                                 .Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                                                 .ToListAsync();
            _UsersDtos = _User_Assembler.WriteListDto(_Users);

            var counts = await CountAsync(u => u.IsDeleted == false && (u.DeviceType == "android" || u.DeviceType == "ios"));


            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(_UsersDtos, counts);
            return result;

        }


        public async Task<OperationOutput> GetAllUsersByPagenation(FiltersBy _filter)
        {
            // var _Users = await _context.Users.Where(u => u.UserTypeId == _filter.UserTypeId && u.IsDeleted == false).ToListAsync();
            var _UsersDtos = new List<UserDto>();
            var spec = Specification<User>.All.And(new UserSpecification(_filter));

            if (_filter.pageNumber == null)
            {
                var _Users = await _context.Users.Where(spec.ToExpression()).Include(u => u.UserType).ToListAsync();
                _UsersDtos = _User_Assembler.WriteListDto(_Users.Where(x => x.UserTypeId == _filter.UserTypeId).ToList());
            }
            else
            {
                var _Users = await _context.Users.Where(spec.ToExpression())
                                                     .Where(u => u.IsDeleted == false)
                                                     .Include(u => u.UserType).
                                                      Skip((int)_filter.pageNumber * (int)_filter.pageSize).Take((int)_filter.pageSize)
                                                     .ToListAsync();
                _UsersDtos = _User_Assembler.WriteListDto(_Users);
            }


            foreach (var usr in _UsersDtos)
            {
                usr.UserPermissions = _context.UsersEntities.Include(x => x.Entity).Include(x => x.PermissionsEntities).Where(u => u.UserId == usr.Id)
                .Select(s => new UserEntityDto
                {
                    Id = s.Id,
                    EntityId = s.Entity.Id,
                    NameAr = s.Entity.NameAr,
                    NameEn = s.Entity.NameEn,
                    url = s.Entity.CmsIdentity,
                    UserId = s.UserId,
                    PermissionsEntities = s.PermissionsEntities.Select(s => new Tuple<string, int>(s.PermissionLevel.NameAr, s.PermissionsLevelId)).ToList()
                }).ToList();


            }
            var counts = 0;
            if (!string.IsNullOrEmpty(_filter.deviceType))
                counts = await CountAsync(u => u.IsDeleted == false && u.DeviceType == _filter.deviceType);
            else
                counts = await CountAsync(u => u.IsDeleted == false);


            var userNames = _UsersDtos.Select(u => u.UserName).ToList();

            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(_UsersDtos, counts);
            return result;

        }

        public async Task<OperationOutput> Export()
        {
            var _UsersDtos = new List<ExportUser>();
            var _Users = await _context.Users.Where(i => i.IsDeleted == false).ToListAsync();
            _UsersDtos = _User_Assembler.WriteListDto(_Users)
                .Select(s => new ExportUser(
                FullName: s.FirstName + " " + s.LastName,
                Email: s.Email,
                DeviceType: s.deviceType,
                Department: s.DepartmentName,
                Position: s.PositionName,
                Code: s.Code,
                CreatedDate: s.CreatedDate,
                Status: s.StatusAvailable,
                Bio: s.Bio,
                EntityName: s.EntityName)).ToList();

            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(_UsersDtos, _UsersDtos.Count());
            return result;
        }



        public async Task<OperationOutput> GetClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email)
            };
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(claims, claims.Count());
            return result;
        }

        public async Task<OperationOutput> ChangePassword(ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
                if (result.Succeeded)
                {

                    var __result = ResultOutputData.GenearetResultOutputSuccess();
                    return __result;
                }

            }

            var _result = ResultOutputData.GenearetResultOutputCatch();
            return _result;
        }

        public async Task<OperationOutput> AddEditRoleClaim(PermissionRoleDto permissionRole)
        {
            var user = await _userManager.FindByIdAsync(permissionRole.UserId);
            var role = await _roleManager.FindByNameAsync(permissionRole.RoleName);


            try
            {
                if (role != null)
                {
                    var ClaimsRole = await _roleManager.GetClaimsAsync(role);
                    var ClaimsUser = await _userManager.GetClaimsAsync(user);

                    foreach (var Clrole in ClaimsRole)
                        await _roleManager.RemoveClaimAsync(role, Clrole);

                    foreach (var Cluser in ClaimsUser)
                        await _userManager.RemoveClaimAsync(user, Cluser);

                    await _roleManager.AddClaimAsync(role, new Claim(CustomType.UserPermissions, permissionRole.PermissionValues));
                    await _roleManager.AddClaimAsync(role, new Claim(CustomType.NotificationsRoles, permissionRole.NotiPermissionValues));
                    await _context.SaveChangesAsync();

                }
                else
                {

                    if (!String.IsNullOrEmpty(permissionRole.RoleName))
                    {
                        role = new IdentityRole(permissionRole.RoleName);
                        await _roleManager.CreateAsync(role);
                        await _userManager.AddToRoleAsync(user, role.Name);
                        var newrole = await _roleManager.FindByNameAsync(role.Name);
                        await _roleManager.AddClaimAsync(newrole, new Claim(CustomType.UserPermissions, permissionRole.PermissionValues));
                        await _roleManager.AddClaimAsync(newrole, new Claim(CustomType.NotificationsRoles, permissionRole.NotiPermissionValues));


                        await _context.SaveChangesAsync();

                    }

                }
            }
            catch (Exception)
            {
                var __result = ResultOutputData.GenearetResultOutputNoDataReturned();
                return __result;
            }


            await _userManager.AddClaimAsync(user, new Claim(CustomType.UserPermissions, permissionRole.PermissionValues));
            await _userManager.AddClaimAsync(user, new Claim(CustomType.NotificationsRoles, permissionRole.PermissionValues));

            var _result = ResultOutputData.GenearetResultOutputSuccess();
            return _result;
        }

        public async Task<OperationOutput> GetAllRols()
        {
            var rols = await _roleManager.Roles.ToListAsync();
            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(rols, rols.Count());
            return result;
        }

        public async Task<OperationOutput> GetAllRolsWithClaims()
        {
            List<PermissionRoleDto> permissionRoleDtos = new List<PermissionRoleDto>();
            try
            {
                var rols = await _context.Roles.ToListAsync();

                for (int i = 0; i < rols.Count; i++)
                {
                    var rol = await _roleManager.FindByNameAsync(rols[i].Name);
                    var permissionClaim = await _context.RoleClaims.FirstOrDefaultAsync(x => x.ClaimType == CustomType.UserPermissions && x.RoleId == rols[i].Id);
                    var notificationClaim = await _context.RoleClaims.FirstOrDefaultAsync(x => x.ClaimType == CustomType.NotificationsRoles && x.RoleId == rols[i].Id);

                    var temp = new PermissionRoleDto();
                    temp.RoleName = rols[i].Name;
                    temp.PermissionValues = permissionClaim.ClaimValue;
                    temp.NotiPermissionValues = notificationClaim.ClaimValue;

                    permissionRoleDtos.Add(temp);
                }
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(permissionRoleDtos, permissionRoleDtos.Count());
                return result;

            }
            catch (Exception)
            {
                ResultOutputData resultOutput = new ResultOutputData();
                var result = resultOutput.GenearetResultOutput(permissionRoleDtos, permissionRoleDtos.Count());
                return result;
            }

        }

        public async Task<OperationOutput> UpdateWebNotificationToken(string userId, string notificationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(f => f.Id == userId);
            if (user == null)
            {
                return ResultOutputData.GenearetResultOutputUserNotExist();
            }

            user.NotificationTokenWeb = notificationToken;
            await _context.SaveChangesAsync();

            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(user, 1);
            return result;

        }
        public async Task<OperationOutput> GetAllUserTypes()
        {
            var userTypes = await _context.UserTypes.Select(s => new UserTypeDto
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn
            }).ToListAsync();
            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(userTypes, userTypes.Count());
            return result;
        }
        public async Task<OperationOutput> GetAllDeviceTypes()
        {
            var deviceTypes = await _context.Users.Where(i => !string.IsNullOrEmpty(i.DeviceType))
                .Select(s => s.DeviceType)
                .Distinct().Select(s => new DeviceTypeDto
                {
                    lable = s,
                    value = s
                }
                    ).ToListAsync();
            ResultOutputData resultOutput = new ResultOutputData();
            var result = resultOutput.GenearetResultOutput(deviceTypes, deviceTypes.Count());
            return result;
        }

        public async Task<UserDto> UpdateUserDevice(User _n, User _o)
        {
            var entity = await UpdateAsync(_n, _o);
            await _context.SaveChangesAsync();
            var userDto = _User_Assembler.WriteDto(entity);
            return userDto;
        }

        public async Task<profile> GetUserProfileInfo(UserDto userDto, LdapDto _ldap, IUnitOfWork _unitOfWork, string Language)
        {
            var _lDAPUsersDetails = await _unitOfWork.LDAP.CheckHQ(userDto.UserName, _ldap.LdapServer, _ldap.LdapAccountLogin, _ldap.LdapPassword, _ldap.LdapDistinguishedName);
            var _settingsUser = await _unitOfWork.UserSettings.GetUserSettingsByUserId(userDto.Id, Language);
            var _voteNotAnswerUser = await _unitOfWork.Votes.GetLastVoteByUserId(userDto.Id);
            var _TopUsersOfMonth = await _unitOfWork.Kafo.GetTopUsersOfMonthResult(new KafoFiltersByYearAndMonth());
            var _TopIcon = await _unitOfWork.Kafo.GetAllKafoIconsToUserProfile();
            var _splash = await _unitOfWork.SpalshRepository.GetEntityWithMaxIdAsync();
            var _isStoryAllowed = await _unitOfWork.dbContext.StoryAllowedUsers.AnyAsync(i => i.UserId == userDto.Id);

            var _trans = new TransactionRequest();
            _trans.empCode = userDto.Code;
            _trans.startTime = DateTime.Now.ToShortDateString();
            var _EmployeeTodayAttendace = await _unitOfWork.Auths.GetEmployeeTodayAttendace(_trans);

            var _voteNotAnswerUserList = new List<VoteDto>();
            if (_voteNotAnswerUser is not null)
            {
                _voteNotAnswerUserList.Add(_voteNotAnswerUser);
            }

            // Create the profile object with all the collected data
            var obj = new profile(
                IsHQ: _lDAPUsersDetails,
                UserSettings: _settingsUser,
                Vote: _voteNotAnswerUserList,
                KafoUsersOfMonths: _TopUsersOfMonth.Item1,
                EmployeeTodayAttendace: _EmployeeTodayAttendace,
                kafoIcons: _TopIcon,
                Profile: userDto,
                Splash: _splash,
                IsStoryAllowed: _isStoryAllowed
            );
            return obj;
        }
        public async Task<string> CreateUserSession(string userId, string jti, int expirationDays = 1)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // Deactivate old sessions for this user
                    var oldSessions = await _context.UserSessions
                        .Where(x => x.UserId == userId && x.IsActive == true)
                        .ToListAsync();

                    foreach (var session in oldSessions)
                    {
                        session.IsActive = false;
                        session.DeactivatedAt = DateTime.UtcNow;
                        session.DeactivatedReason = "New login";
                    }

                    // Create new session
                    var newSession = new UserSession
                    {
                        UserId = userId,
                        SessionId = jti,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                        LastActivityAt = DateTime.UtcNow
                    };

                    await _context.UserSessions.AddAsync(newSession);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Session created for user {userId} with JTI: {jti}");
                    return jti;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating session: {ex.Message}");
                throw; // Or handle as needed
            }
        }


     
        public string GenerateToken(dynamic data, string userId = null)
        {
            try
            {
                string tokenKey = "SecureKeyRequiredforvalidationAdmin";
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(tokenKey);

                // Generate unique session ID (JTI)
                var jti = Guid.NewGuid().ToString();

                // Extract user information from data if userId not provided
                string extractedUserId = userId;
                string userEmail = null;
                string userName = null;

                try
                {
                    // Try to extract from data object if userId not provided
                    if (string.IsNullOrEmpty(extractedUserId))
                    {
                        var dataObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(data));
                        if (dataObj != null)
                        {
                            if (dataObj.ContainsKey("Id"))
                                extractedUserId = dataObj["Id"]?.ToString();
                            if (dataObj.ContainsKey("Email"))
                                userEmail = dataObj["Email"]?.ToString();
                            if (dataObj.ContainsKey("UserName"))
                                userName = dataObj["UserName"]?.ToString();
                            if (string.IsNullOrEmpty(userName) && dataObj.ContainsKey("Name"))
                                userName = dataObj["Name"]?.ToString();
                        }
                    }
                }
                catch
                {
                    // Ignore extraction errors
                }

                // Create claims list with all necessary claims
                var claims = new List<Claim>
        {
            // Original UserEntity claim (for backward compatibility)
            new Claim("UserEntity", JsonConvert.SerializeObject(data)),
            
            // JTI for session tracking
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("session_id", jti),
            
            // Standard claim types for ASP.NET Core
            new Claim(JwtRegisteredClaimNames.Sub, extractedUserId ?? ""),
        };

                // Add user ID if available
                if (!string.IsNullOrEmpty(extractedUserId))
                {
                    claims.Add(new Claim(JwtRegisteredClaimNames.Sid, extractedUserId));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, extractedUserId));
                }

                // Add email if available
                if (!string.IsNullOrEmpty(userEmail))
                {
                    claims.Add(new Claim(JwtRegisteredClaimNames.Email, userEmail));
                    claims.Add(new Claim(ClaimTypes.Email, userEmail));
                }

                // Add name if available
                if (!string.IsNullOrEmpty(userName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, userName));
                    claims.Add(new Claim("name", userName));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(1),
                    IssuedAt = DateTime.UtcNow,
                    NotBefore = DateTime.UtcNow,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // Create session in database if we have a userId
                if (!string.IsNullOrEmpty(extractedUserId))
                {
                    try
                    {
                        // Call the session creation method (non-static, can use _context)
                        CreateUserSession(extractedUserId, jti, 1).Wait(); // Use Wait() since this is synchronous
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error saving session in GenerateToken: {ex.Message}");
                        // Don't fail token generation if session saving fails
                    }
                }

                return Strings.CompressString(tokenString);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GenerateToken: {ex.Message}");
                return null;
            }
        }


    }
}