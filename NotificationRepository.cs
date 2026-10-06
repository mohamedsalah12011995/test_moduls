
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Nupco.DAL;
using Nupco.Core.Interfaces;
using Nupco.Core.Assembler;
using Nupco.Core.Dto;
using Nupco.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using System.Net;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.IdentityModel.Tokens;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.Auth.OAuth2;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using System.Runtime.InteropServices;

namespace Nupco.EF.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        //public List<NotificationWithLanaguge> deviceIds = new List<NotificationWithLanaguge>();
        //public List<NotificationWithLanaguge> deviceWebIds = new List<NotificationWithLanaguge>();
        private readonly ApplicationDbContext _context;

        private readonly ILogger _logger;



        private readonly Notification_Assembler _notification_Assembler;

        // Path to your service account key JSON file
        private readonly string portalSite;
        private readonly string ServiceAccountKeyPath;// =>  "C:\\Mahmoud\\Nupco\\nupco-firebase-service.json";
        private readonly string ServiceDriverKeyPath;// =>  "C:\\Mahmoud\\Nupco\\nupco-driver-firebase.json";

        // The required scope for Firebase Cloud Messaging
        private const string FCM_SCOPE = "https://www.googleapis.com/auth/firebase.messaging";
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IConfiguration _configuration;

        private const string EmployeeFirebaseAppName = "nupco-employee";
        private const string DriverFirebaseAppName = "nupco-driver";
        private static readonly object FirebaseAppLock = new();

        /// <summary>
        /// FCM <see cref="FirebaseAdmin.Messaging.Notification.ImageUrl"/> must be a valid HTTP(S) URL.
        /// Callers often pass placeholders like "0" or relative paths; those throw <c>Malformed image URL</c>.
        /// </summary>
        private static string? NormalizeFcmNotificationImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;
            var t = imageUrl.Trim();
            if (t == "0" || t.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            if (!t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return null;
            return t;
        }

        public NotificationRepository(ApplicationDbContext context, ILogger logger, IConfiguration configuration) 
        {
            _notification_Assembler = new Notification_Assembler();
            _logger = logger;
            _context = context;
            ServiceAccountKeyPath = ResolveEmployeeServiceAccountPath(configuration);
            ServiceDriverKeyPath = ResolveDriverServiceAccountPath(configuration);
            portalSite = configuration["Domains:portal"];
            _configuration = configuration;

            _logger.LogInformation(
                "FCM employee service account path={EmployeePath}, exists={EmployeeExists}; driver path={DriverPath}, exists={DriverExists}",
                ServiceAccountKeyPath,
                !string.IsNullOrWhiteSpace(ServiceAccountKeyPath) && System.IO.File.Exists(ServiceAccountKeyPath),
                ServiceDriverKeyPath,
                !string.IsNullOrWhiteSpace(ServiceDriverKeyPath) && System.IO.File.Exists(ServiceDriverKeyPath));
        }

        private static string? ResolveEmployeeServiceAccountPath(IConfiguration configuration) =>
            FirstNonEmpty(
                configuration["FcmNotification:firbaseServicePath"],
                configuration["FcmNotification:firebaseServicePath"],
                configuration["FcmNotification:FirebaseServicePath"]);

        private static string? ResolveDriverServiceAccountPath(IConfiguration configuration) =>
            FirstNonEmpty(
                configuration["FcmNotification:driverFirbaseServicePath"],
                configuration["FcmNotification:driverFirebaseServicePath"]);

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        private FirebaseMessaging GetEmployeeMessaging() =>
            FirebaseMessaging.GetMessaging(GetOrCreateFirebaseApp(EmployeeFirebaseAppName, ServiceAccountKeyPath, "employee"));

        private FirebaseMessaging GetDriverMessaging() =>
            FirebaseMessaging.GetMessaging(GetOrCreateFirebaseApp(DriverFirebaseAppName, ServiceDriverKeyPath, "driver"));

        private FirebaseApp GetOrCreateFirebaseApp(string appName, string? keyPath, string appKind)
        {
            lock (FirebaseAppLock)
            {
                var existing = TryGetFirebaseApp(appName);
                if (existing != null)
                    return existing;

                if (string.IsNullOrWhiteSpace(keyPath))
                    throw new InvalidOperationException($"FCM {appKind} service path is not configured.");

                if (!System.IO.File.Exists(keyPath))
                    throw new InvalidOperationException($"FCM {appKind} key file not found at {keyPath}");

                _logger.LogInformation(
                    "Creating named FirebaseApp {AppName} from {Kind} key path {Path}",
                    appName, appKind, keyPath);

                try
                {
                    var created = FirebaseApp.Create(
                        new AppOptions { Credential = GoogleCredential.FromFile(keyPath) },
                        appName);

                    if (created == null)
                        throw new InvalidOperationException($"FirebaseApp.Create returned null for {appName}.");

                    return created;
                }
                catch (ArgumentException)
                {
                    // Created by another thread between TryGet and Create.
                    existing = TryGetFirebaseApp(appName);
                    if (existing != null)
                        return existing;
                    throw;
                }
            }
        }

        /// <summary>
        /// FirebaseAdmin GetInstance(name) returns null when the named app does not exist
        /// (it does not always throw). Never pass that null into FirebaseMessaging.GetMessaging.
        /// </summary>
        private static FirebaseApp? TryGetFirebaseApp(string appName)
        {
            try
            {
                return FirebaseApp.GetInstance(appName);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<NotificationWithLanaguge>> GetDeviceIDsAllUsers()
        {
            var deviceIds = new List<NotificationWithLanaguge>();

            try
            {
                deviceIds = await _context.Users
                   .Include(i => i.UserSettings)
                   .Where(d => !String.IsNullOrEmpty(d.NotificationToken) && d.UserSettings != null && d.UserSettings.FirstOrDefault().AllowPushNotification == true)
                   .Select(d => new NotificationWithLanaguge(d.NotificationToken, d.UserSettings.FirstOrDefault().Language)).ToListAsync();


                // Retrieve all users from AspNetUsers table using DbSet directly
                //var users = await _context.Users.Where(i => i.Email== "msmohamed-c@nupco.com")
                //     .Include(i => i.UserSettings).Where(d => !String.IsNullOrEmpty(d.NotificationToken) && d.UserSettings != null && d.UserSettings.FirstOrDefault().AllowPushNotification == true).ToListAsync();
                //deviceIds = users.Select(d => new NotificationWithLanaguge(d.NotificationToken,d.UserSettings.FirstOrDefault().Language)).ToList();

            }
            catch (Exception )
            {
                // Log the exception or handle it in an appropriate way
                // You might want to throw the exception further up the call stack if it cannot be handled here.
                throw;
            }
            return deviceIds;
        }
        public async Task<List<NotificationWithLanaguge>> GetDeviceIDsPortlaAllUsers()
        {
            var deviceWebIds = new List<NotificationWithLanaguge>();

            try
            {
                deviceWebIds = await _context.Users
                   .Include(i => i.UserSettings)
                   .Where(d => !String.IsNullOrEmpty(d.NotificationTokenWeb) && d.UserSettings != null && d.UserSettings.FirstOrDefault().AllowPushNotification == true)
                   .Select(d => new NotificationWithLanaguge(d.NotificationTokenWeb, d.UserSettings.FirstOrDefault().Language)).ToListAsync();

            }
            catch (Exception )
            {
                // Log the exception or handle it in an appropriate way
                // You might want to throw the exception further up the call stack if it cannot be handled here.
                throw;
            }
            return deviceWebIds;
        }

        public async Task<List<NotificationWithLanaguge>> GetDeviceIDsAllUsers(List<string> usersIds)
        {
            var deviceIds = new List<NotificationWithLanaguge>();
            try
            {
                foreach (var userId in usersIds)
                {
                    var user = await _context.Users.Include(i => i.UserSettings)
                   .FirstOrDefaultAsync(d => d.Id == userId && !String.IsNullOrEmpty(d.NotificationToken) && d.UserSettings != null && d.UserSettings.FirstOrDefault().AllowPushNotification == true);

                    if (user != null)
                    {
                        NotificationWithLanaguge notificationWithLanaguge = new NotificationWithLanaguge(user.NotificationToken, user.UserSettings?.FirstOrDefault()?.Language);
                        deviceIds.Add(notificationWithLanaguge);
                    }
                }
                // Retrieve all users from AspNetUsers table using DbSet directly

            }
            catch (Exception )
            {
                // Log the exception or handle it in an appropriate way
                // You might want to throw the exception further up the call stack if it cannot be handled here.
                throw;
            }
            return deviceIds;
        }
        public async Task<List<NotificationWithLanaguge>> GetDeviceIDsAllUsersPortal(List<string> usersIds)
        {
            var deviceWebIds = new List<NotificationWithLanaguge>();
            try
            {
                foreach (var userId in usersIds)
                {
                    var user = await _context.Users.Include(i => i.UserSettings)
                   .FirstOrDefaultAsync(d => d.Id == userId && !String.IsNullOrEmpty(d.NotificationTokenWeb) && d.UserSettings != null && d.UserSettings.FirstOrDefault().AllowPushNotification == true);

                    if (user != null)
                    {
                        NotificationWithLanaguge notificationWithLanaguge = new NotificationWithLanaguge(user.NotificationTokenWeb, user.UserSettings?.FirstOrDefault()?.Language);
                        deviceWebIds.Add(notificationWithLanaguge);
                    }
                }
                // Retrieve all users from AspNetUsers table using DbSet directly

            }
            catch (Exception)
            {
                // Log the exception or handle it in an appropriate way
                // You might want to throw the exception further up the call stack if it cannot be handled here.
                throw;
            }
            return deviceWebIds;
        }



        //public async Task<List<string>> GetDeviceIDsByDepartment(string department)
        //{
        //    try
        //    {
        //        deviceIds =await _context.Users.Include(i => i.UserSettings)
        //        .Where(d => d.DepartmentName.ToLower() == department.ToLower() && d.UserSettings.FirstOrDefault().AllowPushNotification == true)
        //        .Select(d => d.NotificationToken)
        //        .Where(d => !string.IsNullOrEmpty(d))
        //        .ToListAsync();
        //        return deviceIds;
        //    }
        //    catch (Exception )
        //    {
        //        return null;
        //    }

        //    return deviceIds;
        //}

        //public async Task< List<string>> GetDeviceIDsBySelectedUser(string userName)
        //{
        //    try
        //    {
        //        deviceIds =await _context.Users.Include(i=>i.UserSettings)
        //        .Where(d => d.UserName.ToLower() == userName.ToLower()&&d.UserSettings.FirstOrDefault().AllowPushNotification==true)
        //        .Select(d => d.NotificationToken)
        //        .Where(d => !string.IsNullOrEmpty(d))
        //        .ToListAsync();
        //        return  deviceIds;
        //    }
        //    catch (Exception )
        //    {
        //    }

        //    return deviceIds;
        //}


        public async Task<string> SendNotification(
            List<NotificationWithLanaguge> deviceIDs,
            string SERVER_API_KEY,
            string page,
            string index,
            string titleAr,
            string titleEn,
            string bodyAr,
            string bodyEn,
            string imageUrl = null)
        {
            return await SendFcmMulticastMobileInternalAsync(deviceIDs, page, index, titleAr, titleEn, bodyAr, bodyEn, imageUrl);
        }



















        public async Task<string> SendNotificationMobile(List<NotificationWithLanaguge> deviceIDs, string SERVER_API_KEY, string page, string index, string titleAr, string titleEn, string bodyAr, string bodyEn, [Optional] string imageUrl)
        {
            return await SendFcmMulticastMobileInternalAsync(deviceIDs, page, index, titleAr, titleEn, bodyAr, bodyEn, imageUrl);
        }

        /// <summary>
        /// Sends FCM to native app tokens (<see cref="User.NotificationToken"/>).
        /// Web push uses <see cref="User.NotificationTokenWeb"/> via <see cref="SendNotificationWeb"/> — a user may have only one of them populated.
        /// </summary>
        private async Task<string> SendFcmMulticastMobileInternalAsync(
            List<NotificationWithLanaguge> deviceIDs,
            string page,
            string index,
            string titleAr,
            string titleEn,
            string bodyAr,
            string bodyEn,
            string imageUrl)
        {
            var failedTokens = new List<string>();

            if (deviceIDs == null || deviceIDs.Count == 0)
                return "No device tokens provided.";

            var uniqueDevices = deviceIDs
                .Where(d => !string.IsNullOrEmpty(d.notificationToken))
                .GroupBy(d => d.notificationToken!)
                .Select(g => g.First())
                .ToList();

            if (uniqueDevices.Count == 0)
                return "No device tokens provided.";

            var data = new Dictionary<string, string>
            {
                { "page", page ?? "" },
                { "index", index ?? "" }
            };

            const int batchSize = 500;

            foreach (var langGroup in uniqueDevices.GroupBy(d => d.lanaguge?.ToLower() ?? "en"))
            {
                string lang = langGroup.Key;
                var notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = lang == "ar" ? titleAr : titleEn,
                    Body = lang == "ar" ? bodyAr : bodyEn,
                    ImageUrl = NormalizeFcmNotificationImageUrl(imageUrl)
                };

                var devicesInLang = langGroup.ToList();
                var batchCount = (int)Math.Ceiling(devicesInLang.Count / (decimal)batchSize);

                for (var b = 0; b < batchCount; b++)
                {
                    var tokens = devicesInLang
                        .Skip(b * batchSize)
                        .Take(batchSize)
                        .Select(d => d.notificationToken!)
                        .Where(t => !string.IsNullOrEmpty(t))
                        .Distinct()
                        .ToList();

                    if (tokens.Count == 0)
                        continue;

                    var message = new MulticastMessage
                    {
                        Tokens = tokens,
                        Notification = notification,
                        Data = data
                    };

                    try
                    {
                        var messaging = GetEmployeeMessaging();
                        _logger.LogInformation(
                            "SendNotification (employee): using FirebaseApp {AppName}, page={Page}, tokenCount={TokenCount}",
                            EmployeeFirebaseAppName, page, tokens.Count);

                        var response = await messaging.SendEachForMulticastAsync(message);
                        if (response.FailureCount > 0)
                        {
                            for (var k = 0; k < response.Responses.Count; k++)
                            {
                                if (!response.Responses[k].IsSuccess)
                                    failedTokens.Add(tokens[k]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "FCM mobile multicast failed");
                        return $"Error sending notification: {ex.Message}";
                    }
                }
            }

            if (failedTokens.Count > 0)
                return $"Some tokens failed: {string.Join(", ", failedTokens)}";
            return "All notifications sent successfully!";
        }


        public async Task<string> SendNotificationWeb(
            List<NotificationWithLanaguge> deviceIDs,
            string SERVER_API_KEY,
            string page,
            string index,
            string titleAr,
            string titleEn,
            string bodyAr,
            string bodyEn,
            string imageUrl = null)
        {
            var failedTokens = new List<string>();

            if (deviceIDs == null || deviceIDs.Count == 0)
                return "No device tokens provided.";

            // 👇 تصفية التوكنز بحيث يكون كل توكن مرة واحدة فقط
            var uniqueDevices = deviceIDs
                .Where(d => !string.IsNullOrEmpty(d.notificationToken))
                .GroupBy(d => d.notificationToken)
                .Select(g => g.First())
                .ToList();

            // 👀 للتأكد اطبع عدد التوكنز الفعلي
            Console.WriteLine($"Unique Tokens Count: {uniqueDevices.Count}");

            int batchSize = 500;
            int totalBatches = (int)Math.Ceiling((decimal)uniqueDevices.Count / batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = uniqueDevices
                    .Skip(i * batchSize)
                    .Take(batchSize)
                    .ToList();

                // 👇 تقسيم حسب اللغة
                var groupedByLang = batch.GroupBy(d => d.lanaguge?.ToLower() ?? "en");

                foreach (var group in groupedByLang)
                {
                    string lang = group.Key;
                    string title = lang == "ar" ? titleAr : titleEn;
                    string body = lang == "ar" ? bodyAr : bodyEn;

                    var tokens = group.Select(d => d.notificationToken).Distinct().ToList();

                    Console.WriteLine($"Sending {tokens.Count} notifications in {lang}...");

                    var message = new MulticastMessage()
                    {
                        Tokens = tokens,
                        Data = new Dictionary<string, string>
    {
        { "title", title },
        { "body", body },
        { "image", imageUrl ?? portalSite + "/assets/nupconeersLogo.png" },
        { "page", page },
        { "index", index },
        { "url", portalSite },
        { "icon", portalSite + "assets/nupconeersLogo.png" },
        { "click_action", portalSite }
    }
                    };

                    try
                    {
                        var messaging = GetEmployeeMessaging();
                        _logger.LogInformation(
                            "SendNotificationWeb (employee): using FirebaseApp {AppName}, page={Page}, tokenCount={TokenCount}",
                            EmployeeFirebaseAppName, page, tokens.Count);

                        var response = await messaging.SendEachForMulticastAsync(message);

                        for (int k = 0; k < response.Responses.Count; k++)
                        {
                            if (!response.Responses[k].IsSuccess)
                            {
                                failedTokens.Add(tokens[k]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return $"Error sending notification: {ex.Message}";
                    }
                }
            }

            return failedTokens.Count > 0
                ? $"Some tokens failed: {string.Join(", ", failedTokens)}"
                : "All notifications sent successfully!";
        }








        public async Task<string> GetAccessTokenAsync()
        {
            GoogleCredential credential;

            // Load the service account credentials from the JSON key file
            using (var stream = new FileStream(ServiceAccountKeyPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                                             .CreateScoped(FCM_SCOPE);
            }

            // Request an access token for FCM
            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            return accessToken;
        }





        public async Task<List<NotificationDto>> GetNotificationsByUsersId(string userId)
        {
            return _notification_Assembler.WriteListDto(await _context.Notifications.Where(x => x.UserId == userId).ToListAsync());

        }

        public async Task<bool> DeleteNotification(int id)
        {
            var Exite = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id);
            if (Exite != null)
            {
                _context.Remove(Exite);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }


        public async Task<string> SendNotificationToDrivers(
    List<NotificationWithLanaguge> deviceIDs,
    string SERVER_API_KEY,
    string page,
    string index,
    string titleAr,
    string titleEn,
    string bodyAr,
    string bodyEn,
    string imageUrl = null)
        {
            if (deviceIDs == null || deviceIDs.Count == 0)
            {
                _logger.LogWarning("SendNotificationToDrivers: no device IDs provided");
                return "No device tokens provided.";
            }

            _logger.LogInformation(
                "SendNotificationToDrivers: start. deviceCount={DeviceCount}, page={Page}, index={Index}, driverKeyPathConfigured={KeyConfigured}",
                deviceIDs.Count,
                page,
                index,
                !string.IsNullOrWhiteSpace(ServiceDriverKeyPath));

            // Prepare a list of device tokens for the multicast message
            var failedTokens = new List<string>();
            var failureReasons = new List<string>();
            // Create a notification payload (you can use device language here)
            var notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = deviceIDs[0].lanaguge?.ToLower() == "ar" ? titleAr : titleEn, // Assume language based on the first device, can be adjusted
                Body = deviceIDs[0].lanaguge?.ToLower() == "ar" ? bodyAr : bodyEn,
                ImageUrl = NormalizeFcmNotificationImageUrl(imageUrl)
            };

            // Add custom data payload (optional)
            var data = new Dictionary<string, string>
        {
            { "page", page },
            { "index", index }
        };
            var _times = Math.Ceiling((Decimal)deviceIDs.Count / 500);
            for (int i = 0; i < _times; i++)
            {


                var tokens = new List<string>();
                int _end = (i * 500) + 500 > deviceIDs.Count ? deviceIDs.Count : (i * 500) + 500;
                for (int j = i * 500; j < _end; j++)
                {
                    var deviceID = deviceIDs[j];
                    if (deviceID.notificationToken != null)
                    {
                        tokens.Add(deviceID.notificationToken);
                    }
                }

                if (tokens.Count == 0)
                {
                    _logger.LogWarning("SendNotificationToDrivers: batch {Batch} has no tokens", i);
                    continue;
                }

                _logger.LogInformation(
                    "SendNotificationToDrivers: sending batch {Batch}, tokenCount={TokenCount}, title={Title}",
                    i, tokens.Count, notification.Title);




                // Create a multicast message with notification and data payloads
                var message = new MulticastMessage()
                {
                    Tokens = tokens,
                    Notification = notification,
                    Data = data
                };

                try
                {
                    var messaging = GetDriverMessaging();
                    _logger.LogInformation(
                        "SendNotificationToDrivers: using FirebaseApp {AppName}",
                        DriverFirebaseAppName);

                    var response = await messaging.SendEachForMulticastAsync(message);

                    _logger.LogInformation(
                        "SendNotificationToDrivers: batch {Batch} FCM response SuccessCount={SuccessCount}, FailureCount={FailureCount}",
                        i, response.SuccessCount, response.FailureCount);

                    // Process the response, return success or failure
                    if (response.FailureCount > 0)
                    {

                        for (int k = 0; k < response.Responses.Count; k++)
                        {
                            if (!response.Responses[k].IsSuccess)
                            {
                                // Collect the failed tokens
                                failedTokens.Add(MaskToken(tokens[k]));
                                var err = response.Responses[k].Exception?.Message ?? "unknown";
                                failureReasons.Add($"{MaskToken(tokens[k])}: {err}");
                                _logger.LogWarning(
                                    "SendNotificationToDrivers: token failed. Token={Token}, Error={Error}",
                                    MaskToken(tokens[k]),
                                    err);
                            }
                        }

                    }

                    //
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SendNotificationToDrivers: exception while sending batch {Batch}", i);
                    return $"Error sending notification: {ex.Message}";
                }
            }
            if (failedTokens.Count > 0)
            {
                var summary = $"Some tokens failed: {string.Join(" | ", failureReasons)}";
                _logger.LogWarning("SendNotificationToDrivers: {Summary}", summary);
                return summary;
            }

            _logger.LogInformation("SendNotificationToDrivers: all notifications sent successfully");
            return "All notifications sent successfully!";
        }

        private static string MaskToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return "(empty)";
            if (token.Length <= 12)
                return "***";
            return $"{token.Substring(0, 6)}...{token.Substring(token.Length - 4)}";
        }




    }
}