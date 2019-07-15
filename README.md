# YNAB CSV Export to Ledger
YNAB CSV Export to Ledger is a conversion tool to take a YNAB 4 CSV export and create a plaintext accounting ledger register file.

# Introduction
[ledger-cli](https://github.com/ledger/ledger) is a command line tool for double-entry accounting based on a [plain text file](https://plaintextaccounting.org/).  This application takes a CSV file that was exported from YNAB 4 register and creates a plain text register file.

Although ledger-cli offers a [CSV import tool](https://www.ledger-cli.org/3.0/doc/ledger3.html#The-convert-command) and maintains a list of [CSV conversion tools](https://github.com/ledger/ledger/wiki/CSV-Import), this tool is specifically designed to import YNAB CSV files.  This simplifies the process by asking a few simple questions.

# Features
 * Removes duplicate transfer transactions.  Since YNAB isn't a double entry accounting system, a transfer appears twice in the register, once for each account.  This import tool removes the duplication.
 * Groups split transactions as a single transaction
   * Supports per-line item comments
 * Imports memos as comments
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
 * Download dotnet release from [Releases](https://github.com/vermiceli/ynab-to-ledger/releases/download/v1.0/dotnet.zip)
 * Extract the zip file
 * Run the command `dotnet run` or `dotnet run --configuration Release` if you wish to have compilation optimizations

### Windows
 * Download release from [Releases](https://github.com/vermiceli/ynab-to-ledger/releases/download/v1.0/windows.zip)
 * Extract the zip file
 * Run the command `.\YNABCSVToLedger.exe`

### Mac
 * Download release from [Releases](https://github.com/vermiceli/ynab-to-ledger/releases/download/v1.0/mac.zip)
 * Extract the zip file
 * Run the command `./YNABCSVToLedger`

### Linux
 * Download release from [Releases](https://github.com/vermiceli/ynab-to-ledger/releases/download/v1.0/mac.zip)
 * Extract the zip file
 * Run the command `./YNABCSVToLedger`

# Usage
The first step is to export the data from YNAB 4.  To do this, click `File` and then select `Export Ctrl+E`.    Then select `Export to CSV`.  Do not select `Export Budget to CSV` as that does not include the register, which this tool uses.

Run the application (see installation), it'll ask various questions to import the file.

 * Output file name - This is the name and path of the exported YNAB 4 CSV file.  YNAB exports two files, the budget and the register.  This tool requires the register.
 * Use Cleared colum - (Default Y) whether or not to use the reconcile feature of YNAB in the export file to set the [transaction state flags](https://www.ledger-cli.org/3.0/doc/ledger3.html#State-flags).  If you do not reconcile transactions in YNAB, then you can set this to N so that your transactions in ledger won't all include the ! state flag.
 * Culture - (Default en-US) Use this to specify which culture the numbers and dates will be in.  For example, if you are using YNAB with french euros, then set the field to `fr-FR`.   This is used by the tool to parse the amounts and dates.
 * CSV Delimter - (Default ,) YNAB exports the register with comma separated fields, or tab separated fields depending on the culture.  If the register csv contains numbers from a culture that separates the decimal component with commas, it'll export the data as tab separated.  To specify a tab separated file, press the `[Tab]` key and then enter.
  * For every account the tool finds, it'll prompt to specify whether the account is an asset or a liability.  For example, credit cards are a liability, but a checking account is an asset.  The responses are case insensitive. For example, `Asset` and `asset` both are will work.  You can also specify `a` or `l` for convenience.

```
YNAB-exported CSV Filename : C:\Users\user\Dropbox\YNAB\Exports\Household as of 2019-01-01 001 PM-Register.csv
Output file name [register.dat]:
Use Cleared column Y/N [Y]: Y
Culture: [en-US]:
CSV Delimeter [,]:
Specify account type for 'Checking Account' [Asset/Liability] use [a/l] for short:
Asset
Specify account type for 'Discover Card' [Asset/Liability] use [a/l] for short:
liability
Specify account type for 'American Express' [Asset/Liability] use [a/l] for short:
l
Specify account type for 'Savings Account' [Asset/Liability] use [a/l] for short:
l
Specify account type for 'Trading Account' [Asset/Liability] use [a/l] for short:
a
Warning: record doesn't transfer any money.
Warning: record doesn't transfer any money.
```
