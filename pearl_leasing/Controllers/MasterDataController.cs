using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Data;
using pearl_leasing.DTO;
using pearl_leasing.Models;

namespace pearl_leasing.Controllers
{
    [ApiController]
    [Route("api/master-data")]
    public class MasterDataController : ControllerBase
    {
        private PearlLeasingDbContext _context;
        public MasterDataController(PearlLeasingDbContext context)
        {
            _context = context;
        }

        [HttpPost("buildings")]
        public async Task<IActionResult> CreateBuildings([FromBody] List<CreateBuildingRequestDto> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest("No buildings provided for creation.");
            }

            var savedBuildings = new List<Building>();

            foreach (var request in requests)
            {
                if (string.IsNullOrWhiteSpace(request.Name)) continue;

                var building = new Building { Name = request.Name.Trim() };
                _context.Buildings.Add(building);
                savedBuildings.Add(building);
            }

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = savedBuildings.Count == 1
                        ? "Building created successfully!"
                        : "All buildings created successfully!",
                    count = savedBuildings.Count,
                    data = savedBuildings.Select(b => new { b.Id, b.Name })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving the buildings.");
            }
        }

        [HttpGet("buildings")]
        public async Task<IActionResult> GetBuildings()
        {
            var buildings = await _context.Buildings
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();
            return Ok(buildings);
        }

        [HttpPost("stalls/bulk-generate")]
        public async Task<IActionResult> BulkGenerateStalls([FromBody] CreateBulkStallsRequestDto request)
        {
            var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == request.BuildingId);
            if (!buildingExists)
            {
                return NotFound($"Target Building with ID {request.BuildingId} does not exist.");
            }

            if (request.StartNumber > request.EndNumber)
            {
                return BadRequest("Start number cannot be greater than end number.");
            }

            var generatedStalls = new List<StallUnit>();

            for (int i = request.StartNumber; i <= request.EndNumber; i++)
            {
                string finalUnitNumber;

                if (string.IsNullOrWhiteSpace(request.Prefix))
                {
                    finalUnitNumber = i.ToString();
                }
                else
                {
                    finalUnitNumber = $"{request.Prefix.Trim()}-{i}";
                }

                var stall = new StallUnit
                {
                    UnitNumber = finalUnitNumber,
                    BuildingId = request.BuildingId,
                    IsOccupied = false
                };

                _context.StallUnits.Add(stall);
                generatedStalls.Add(stall);
            }

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully generated {generatedStalls.Count} stall units!",
                    buildingId = request.BuildingId,
                    count = generatedStalls.Count,
                    stalls = generatedStalls.Select(s => new { s.Id, s.UnitNumber })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while mass generating the stall units.");
            }
        }

        [HttpGet("stalls/by-building/{buildingId}")]
        public async Task<IActionResult> GetStallsByBuilding(int buildingId)
        {
            var stalls = await _context.StallUnits
                .Where(s => s.BuildingId == buildingId)
                .Select(s => new { s.Id, s.UnitNumber, s.IsOccupied })
                .ToListAsync();
            return Ok(stalls);
        }

        [HttpPost("vendors")]
        public async Task<IActionResult> CreateVendors([FromBody] List<CreateVendorRequestDto> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest("No vendors or categories provided for creation.");
            }

            var savedVendors = new List<VendorCategory>();

            foreach (var request in requests)
            {
                if (string.IsNullOrWhiteSpace(request.Name)) continue;

                var vendor = new VendorCategory { Name = request.Name.Trim() };
                _context.VendorCategories.Add(vendor);
                savedVendors.Add(vendor);
            }

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = savedVendors.Count == 1
                        ? "Vendor/Category created successfully!"
                        : "All vendors/categories created successfully!",
                    count = savedVendors.Count,
                    data = savedVendors.Select(v => new { v.Id, v.Name })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving the vendors or categories.");
            }
        }

        [HttpGet("vendors")]
        public async Task<IActionResult> GetVendors()
        {
            var vendors = await _context.VendorCategories
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();
            return Ok(vendors);
        }
    }
}
