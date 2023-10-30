using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Anevo.Data;
using System.Net;
using RestSharp;
using Nancy.Json;
using Anevo.Enums.SU0010;
using Anevo.Actions.Groups;
using Anevo.Models;
using Anevo.Models.Tables.User;
using Anevo.Actions.Users;
using Microsoft.EntityFrameworkCore;
using Anevo.Actions.JWT;
using Anevo.Models.JWT;
using Anevo.Interfaces.JWT;

namespace Anevo.Controllers;

[Authorize]
[ApiController]
[Route("api/User")]
public class UserController : ControllerBase
{
    private readonly JWTSettings _options;
    private readonly ILogger<UserController> _logger;
    private UserActions _userActions;
    private UserGroups _userGroups;
    private readonly ApplicationContext _context;
    private readonly ITokenService _tokenService;
    private readonly IOptions<JWTSettings> _config;

    public UserController(ILogger<UserController> logger,IOptions<JWTSettings> optAccess,ApplicationContext context, ITokenService tokenService, IOptions<JWTSettings> config)
    {
        _logger = logger;
        _options = optAccess.Value;
        _context = context;
        _userActions = new UserActions(_context);
        _userGroups = new UserGroups(_context);
        _tokenService = tokenService;
        _config = config;
    }

    [HttpGet]
    [Authorize(Roles = UserRolesTemplate.Admin)]
    [Route("GetUsers")]
    public async Task<List<SU_001>> GetUsers()
    {
        return await _userActions.GetUsers();
    }
    
    [HttpGet("GetUserByEmail")]
    public async Task<SU_001> GetUserByEmail(string email)
    {
        return await _userActions.GetUserByEmail(email);
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("Register")]
    public async Task<ActionResult> Register(SU_001 user)
    {
        var find_user = await _userActions.GetUserByEmail(user.Email);
        if (find_user == null)
        {
            await _userActions.CreateUser(user);
            LoginTemplate loginTemplate = new LoginTemplate();
            loginTemplate.SU_001 = user;
            find_user = await _userActions.GetUserByEmail(user.Email);
            await _userGroups.AddUserToGroup(loginTemplate.SU_001,SU010_Types.User);
            var jwt_resp = Login(loginTemplate.SU_001).Result.ExecuteResultAsync;
            var resp = (ContentResult)jwt_resp.Target;
            return Content(resp.Content.ToString());
        }
        else
        {
            return StatusCode(208);
        }
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("Login")]
    public async Task<ActionResult> Login(SU_001 user)
    {
        LoginTemplate login_template = new LoginTemplate();
        var find_user = await _userActions.GetUserByEmail(user.Email);
        if (find_user != null)
        {
            login_template.SU_001 = find_user;
            var user_group = await _userGroups.GetUserInGroup(find_user.Id);
            var group_data = await _userGroups.GetGroup(user_group.SG001_GroupNr);
            login_template.SG_010 = group_data;
            login_template.SG_001 = user_group;
            CreateJWTToken cjwttoken = new CreateJWTToken(_config);
            AuthenticatedResponse aresp = new AuthenticatedResponse();
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, find_user.Email),
                new Claim(ClaimTypes.GroupSid, login_template.SG_001.SG001_GroupNr.ToString()), 
                new Claim(ClaimTypes.Role, login_template.SG_010.SU010_Name) 
            };
            aresp.Token = cjwttoken.GenerateAccessToken(claims);
            aresp.RefreshToken = cjwttoken.GenerateRefreshToken();
            find_user.RefreshToken = aresp.RefreshToken;
            find_user.RefreshTokenExpiryTime = DateTime.Now.AddMinutes(1440);
            await _context.SaveChangesAsync();
            return Ok(aresp);
        }
        else
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<IActionResult> Refresh(TokenApiModel tokenApiModel)
    {
        if (tokenApiModel is null)
            return BadRequest("Invalid client request");
        string accessToken = tokenApiModel.AccessToken;
        string refreshToken = tokenApiModel.RefreshToken;
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        var email = principal.Identity.Name;
        var user = _userActions.GetUserByEmail(email).Result;
        if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            return BadRequest("Invalid client request");
        var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        await _context.SaveChangesAsync();
        return Ok(new AuthenticatedResponse()
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
    [HttpPost]
    [Route("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var email = User.Identity.Name;
        var user = _userActions.GetUserByEmail(email).Result;
        if (user == null) return BadRequest();
        user.RefreshToken = null;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
