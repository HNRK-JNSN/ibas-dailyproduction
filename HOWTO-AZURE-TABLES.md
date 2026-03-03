# Guide: How to connect to Azure Tables

In this small guide we run through the steps necessary for creating the code in this application.

```mermaid

```

---

## Step 1 - Create Storage Account

Lets start by creating a storage account in our existing resource group called `IBasTestGroup` using Azure-CLI in a terminal:

a) Setup environment in terminal:

``` bash
export RESGRP=IBasTestGroup
export ACCNAME=ibasproductionstorage65fsre
export TABLENAME=DailyProductionTable
```

b) Create the storage account:

``` bash
az storage account create \
    --name $ACCNAME \
    --resource-group $RESGRP \
    --location swedencentral \
    --sku Standard_LRS \
    --kind StorageV2
```

> **Note:** the `--name` parameter must be globally unique, so add some random text after the name.

---

## Step 2 - Create new table in the storage account

> Import: assert that the environment variables `RESGRP` and `ACCNAME` have been set in you terminal (see step 1)
> use `echo $RESGRP` and `echo $ACCNAME` to verify their content.

a) Get your Storage Account Key and store it in `AZURE_STORAGE_KEY`:

``` bash
AZURE_STORAGE_KEY=$(az storage account keys list \
    --resource-group $RESGRP \
    --account-name $ACCNAME \
    --query "[0].value" -o tsv)
```

b) Verify your key:

``` bash
echo $AZURE_STORAGE_KEY
```

c) Create a table in your storage account:

```bash
az storage table create \
    --name  $TABLENAME \
    --account-name $ACCNAME \
    --account-key $AZURE_STORAGE_KEY
```

---

## Step 3 - Import data using Azure Storage Explorer

If you haven't downloaded and installed the Azure Storage Explorer already, you can do it from the following link:
[Azure Data Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer).

a) Open the Storage Explore app on your local machine

b) Chose `Sign in with Azure` (this is similar to using `az login`)

c) on the left side select `Open Explorer` and browse through the tree to find you newly created **account** and **table** from **step 2** above.

d) Click on the table to open the **table view** for `DailyProductionTable`

e) Select `Import` from the icon menu at the top of the view and select the CSV file `IBASProduction2022.csv` found in the `Models\TestData` folder

f) In this CSV file, columns are separated (delimited) by comma, so type in a comma in the `Column delimiter` textbox.

g) the `Import Entities` view now shows 4 columns: `PartitionKey`, `RowKey`, `ProductionTime` and `itemsProduced`. Accept the defaults selected below and click the `Import`button.

h) Congratulations! Your table is now populated with the data from the CSV file and available in Azure storage.

---

## Step 4 - C# client

Next we shall implement the C# code that connects and fetches the data from our table.

a) add the `Azure.Data.Tables` nuget to the project

b) Extend the constructor to read system configuration and fetch the **connection string** and **table name** in the constructor. See lines 21 and 24-25.

c) In the HTTP `Get()`method of the controller, add logic to:
  
  - 1) Validate that the configuration is correct (lines 35-39);
  - 2) Create a Azure TableClient and list for results (lines 41-43);
  - 3) Query the table for all entities, converting them to DailyProductionDTO objects (lines 45-71)

---

## Step 5 - Local test

a) Now build the project with `dotnet build`

b) Get you connection string using az-cli (make sure the environmental variables from step 1 are still present in your terminal):

```bash
export TableConnectionString=$(az storage account show-connection-string \
    --name $ACCNAME \
    --resource-group $RESGRP \
    --query connectionString \
    --output tsv)
```

c) run the application with `dotnet run`

---
