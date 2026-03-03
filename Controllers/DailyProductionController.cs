using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.Data.Tables;

using DailyProduction.Models;

namespace IbasAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DailyProductionController : ControllerBase
    {

        private List<DailyProductionDTO> _productionRepo = new List<DailyProductionDTO>();
        private readonly ILogger<DailyProductionController> _logger;
        private readonly string _connectionString = string.Empty;
        private readonly string _tableName = string.Empty;

        public DailyProductionController(ILogger<DailyProductionController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetValue<string>("TableConnectionString") ?? string.Empty;
            _tableName = configuration.GetValue<string>("TABLENAME") ?? string.Empty;
        }
        
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // 1) Validate that the table storage configuration is present
            if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(_tableName))
            {
                return StatusCode(500, "Table storage configuration is missing.");
            }

            // 2) Create a Azure TableClient
            var tableClient = new TableClient(_connectionString, _tableName);
            var results = new List<DailyProductionDTO>();

            // 3) Query the table for all entities, converting them to DailyProductionDTO objects
            try
            {
                // Pageable query to get all entities using specialized TableEntity type, 
                // which allows dynamic access to properties
                AsyncPageable<TableEntity> queryResults = tableClient.QueryAsync<TableEntity>(filter: "");

                await foreach (TableEntity entity in queryResults)
                {
                    results.Add(new DailyProductionDTO
                    {
                        // Convert the PartitionKey to the BikeModel enum, 
                        // the RowKey to a DateTime, 
                        // and get the itemsProduced property as an integer
                        Model = (BikeModel) Enum.Parse(typeof(BikeModel), entity.PartitionKey),
                        Date = DateTime.Parse(entity.RowKey),
                        ItemsProduced = (int)(entity.GetInt32("itemsProduced") ?? 0),
                    });
                }

                return Ok(results);
            }
            catch (RequestFailedException ex)
            {
                return StatusCode((int)ex.Status, ex.Message);
            }
        }
    }
}
