﻿using JwtAuth.Core.Dto;
using JwtAuth.Core.Entities;
using JwtAuth.Core.OtherObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        //Route for seeding my roles to DB

        [HttpPost]
        [Route("seed-roles")]
        public async Task<IActionResult> SeedRoles() {

            bool isOwnerRoleExists = await _roleManager.RoleExistsAsync(StaticUserRoles.OWNER);
            bool isAdminRoleExists = await _roleManager.RoleExistsAsync(StaticUserRoles.ADMIN);
            bool isUserRoleExists = await _roleManager.RoleExistsAsync(StaticUserRoles.USER);

            if (isOwnerRoleExists && isAdminRoleExists && isUserRoleExists)
                return Ok("Seeding roles is already done");

            await _roleManager.CreateAsync(new IdentityRole(StaticUserRoles.USER));
            await _roleManager.CreateAsync(new IdentityRole(StaticUserRoles.ADMIN));
            await _roleManager.CreateAsync(new IdentityRole(StaticUserRoles.OWNER));

            return Ok("Role Seeding Done Successfully");
        }

        //Route -> Register
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody]RegisterDto registerDto) {

            var isExistsUser = await _userManager.FindByNameAsync(registerDto.UserName);

            if (isExistsUser != null)
                return BadRequest("Username already exists");

            ApplicationUser newUser = new ApplicationUser()
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.UserName,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var createUserResult = await _userManager.CreateAsync(newUser, registerDto.Password);

            if (!createUserResult.Succeeded) {
                var errorString = "User Creation Failed Because: ";
                foreach (var error in createUserResult.Errors) {
                    errorString += "# " + error.Description;
                }
                return BadRequest(errorString);
            }
            //Add a default user role to all new registered useres
            await _userManager.AddToRoleAsync(newUser, StaticUserRoles.USER);
            return Ok("User Created Successfully");
        }


        //Route -> Login
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto) 
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user is null)
                return Unauthorized("Invalid Credentials");

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if(!isPasswordCorrect)
                return Unauthorized("Invalid Credentials");

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim> {
                new Claim(ClaimTypes.Name , user.UserName),
                new Claim(ClaimTypes.NameIdentifier , user.Id),
                new Claim("JWTID",Guid.NewGuid().ToString()),
                new Claim("FirstName",user.FirstName),
                new Claim("LastName",user.LastName),
            };

            foreach (var userRole in userRoles) 
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GenerateNewJsonWebToken(authClaims);

            return Ok(token);  


        }
        private string GenerateNewJsonWebToken(List<Claim> claims) 
        {
            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var tokenObject = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(1),
                claims: claims,
                signingCredentials: new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256)
                );

            string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);

            return token;
        }


        //Route -> make user = admin
        [HttpPost]
        [Route("make-admin")]
        public async Task<IActionResult> MakeAdmin([FromBody]UpdatePermissionsDto updatePermissionsDto) 
        {
            var user = await _userManager.FindByNameAsync(updatePermissionsDto.UserName);
            if (user is null)
                return BadRequest("Invalid UserName");

            await _userManager.AddToRoleAsync(user, StaticUserRoles.ADMIN);
            return Ok("User is now an Admin");
        }

        //Route -> make user = owner
        [HttpPost]
        [Route("make-owner")]
        public async Task<IActionResult> MakeOwner([FromBody] UpdatePermissionsDto updatePermissionsDto)
        {
            var user = await _userManager.FindByNameAsync(updatePermissionsDto.UserName);
            if (user is null)
                return BadRequest("Invalid UserName");

            await _userManager.AddToRoleAsync(user, StaticUserRoles.OWNER);
            return Ok("User is now an Owner");
        }
    }
}

