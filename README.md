# YNAB CSV Export to Ledger
YNAB CSV Export to Ledger is a conversion tool to take a YNAB 4 CSV export and create a plaintext accounting ledger register file.

# Introduction
[ledger-cli](https://github.com/ledger/ledger) is a command line tool for double-entry accounting based on a [plain text file](https://plaintextaccounting.org/).  This application takes a CSV file that was exported from YNAB 4 register and creates a plain text register file.

Although ledger-cli offers a [CSV import tool](https://www.ledger-cli.org/3.0/doc/ledger3.html#The-convert-command) and maintains a list of [CSV conversion tools](https://github.com/ledger/ledger/wiki/CSV-Import), this tool is specifically designed to import YNAB CSV files.  This simplifies the process by asking a few simple questions.

# Features
 * Removes duplicate transfer transactions.  Since YNAB isn't a double entry accounting system, a transfer appears twice in the register, once for each account.  This import tool removes the duplication.
 * Groups split transactions as a single transaction
   * Supports per-line item comments
 * Imports memos a comments
 * Imports accounts as assets or liabilities based on input
 * Internationalization
   * Is able to handle imports from YNAB with various currencies.  Tested $, €, and ден
   * Able to import currencies that use commas decimal separators, spaces for thousands separators, etc.
   * Able to parse numbers in any format supported by the dotnet `CultureInfo` object
  * Able to parse exported CSV from YNAB in both comma separated and tab separated.  YNAB decides how to export based on the currency
  * Tag flagged transactions from YNAB as a [meta-data tag](https://www.ledger-cli.org/3.0/doc/ledger3.html#Metadata-tags)
  * Optionally imports [transaction state flags](https://www.ledger-cli.org/3.0/doc/ledger3.html#State-flags)
  
# Installation
## From source
Before building the repository, please ensure you have the [dotnet 2.2 sdk or later](https://dotnet.microsoft.com/download/dotnet-core/2.2)
 * Clone the repository `git clone https://github.com/vermiceli/ynab-to-ledger`
 * `cd ynab-to-ledger`
 * `dotnet run` or `dotnet run --configuration Release` if you wish to have compilation optimizations

## From binary
### Dotnet Runtime

### Windows
 * Download release from [Releases](TOOD GET LINK)
 * Extract the zip file
 * Run the command 
 
### Mac
 * Download release from [Releases](TOOD GET LINK)
 * Extract the zip file
 * Run the command 
 
### Linux
 * Download release from [Releases](TOOD GET LINK)
 * Extract the zip file
 * Run the command 

# Usage
TOOD FINISH
