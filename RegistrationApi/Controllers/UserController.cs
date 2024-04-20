using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegistrationApi.Context;
using RegistrationApi.Models;
using static BCrypt.Net.BCrypt;

namespace RegistrationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationContext _context;
        private readonly IConfiguration _configuration;
        private readonly int salt;


        private User UserDtoToUser(UserDto entity)
        {
            return new User()
            { 
                Name = entity.Name,
                Email = entity.Email,
                Password = entity.Password,
            };
        }

        private UserDto UserToUserDto(User entity)
        {
            return new UserDto()
            {
                Name = entity.Name,
                Email = entity.Email,
                Password = entity.Password,
            };
        }

        public UserController(ApplicationContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            this.salt = configuration.GetValue<int>("Secret:Salt");
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
          if (_context.Users == null)
          {
              return NotFound();
          }
            return Ok((await _context.Users.ToListAsync()).Select(x => UserToUserDto(x)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
          if (_context.Users == null)
          {
              return NotFound();
          }
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(UserToUserDto(user));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(Guid id, [FromBody]UserDto userDto)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == id);

            user.Email = userDto.Email;
            user.Password = HashPassword(userDto.Password, salt);
            user.Name = userDto.Name;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> PostUser([FromBody]UserDto userDto)
        {
          if (_context.Users == null)
          {
              return Problem("Users are null.");
          }
            userDto.Password = HashPassword(userDto.Password, salt);
            User user = UserDtoToUser(userDto);
            
            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (UserExists(user.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("check-password")]
        public async Task<IActionResult> CheckUserPassword([FromBody] LoginDto entity)
        {
            try
            {
                var user = await GetUserByEmail(entity.Email);
                var result = Verify(entity.Password, user.Password);
                if (result) {
                    return Ok("Success!!!");
                }
                else
                {
                    return BadRequest("Invalid password!!!");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        private bool UserExists(Guid id)
        {
            return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<User> GetUserByEmail(string email)
        {
            return _context.Users?.Where(e => e.Email == email).First();
        }
    }
}
