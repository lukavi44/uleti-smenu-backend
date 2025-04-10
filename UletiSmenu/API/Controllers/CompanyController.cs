using API.DTOs;
using AutoMapper;
using Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly IMapper _mapper;

        public CompanyController(ICompanyService companyService, IMapper mapper)
        {
            _companyService = companyService;
            _mapper = mapper;
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetAllCompanies()
        {
            var companies = await _companyService.GetAllCompaniesAsync();

            // 🔍 DEBUG: Try mapping just one company to isolate the error
            var firstCompany = companies.FirstOrDefault();
            if (firstCompany != null)
            {
                var testDto = _mapper.Map<CompanyDTO>(firstCompany);
            }

            // 🔁 Then map all of them
            var companyDtos = _mapper.Map<IEnumerable<CompanyDTO>>(companies);
            return Ok(companyDtos);
        }


    }
}
