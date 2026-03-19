using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApit4s.DAL;
using WebApit4s.Models;

namespace WebApit4s.WebApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonalDetailsController : ControllerBase
    {
        private readonly TimeContext _context;

        public PersonalDetailsController(TimeContext context)
        {
            _context = context;
        }

        // GET: api/PersonalDetails
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonalDetails>>> GetPersonalDetails()
        {
            return await _context.PersonalDetails.ToListAsync();
        }

        // GET: api/PersonalDetails/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonalDetails>> GetPersonalDetails(int id)
        {
            var personalDetails = await _context.PersonalDetails.FindAsync(id);

            if (personalDetails == null)
            {
                return NotFound();
            }

            return personalDetails;
        }

        // PUT: api/PersonalDetails/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPersonalDetails(int id, PersonalDetails personalDetails)
        {
            if (id != personalDetails.Id)
            {
                return BadRequest();
            }

            _context.Entry(personalDetails).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonalDetailsExists(id))
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

        // POST: api/PersonalDetails
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PersonalDetails>> PostPersonalDetails(PersonalDetails personalDetails)
        {
            _context.PersonalDetails.Add(personalDetails);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPersonalDetails", new { id = personalDetails.Id }, personalDetails);
        }

        // DELETE: api/PersonalDetails/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonalDetails(int id)
        {
            var personalDetails = await _context.PersonalDetails.FindAsync(id);
            if (personalDetails == null)
            {
                return NotFound();
            }

            _context.PersonalDetails.Remove(personalDetails);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PersonalDetailsExists(int id)
        {
            return _context.PersonalDetails.Any(e => e.Id == id);
        }
    }
}
