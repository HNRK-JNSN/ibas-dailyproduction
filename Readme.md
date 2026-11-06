# ibas-dailyproduction

The daily production report from International Bicycles A/S

> Note: This `git` branch uses Azure Identity Management to authenticate against the storage account in Azure.

## To execute

Get your own table endpoint from Azure portal (Storage Account -> Settings -> Endpoints -> **Table service**) and export it to the environment as shown below:

```bash
$ export STORAGETABLE_ENDPOINT=YOUR-OWN_ENDPOINT_GOES-HERE
```

Run the application from a terminal with:

```bash
$ dotnet run
```
