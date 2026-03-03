# How to add CSV support to project

## Step 1 - Add CSV file to project

a) create new folder `Models\TestData`
b) place CSV file in above folder

---

## Step 2 - Add nuget for CsvHelper library

``` bash
dotnet add package csvhelper
```

---

## Step 3 - Create CSV record model

The rows in the CSV file are incompatible with the existing `DailyProductionDTO` model required by the D3 logic in the frontend (see line 18 in `Pages/Index.cshtml`). So we need to create a new model class for reading from CSV file:

See [DailyProductionCSV.cs](Models\DailyProductionCSV.cs).

> **note:** the special annotations above the properties (e.g.  [Name("PartitionKey")]) that maps the correct columns to the C# properties in the class.

---

## Step 4 - Setup file path

The application needs to know how to find the CSV-file in the filesystem. Therefore we need to supply a filepath in form of an environment variable (lets call it `CSVFILE`), that will be read in the `DailyProductionController` class.

We have added the following code to the constructor:

``` csharp
    private readonly string? _filePath;

    public DailyProductionController(ILogger<DailyProductionController> logger, 
                                     IConfiguration configuration)
    {
        _logger = logger;
        // Get the CSV file path from configuration
        _filePath = configuration.GetValue<string>("CSVFILE");
    }
```

For this to work, we need to place the path in CSVFILE prior to starting the application with:

```bash
export CSVFILE=/full/path/to/Models/TestData/IBASProduction2022.csv
```

> **NOTE:** Replace "/full/path/to/" with the correct path on your system.

---

## Step 5 - Create function for reading CSV

Now lets add a `private` function to the DailyProductionController that can read data from the file pointed to by `CSVFILE`.

See [LoadDataFromCSV() in Controllers/DailyProductionController.cs](Controllers/DailyProductionController.cs).

> **Note:** 
> Line 77 reads the CSV rows into a list of type `DailyProductionCSV` -- not `DailyProductionDTO`.

---

## Step 6 - Adapt HTTP GET to read CSV file

Now add the CSV read capability to the `Get()` method in the controller. See lines 28-39.

We finish off the solution by converting the rows returned by LoadDataFromCSV() into a list of DailyProductionDTOs, as required by the frontend -- see lines 42-47.

---

## Step 7 - Local test

Now run the application from a local Bash terminal:

```bash
 export CSVFILE=./Models/TestData/IBASProduction2022.csv # May require adaptation
 dotnet run
```

---
