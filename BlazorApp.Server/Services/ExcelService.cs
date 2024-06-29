using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace BlazorApp.Server.Services {
    public class ExcelService {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Excelファイルを読み込み、データを返す
        public List<List<string>> ReadExcel(Stream fileStream) {
            var data = new List<List<string>>();
            try {
                using (var workbook = new XLWorkbook(fileStream)) {
                    var worksheet = workbook.Worksheets.Worksheet(1);
                    foreach (var row in worksheet.RowsUsed()) {
                        var rowData = new List<string>();
                        foreach (var cell in row.CellsUsed()) {
                            rowData.Add(cell.GetValue<string>());
                        }
                        data.Add(rowData);
                    }
                }
                _logger.LogInformation("Excel file read successfully.");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error reading Excel file.");
                throw;
            }
            return data;
        }

        // JSONデータをExcelファイルにエクスポートする
        public byte[] ExportToExcel(List<List<string>> data) {
            try {
                using (var workbook = new XLWorkbook()) {
                    var worksheet = workbook.Worksheets.Add("Sheet1");
                    for (int i = 0; i < data.Count; i++) {
                        for (int j = 0; j < data[i].Count; j++) {
                            worksheet.Cell(i + 1, j + 1).Value = data[i][j];
                        }
                    }
                    using (var stream = new MemoryStream()) {
                        workbook.SaveAs(stream);
                        _logger.LogInformation("Excel file created successfully.");
                        return stream.ToArray();
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Error creating Excel file.");
                throw;
            }
        }
    }
}

