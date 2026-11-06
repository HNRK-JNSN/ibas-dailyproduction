using Microsoft.AspNetCore.Mvc;
using DailyProduction.Models;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;

namespace IbasAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DailyProductionController : ControllerBase
    {
        private readonly ILogger<DailyProductionController> _logger;
        private const string TableName = "IbasProduction2022";
        private readonly TableClient? _client;
        private readonly string? _configError;

        /// <summary>
        /// Initializes a new instance of the <see cref="DailyProductionController"/> class.
        /// </summary>
        public DailyProductionController(ILogger<DailyProductionController> logger)
        {
            _logger = logger;

            var tableEndpoint = Environment.GetEnvironmentVariable("STORAGETABLE_ENDPOINT");

            /// Check if the table endpoint environment variable is set and valid.
            if (string.IsNullOrWhiteSpace(tableEndpoint))
            {
                _configError = "Environment variable STORAGETABLE_ENDPOINT is not set.";
                _logger.LogCritical("{ConfigError} Set it to the table endpoint, e.g. https://<account>.table.core.windows.net", _configError);
                return;
            }

            /// Check if the table exists and create it if it doesn't.
            /// Validate the table endpoint URI.
            if (!Uri.TryCreate(tableEndpoint, UriKind.Absolute, out var endpointUri))
            {
                _configError = "STORAGETABLE_ENDPOINT is not a valid absolute URI.";
                _logger.LogCritical("{ConfigError} Value: '{Endpoint}'", _configError, tableEndpoint);
                return;
            }

            /// Create the TableServiceClient and TableClient instances.
            try
            {
                var client = new TableServiceClient(endpointUri, new DefaultAzureCredential());
                _client = client.GetTableClient(TableName);
                _logger.LogInformation("TableClient instance created for table {Table} at {Endpoint}.", TableName, endpointUri);               

            }
            catch (Exception ex)
            {
                _configError = "Could not create the table client.";
                _logger.LogCritical(ex, "Could not create TableClient for {Endpoint}, table {Table}.", endpointUri, TableName);
            }
        }

        /// <summary>
        /// Provides a hint for common storage-related exceptions.
        /// </summary>
        private static string HintFor(RequestFailedException ex) => (ex.Status, ex.ErrorCode) switch
        {
            (404, "TableNotFound") => $"Table '{TableName}' does not exist in the storage account.",
            (404, _) => "Resource not found; check STORAGETABLE_ENDPOINT points to the right storage account.",
            (401, _) => "Unauthorized; the identity token was rejected.",
            (403, _) => "Forbidden; the identity likely lacks the 'Storage Table Data Reader' role, or a firewall blocks this client.",
            (0, _) => "Could not reach the storage endpoint (network/DNS error); check the endpoint and connectivity.",
            _ => ex.Message
        };

        /// <summary>
        /// Retrieves the daily production data.
        /// </summary>
        /// <returns>A list of daily production data.</returns> 
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var _productionRepo = new List<DailyProductionDTO>();

            if (_client == null)
            {
                _logger.LogCritical("No TableClient available: {ConfigError}", _configError);
                return Problem(
                    title: "Storage is not configured",
                    detail: _configError,
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            int skipped = 0;

            _logger.LogInformation("Querying daily production data from table {Table}.", TableName);

            try
            {
                await foreach (TableEntity entity in _client.QueryAsync<TableEntity>(cancellationToken: HttpContext.RequestAborted))
                {
                    // Parse the row key as a date and the partition key as a model ID. Skip malformed rows.
                    if (!DateTime.TryParse(entity.RowKey, out var date)
                        || !int.TryParse(entity.PartitionKey, out var modelId))
                    {
                        skipped++;
                        _logger.LogWarning(
                            "Skipping malformed row in {Table}: PartitionKey='{PartitionKey}' (expected integer bike model), RowKey='{RowKey}' (expected date).",
                            TableName, entity.PartitionKey, entity.RowKey);
                        continue;
                    }

                    // Retrieve the number of items produced, defaulting to 0 if not present or invalid.
                    if (entity.GetInt32("itemsProduced") is not int itemsProduced)
                    {
                        _logger.LogWarning(
                            "Row {PartitionKey}/{RowKey} has no valid 'itemsProduced' value; using 0.",
                            entity.PartitionKey, entity.RowKey);
                        itemsProduced = 0;
                    }

                    // Add the parsed data to the production repository.
                    _productionRepo.Add(new DailyProductionDTO()
                    {
                        Date = date,
                        ItemsProduced = itemsProduced,
                        Model = (BikeModel)modelId
                    });
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request cancelled while querying table {Table}.", TableName);
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogError(ex, "Authentication to Azure failed. Check az login / managed identity / environment credentials.");
                return Problem(
                    title: "Authentication to Azure failed",
                    detail: "The server could not obtain a credential. Check the identity the app runs as.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex,
                    "Table query failed. Status={Status}, ErrorCode={ErrorCode}, Table={Table}. {Hint}",
                    ex.Status, ex.ErrorCode, TableName, HintFor(ex));

                var status = ex.Status is 401 or 403 or 404 ? ex.Status : StatusCodes.Status502BadGateway;
                return Problem(
                    title: "Table storage request failed",
                    detail: $"{HintFor(ex)} (storage status {ex.Status}, code {ex.ErrorCode ?? "n/a"})",
                    statusCode: status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error querying table {Table}.", TableName);
                return Problem(
                    title: "Unexpected error querying table",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            if (skipped > 0)
            {
                _logger.LogWarning("Returned {Count} rows; skipped {Skipped} malformed rows.", _productionRepo.Count, skipped);
            }
            else
            {
                _logger.LogInformation("Returned {Count} rows from {Table}.", _productionRepo.Count, TableName);
            }

            return Ok(_productionRepo);
        }
    }
}
