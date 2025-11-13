using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entity;
using Service.Interface;

namespace PRN232_SU25_SE185118.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeopardProfilesController : ControllerBase
    {
        private readonly ILeopardProfileService _service;

        public LeopardProfilesController(ILeopardProfileService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "administrator,moderator,developer,member")]
        public IActionResult GetAll() => Ok(_service.GetAll());

        [HttpGet("{id}")]
        [Authorize(Roles = "administrator,moderator,developer,member")]
        public IActionResult Get(int id)
        {
            var item = _service.Get(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "administrator,moderator")]
        public IActionResult Create([FromBody] LeopardProfile leopardProfile)
        {
            try
            {
                _service.Create(leopardProfile);
                return StatusCode(201);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "administrator,moderator")]
        public IActionResult Update(int id, [FromBody] LeopardProfile leopardProfile)
        {
            if (id != leopardProfile.LeopardProfileId) return BadRequest();

            try
            {
                _service.Update(leopardProfile);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "administrator,moderator")]
        public IActionResult Delete(int id)
        {
            var existing = _service.Get(id);
            if (existing == null) return NotFound();
            _service.Delete(id);
            return Ok();
        }

        [HttpGet("search")]
        [Authorize(Roles = "administrator,moderator,developer,member")]
        public IActionResult Search([FromQuery] string name, [FromQuery] string weight)
        {
            var result = _service.Search(name, weight);
            return Ok(result);
        }
    }

}
