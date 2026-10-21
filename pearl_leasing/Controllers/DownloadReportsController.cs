using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;

namespace pearl_leasing.Controllers
{
    [ApiController]
    [Route("api/report")]
    public class DownloadReportsController : ControllerBase
    {
        private readonly PearlLeasingDbContext _context;
        private readonly IConfiguration _configuration;

        public DownloadReportsController(PearlLeasingDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        

        
    }
}
