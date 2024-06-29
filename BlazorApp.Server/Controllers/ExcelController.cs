using BlazorApp.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlazorApp.Server.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class ExcelController : ControllerBase {
        private readonly ExcelService _excelService;
        private readonly ILogger<ExcelController> _logger;

        public ExcelController(ExcelService excelService, ILogger<ExcelController> logger) {
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("read")]
        public async Task<IActionResult> ReadExcel(IFormFile file) {
            if (file == null || file.Length == 0) {
                return BadRequest("No file uploaded.");
            }

            try {
                using (var stream = new MemoryStream()) {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    var data = _excelService.ReadExcel(stream);
                    return Ok(data);
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Error reading Excel file.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("export")]
        public IActionResult ExportToExcel([FromQuery] string filename, [FromBody] List<List<string>> data) {
            if (data == null || data.Count == 0) {
                return BadRequest("No data provided.");
            }

            if (string.IsNullOrEmpty(filename)) {
                filename = "export.xlsx";
            }

            try {
                var excelFile = _excelService.ExportToExcel(data);
                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error creating Excel file.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}

