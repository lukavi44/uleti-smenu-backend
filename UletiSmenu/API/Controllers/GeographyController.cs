using Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/geography")]
    public class GeographyController : ControllerBase
    {
        private readonly IGeographyService _geographyService;

        public GeographyController(IGeographyService geographyService)
        {
            _geographyService = geographyService;
        }

        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries() =>
            Ok(await _geographyService.GetCountriesAsync());

        [HttpGet("regions")]
        public async Task<IActionResult> GetRegions([FromQuery] string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return BadRequest("Country code is required.");

            return Ok(await _geographyService.GetRegionsAsync(countryCode));
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities(
            [FromQuery] string countryCode,
            [FromQuery] string regionCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return BadRequest("Country code is required.");

            if (string.IsNullOrWhiteSpace(regionCode))
                return BadRequest("Region code is required.");

            return Ok(await _geographyService.GetCitiesAsync(countryCode, regionCode));
        }
    }
}
