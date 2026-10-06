using Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Nupco.Core.Assembler;
using Nupco.Core.Dto;
using Nupco.Core.Helpers;
using Nupco.Core.Interfaces;
using Nupco.DAL;
using Nupco.DAL.Extensions;
using Nupco.DAL.Models.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Transactions;


namespace Nupco.EF.Repositories
{
    public class AuthRepository : BaseRepository<User>, IAuthRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly JWT _jwt;
        private readonly IUserRepository _userRepository;

        private static readonly HttpClient client = new HttpClient();
        private readonly string Api_transactions;
        private readonly string Api_Token_Auth;
        private readonly string AttendanceUser;
        private readonly string AttendancePassword;
        private readonly User_Assembler _User_Assembler;

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(1);
        private string cacheKey = "attendanceToken";
        private readonly ApplicationDbContextAttendance _contextAttendance;
        private readonly Iclock_transaction_Assembler _iclock_transaction_Assembler = new Iclock_transaction_Assembler();


        public AuthRepository(UserManager<User> userManager, JWT jwt, ApplicationDbContext context, ApplicationDbContextAttendance contextAttendance, ILogger logger, IMemoryCache cache) : base(context, logger)
        {
            _userManager = userManager;
            _jwt = jwt;
            _logger = logger;
            _context = context;
            _contextAttendance = contextAttendance;
            _User_Assembler = new User_Assembler();
            _cache = cache;


            Api_transactions = ConfigurationHelper.GetValueWithParam_String("SiteSettings:Api_transactions");
            Api_Token_Auth = ConfigurationHelper.GetValueWithParam_String("SiteSettings:Api_Token_Auth");
            AttendanceUser = ConfigurationHelper.GetValueWithParam_String("SiteSettings:AttendanceUser");
            AttendancePassword = ConfigurationHelper.GetValueWithParam_String("SiteSettings:AttendancePassword");
        }

        public async Task<OperationOutput> GetEmployeeTransactions(TransactionRequest modal, CancellationToken cancellationToken)
        {
            try
            {

                var startDate = Dates.ConvertStringToDate(modal.startTime);
                var endDate = Dates.ConvertStringToDate(modal.endTime+ " 23:59:59");

                var transactionList = await _contextAttendance.iclock_transaction.AsNoTrackingWithIdentityResolution()
                .Where(i => i.emp_code == modal.empCode && i.punch_time.Date >= startDate.Value.Date && i.punch_time.Date <= endDate.Value.Date)
                .ToListAsync();

                var _resultTransaction = _iclock_transaction_Assembler.WriteListDto(transactionList);
                var response = new Response();
                response.count = _resultTransaction.Count();
                response.dataClock.AddRange(_resultTransaction);

                ResultOutputData resultOutput = new ResultOutputData();
                var _result = resultOutput.GenearetResultOutput(response, response.count.Value);
                return _result;
            }
            catch (OperationCanceledException)
            {
                // Handle the case when the client cancels the request
                _logger.LogWarning("Client disconnected or request was canceled from GetEmployeeTransactions.");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("this Error in GetEmployeeTransactions : " + ex.Message);
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }

        }


        public async Task<OperationOutput> GetRefreshTokenAsync(UserDto CurrentUser)
        {
            ResultOutputData resultOutput = new ResultOutputData();

            var user = await FindAsync(u => u.Id == CurrentUser.Id);
            if (user != null && user.IsActive && !user.IsDeleted)
            {

                LoginDto model = new LoginDto();
                model.Email = user.Email;
                model.Password = user.PasswordHash;
                var result = resultOutput.GenearetResultOutput(await GetTokenAsync(model, user), 1);
                return result;

            }
            else
            {
                var _user = new AuthDto { Message = "Not Active User", IsAuthenticated = false };
                var _result = resultOutput.GenearetResultOutput(_user, 1);
                return _result;
            }

        }

        public async Task<OperationOutput> GetTokenAsync(LoginDto model, bool isSSO = false)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    var Result = ResultOutputData.GenearetResultOutputUserNotExist();
                    return Result;
                }
                else
                {
                    if (!isSSO)
                    {
                        var result = await _userManager.CheckPasswordAsync(user, model.Password);
                        if (!result)
                        {
                            var Result = ResultOutputData.GenearetResultOutputAccessDenied();
                            return Result;
                        }
                    }

                    if (!user.IsDeleted && user.IsActive)
                    {
                        // FIX: Call the overloaded method directly, not recursively!
                        var tokenResult = await GetTokenAsync(model, user);  // This calls the overload with (LoginDto, User)

                        ResultOutputData resultOutput = new ResultOutputData();
                        var _result = resultOutput.GenearetResultOutput(tokenResult.Item1, 1, tokenResult.Item2.Token);
                        return _result;
                    }
                    else if (user.IsDeleted || (!user.IsActive))
                    {
                        OperationOutput Result = new OperationOutput();
                        if (user.IsDeleted)
                            Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.UserNotExist);
                        else
                            Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.AccessDenied);

                        Result.Header.Success = false;
                        Result.Header.Code = 200;
                        return Result;
                    }
                    else
                    {
                        var Result = ResultOutputData.GenearetResultOutputPasswordError();
                        return Result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetTokenAsync: {ex.Message}");
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<OperationOutput> GetTokenOnlyAsync(LoginDto model)
        {
            try
            {
                var user = await FindAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    var Result = ResultOutputData.GenearetResultOutputUserNotExist();
                    return Result;
                }
                else
                {
                    var result = await _userManager.CheckPasswordAsync(user, model.Password);

                    if (result && !user.IsDeleted && user.IsActive)
                    {
                        var _auth = await GetTokenOnlyAsync(model, user);
                        ResultOutputData resultOutput = new ResultOutputData();
                        // Pass the existing token
                        var _result = resultOutput.GenearetResultOutput(_auth, 1, _auth.Token);
                        return _result;
                    }
                    else if (user.IsDeleted || (!user.IsActive && result))
                    {
                        OperationOutput Result = new OperationOutput();
                        if (user.IsDeleted)
                            Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.UserNotExist);
                        else
                            Result.Header = ApplicationOperation.OperationResult(Enums.ServiceMessages.AccessDenied);

                        Result.Header.Success = false;
                        Result.Header.Code = 200;
                        return Result;
                    }
                    else
                    {
                        var Result = ResultOutputData.GenearetResultOutputPasswordError();
                        return Result;
                    }
                }
            }
            catch
            {
                var Result = ResultOutputData.GenearetResultOutputCatch();
                return Result;
            }
        }
        public async Task<Tuple<UserDto, AuthDto>> GetTokenAsync(LoginDto model, User user)
        {
            var _authDto = new AuthDto();

            var _userDto = await GetUser(user.Id);
            //_authDto.Token = GenerateJwtToken(_userDto);
            _authDto.Token = CreateJwtToken(_userDto);
            _authDto.IsAuthenticated = true;
            var _obj = Tuple.Create(_userDto, _authDto);


            return _obj;
        }
        
        public async Task<AuthDto> GetTokenOnlyAsync(LoginDto model, User user)
        {
            var _authDto = new AuthDto();

            var _userDto = await GetUser(user.Id);
            //_authDto.Token = GenerateJwtToken(_userDto);
            _authDto.Token = CreateJwtToken(_userDto);
            _authDto.IsAuthenticated = true;



            return await Task.FromResult(_authDto);
        }
        private string CreateJwtToken(UserDto user)
        {
            try
            {
                var symmetricSecurityKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwt.Key));
                var signingCredentials = new SigningCredentials(
                    symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

                var jti = Guid.NewGuid().ToString();

                // Create claim in the format JWTHelper expects: "UserEntity: {json}"
                string userJson = JsonConvert.SerializeObject(user);

                var claims = new List<Claim> {
            new Claim("UserEntity", userJson),
            new Claim(JwtRegisteredClaimNames.Sid, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        };

                // Check if this is the anonymous user
                bool isAnonymousUser = user.Email?.Equals("anonymous@nupco.com", StringComparison.OrdinalIgnoreCase) == true;

                // Set expiry based on user type
                DateTime expiry;
                if (isAnonymousUser)
                {
                    // Extended expiry for anonymous user (e.g., 30 days)
                    expiry = DateTime.Now.AddDays(30);
                    _logger.LogInformation($"Creating token for anonymous user with extended expiry: {expiry}");
                }
                else
                {
                    // Normal expiry for regular users
                    expiry = DateTime.Now.AddMinutes(_jwt.DurationInMins);
                }

                var token = new JwtSecurityToken(
                    issuer: _jwt.Issuer,
                    audience: _jwt.Audience,
                    claims: claims,
                    expires: expiry,
                    signingCredentials: signingCredentials);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // Log for debugging
                _logger.LogInformation($"Token created for user {user.Email} with claims: {string.Join(", ", claims.Select(c => $"{c.Type}"))}");

                // Session management - special handling for anonymous user
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!isAnonymousUser)
                    {
                        // For regular users: deactivate old sessions (single session)
                        var oldSessions = _context.UserSessions
                            .Where(x => x.UserId == user.Id && x.IsActive == true)
                            .ToList();

                        foreach (var session in oldSessions)
                        {
                            session.IsActive = false;
                            session.DeactivatedAt = DateTime.Now;
                            session.DeactivatedReason = "New login";
                        }
                    }
                    else
                    {
                        // For anonymous user: allow concurrent logins
                        // Optionally clean up very old anonymous sessions if needed
                        var veryOldSessions = _context.UserSessions
                            .Where(x => x.UserId == user.Id &&
                                   x.IsActive == true &&
                                   x.CreatedAt < DateTime.Now.AddDays(-60)) // Clean up sessions older than 60 days
                            .ToList();

                        foreach (var session in veryOldSessions)
                        {
                            session.IsActive = false;
                            session.DeactivatedAt = DateTime.Now;
                            session.DeactivatedReason = "Anonymous session older than 60 days";
                        }

                        _logger.LogInformation($"Anonymous user login - allowing concurrent sessions. Cleaned up {veryOldSessions.Count} very old sessions.");
                    }

                    // Add new session
                    _context.UserSessions.Add(new UserSession
                    {
                        UserId = user.Id,
                        SessionId = jti,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        ExpiresAt = expiry // Use the same expiry as token
                    });

                    _context.SaveChanges();
                    scope.Complete();
                }

                return Strings.CompressString(tokenString);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating JWT token: {ex.Message}");
                throw new ApplicationException("An error occurred while creating the JWT token.", ex);
            }
        }
        private async Task<string> GetTokenEmployee()
        {
            var values = new Dictionary<string, string>
                {
                    { "username",AttendanceUser },
                    { "password", AttendancePassword }
                };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(Api_Token_Auth, content);

            var responseString = await response.Content.ReadAsStringAsync();
            var _obj = System.Text.Json.JsonSerializer.Deserialize<JwtToken>(responseString);
            return _obj?.token;
        }

        public async Task<UserDto> GetUser(string id)
        {
            var _user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
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
                var _UserType = await _context.UserTypes.FirstOrDefaultAsync(i => i.Id == user.UserTypeId);
                user.UserTypeNameAr = _UserType?.NameAr;
                user.UserTypeNameEn = _UserType?.NameEn;

                return user;
            }

            return null;
        }

        private static string GetLogType(string? terminal_alias)
        {
            return terminal_alias.ToLower().Contains("in") ? "in" : "out";
        }

        public async Task<List<AttendanceTransactionResponseDto>> GetAttendanceTransactionsDataList(transactionAttendance attendance)
        {
            try
            {
                // Prepare date range
                var endDateTimeString = $"{attendance.endDate} 23:59:59.993";
                var startDate = Dates.ConvertStringToDate(attendance.startDate);
                var endDate = Dates.ConvertStringToDate(endDateTimeString);
                var user = await _context.Users.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(i => i.Id == attendance.userId || i.Code == attendance.empCode);

                // Fetch Attendance Transactions from AttendanceTransactions table
                var attendanceTransactions = await _context.AttendanceTransactions
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => i.UserId == user.Id && i.CreatedDate >= startDate && i.CreatedDate <= endDate)
                    .Select(s => new AttendanceTransactionResponseDto
                    {
                        Id = s.Id,
                        CreatedDate = s.CreatedDate,
                        punch_time = s.CreatedDate,
                        UserName = user.UserName,
                        UserCode = user.Code,
                        LogType = s.LogType
                    })
                    .ToListAsync();

                // Fetch Transactions from iclock_transaction table
                var iclockTransactions = await _contextAttendance.iclock_transaction
                    .AsNoTrackingWithIdentityResolution()
                    .Where(i => i.emp_code == attendance.empCode && i.punch_time.Date >= startDate.Value.Date && i.punch_time.Date <= endDate.Value.Date)
                    .Select(s => new AttendanceTransactionResponseDto
                    {
                        Id = s.id,
                        CreatedDate = s.punch_time,
                        punch_time = s.punch_time,
                        UserName = user.UserName,
                        UserCode = user.Code,
                        LogType = GetLogType(s.terminal_alias)
                    })
                    .ToListAsync();

                // Combine both lists

                var combinedTransactions = attendanceTransactions.Union(iclockTransactions).ToList();

                return combinedTransactions;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Client disconnected or request was canceled from GetAttendanceTransactions.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAttendanceTransactions: {ex.Message}");
                return null;
            }
        }


        public async Task<Response> GetEmployeeTodayAttendace(TransactionRequest modal)
        {
            //var _result = await _unitOfWork.Auths.GetEmployeeTransactions(modal, cancellationToken);
            // Map TransactionRequest to transactionAttendance
            //var attendanceRecord = new transactionAttendance(
            //    startDate: modal.startTime ?? throw new ArgumentException("Start time is required"),
            //    endDate: modal.endTime ?? modal.startTime,
            //    userId: null,
            //    empCode: modal.empCode ?? throw new ArgumentException("Employee code is required")
            //);

            var attendanceRecord = new transactionAttendance(
                startDate: modal.startTime,
                endDate: modal.endTime ?? modal.startTime,
                userId: null,
                empCode: modal.empCode
            );
            var response = new Response();

            if (attendanceRecord.startDate is not null && !string.IsNullOrEmpty(attendanceRecord.empCode))
            {
                var _result = await GetAttendanceTransactionsDataList(attendanceRecord);
                response.count = _result.Count();
                response.data.AddRange(_result);
            }



            return response;

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
                var expiry = DateTime.Now.AddMinutes(_jwt.DurationInMins);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expiry,
                    IssuedAt = DateTime.Now,
                    NotBefore = DateTime.Now,
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
                        session.DeactivatedAt = DateTime.Now;
                        session.DeactivatedReason = "New login";
                    }
                    var expiry = DateTime.Now.AddMinutes(_jwt.DurationInMins);

                    // Create new session
                    var newSession = new UserSession
                    {
                        UserId = userId,
                        SessionId = jti,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        ExpiresAt = expiry,
                        LastActivityAt = DateTime.Now
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


    }
}