using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FIG.Assessment;

/// <summary>
/// In this example, we are writing a service that will run (potentially as a windows service or elsewhere) and once a day will run a report on all new
/// users who were created in our system within the last 24 hours, as well as all users who deactivated their account in the last 24 hours. We will then
/// email this report to the executives so they can monitor how our user base is growing.
/// 
/// 
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio
/// https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service
/// </summary>
public class Example3
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(services =>
            {
                services.AddDbContext<MyContext>(options =>
                {
                    options.UseSqlServer("dummy-connection-string");
                });
                services.AddScoped<ReportEngine>();
                services.AddHostedService<DailyReportService>();
            })
            .Build()
            .Run();
    }
}

public class DailyReportService : BackgroundService
{
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<DailyReportService> _logger;

    public DailyReportService(IServiceScopeFactory factory, ILogger<DailyReportService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // when the service starts up, start by looking back at the last 24 hours
        var startingFrom = DateTime.Now.AddDays(-1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Initiating Report Generation...");
                // report engine is a service for this background service
                var scope = _factory.CreateScope();
                var reportEngine = scope.ServiceProvider.GetRequiredService<ReportEngine>();

                var newUsersTask = reportEngine.GetNewUsersAsync(startingFrom);
                var deactivatedUsersTask = reportEngine.GetDeactivatedUsersAsync(startingFrom);
                await Task.WhenAll(newUsersTask, deactivatedUsersTask); // run both queries in parallel to save time
                _logger.LogInformation("Successfully generated report");
                // send report to execs, .Result can be blocking, but we already awaited the tasks
                await SendUserReportAsync(await newUsersTask, await deactivatedUsersTask);
                _logger.LogInformation("Successfully sent user report");

                // save the current time, wait until next midnight, and run the report again - using the new cutoff date
                startingFrom = DateTime.Now;
                var nextMidnight = startingFrom.Date.AddDays(1) - startingFrom;
                await Task.Delay(nextMidnight, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in report service");
            }
        }
    }

    private Task SendUserReportAsync(IEnumerable<User> newUsers, IEnumerable<User> deactivatedUsers)
    {
        try
        {
            // not part of this example
            return Task.CompletedTask;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error sending report");
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// A dummy report engine that runs queries and returns results.
/// The queries here a simple but imagine they might be complex SQL queries that could take a long time to complete.
/// </summary>
public class ReportEngine
{
    private readonly MyContext _db;
    private readonly ILogger _logger;

    public ReportEngine(MyContext db, ILogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<User>> GetNewUsersAsync(DateTime startingFrom)
    {
        try
        {
            var newUsers = await this._db.Users
                .Where(u => u.CreatedAt > startingFrom)
                .ToListAsync();
            return newUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving new users");
            return Enumerable.Empty<User>();
        }
    }

    public async Task<IEnumerable<User>> GetDeactivatedUsersAsync(DateTime startingFrom)
    {
        try
        {
            var deactivatedUsers = await this._db.Users
                .Where(u => u.DeactivatedAt > startingFrom)
                .ToListAsync();
            return deactivatedUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deactivated users");
            return Enumerable.Empty<User>();
        }
    }
}

#region Database Entities
// a dummy EFCore dbcontext - not concerned with actually setting up connection strings or configuring the context in this example
public class MyContext : DbContext
{
    public DbSet<User> Users { get; set; }
}

public class User
{
    public int UserId { get; set; }

    public string UserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeactivatedAt { get; set; }
}
#endregion