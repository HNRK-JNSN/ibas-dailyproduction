using CsvHelper;
using CsvHelper.TypeConversion;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using DailyProduction.Models;
using System.Globalization;

namespace IbasAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DailyProductionController : ControllerBase
    {
        private List<DailyProductionDTO> _productionRepo = new List<DailyProductionDTO>();
        private readonly ILogger<DailyProductionController> _logger;
        private readonly string? _filePath;

        public DailyProductionController(ILogger<DailyProductionController> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Get the CSV file path from configuration
            _filePath = configuration.GetValue<string>("CSVFILE");
        }
        
        [HttpGet]
        public IEnumerable<DailyProductionDTO> Get()
        {
            List<DailyProductionCSV> csvRecords;

            // 1. Read data from CSV file into temporary list            
            try
            {
                csvRecords = LoadDataFromCSV();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file.");
                return Enumerable.Empty<DailyProductionDTO>();
            }

            // 2. Map CSV records to DTOs and store in repository
            _productionRepo = csvRecords.Select(csvRow => new DailyProductionDTO
            {
                Date = csvRow.Date,
                Model = (BikeModel)csvRow.Model, // Assuming Model in CSV corresponds to BikeModel enum
                ItemsProduced = csvRow.ItemsProduced
            }).ToList();

            return _productionRepo;
        }

        /// <summary>
        /// Loads data from the CSV file and returns a list of DailyProductionCSV records.
        /// </summary>
        /// <returns>A list of DailyProductionCSV records.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the CSV file path is not configured.</exception>
        /// <exception cref="ApplicationException">Thrown when there is an error reading the CSV file.</exception>
        private List<DailyProductionCSV> LoadDataFromCSV()
        {
            Stream fileStream = new FileStream(
                _filePath ?? throw new InvalidOperationException("CSV file path is not configured."),
                FileMode.Open, FileAccess.Read);

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null, // Ignore missing fields
                    HeaderValidated = null, // Ignore header validation
                    BadDataFound = null // Ignore bad data
                };

                using var reader = new StreamReader(fileStream);
                using var csv = new CsvReader(reader, config);
                
                var records = csv.GetRecords<DailyProductionCSV>().ToList();
                return records;
            }
            catch (HeaderValidationException ex)
            {
                throw new ApplicationException("CSV file header is invalid.", ex);
            }
            catch (TypeConverterException ex)
            {
                throw new ApplicationException("CSV file contains invalid data format.", ex);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error reading CSV file", ex);
            }
        }
    }
}
