using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FIG.Assessment;
/// <summary>
///  .net pw hasher, much better than md5 hashing their inpuy
/// the intial password needs to be hashed with the same tool
/// https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-9.0
/// https://learn.microsoft.com/en-us/aspnet/mvc/overview/security/preventing-open-redirection-attacks
/// </summary>
public class Example2 : Controller
{

    private readonly PasswordHasher<string> _hasher = new();
    private readonly Example2Context _dbContext;

    public Example2(Example2Context dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromForm] LoginPostModel model)
    {
        var user = await _dbContext.Example2Users.SingleOrDefaultAsync(u => u.UserName == model.UserName);

        // first check user exists by the given username
        if (user == null)
        {
            return this.Redirect("/Error?msg=invalid_username");
        }

        // then check password is correct
        var verifyStatus = _hasher.VerifyHashedPassword(user.UserName, user.HashedPassword, model.Password);
        if (verifyStatus == PasswordVerificationResult.Failed)
        {
            return this.Redirect("/Error?msg=invalid_password");
        }

        // if we get this far, we have a real user. sign them in
        var userId = user.Id;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        await this.HttpContext.SignInAsync(principal);

        // check return url to be safe, don't allow external return urls
        if (Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }
}

// require username/password, if returnurl is null or invalid, new IsLocalUrl call will catch it
public class LoginPostModel
{
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

// this context and model are here for sake of this example
public class Example2Context : DbContext
{
    private IConfiguration _config;
    public Example2Context(IConfiguration config) => _config = config;
    public DbSet<Example2User> Example2Users { get; set; }

    // for the sake of this example, we do this here, but this ideally would be outside of this file (like in example 3)
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_config.GetConnectionString("SQL"));
    }
}
// example model 
public class Example2User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string HashedPassword { get; set; } = string.Empty;
}