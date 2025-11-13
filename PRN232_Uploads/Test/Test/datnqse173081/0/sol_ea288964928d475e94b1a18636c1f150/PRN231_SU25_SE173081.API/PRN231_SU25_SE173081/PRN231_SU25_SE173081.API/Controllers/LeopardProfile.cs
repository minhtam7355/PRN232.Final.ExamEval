using Azure;
using BLL.DTOs;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using PRN231_SU25_SE173081.API.ErrorReponses;

namespace PRN231_SU25_SE173081.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeopardProfile(ProfileService service) : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "4, 5, 6, 7")]
        public async Task<IActionResult> GetAll()
        {
            var response = await service.GetAll();
            return Ok(response);
        }
        [HttpGet("{id}")]
        [Authorize(Roles = "4, 5, 6, 7")]
        public async Task<IActionResult> Get(int id)
        {
            var response = await service.GetById(id);
            if (response == null)
            {
                return NotFound(new ErrorReponses.ErrorResponse("HB40401", "Resource not found"));
            }
            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "5, 6")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] ProfileRequest request)
        {
            try
            {
                if (request.Weight <= 15)
                {
                    return BadRequest(new ErrorReponses.ErrorResponse("HB40001", "Invalid weight"));
                }
                
                await service.Add(request);

                return CreatedAtAction(nameof(GetAll), new { });
            }
            catch (Exception)
            {

                return StatusCode(500, new ErrorReponses.ErrorResponse("HB50001", "Internal server error"));
            }

        }
        [HttpPut("{id}")]
        [Authorize(Roles = "5, 6")]

        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProfileRequest request)
        {
            try
            {
                var item = await service.GetById(id);
                if (item == null)
                {
                    return NotFound(new ErrorReponses.ErrorResponse("HB40401", "Resource not found"));
                }
                if (request.Weight <= 15)
                {
                    return BadRequest(new ErrorReponses.ErrorResponse("HB40001", "Invalid weight"));
                }
                await service.Update(id, request);
                return Ok();
            }
            catch (Exception)
            {

                return StatusCode(500, new ErrorReponses.ErrorResponse("HB50001", "Internal server error"));
            }
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "5, 6")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var item = await service.GetById(id);
                if (item == null)
                {
                    return NotFound(new ErrorReponses.ErrorResponse("HB40401", "Resource not found"));
                }
                await service.Delete(id);
                return Ok();
            }
            catch (Exception)
            {

                return StatusCode(500, new ErrorReponses.ErrorResponse("HB50001", "Internal server error"));
            }
        }

        [HttpGet("search")]
        [Authorize] // All roles with token
        [EnableQuery]
        public IActionResult SearchWithOData()
        {
            try
            {
                var query = service.GetHandbagsQueryable();
                return Ok(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorResponse("HB50001", "Internal server error"));
            }
        }
    }
}
