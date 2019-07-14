namespace YNABCSVToLedger {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using CsvHelper;
    using CsvHelper.Configuration;

    /// <summary>
    /// Takes a YNAB-exported CSV file and creates a plaintext accounting file suitable for use by ledger
    /// </summary>
    public class Program {
        /// <summary>
        /// The entry point of the program
        /// </summary>
        public static void Main() {
            string input;
            Console.Write($"YNAB-exported CSV Filename : ");
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) {
                throw new Exception("Input CSV file required!");
            }

            string fileName = input;

            if (!File.Exists(fileName)) {
                throw new Exception("File does not exist!");
            }

            Console.Write($"Output file name [register.dat]: ");
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) {
                input = "register.dat";
            }

            string outputFileName = input;
            bool replaceFile = false;
            if (File.Exists(outputFileName)) {
                Console.WriteLine("The output file already exists.  Do you want to replace it? Y/N [N]: ");
                input = Console.ReadLine();
                replaceFile = Program.StringToBool(input, false);

                if (!replaceFile) {
                    return;
                }
            }

            Console.Write($"Use Cleared column Y/N [Y]: ");
            input = Console.ReadLine();
            bool useClear = Program.StringToBool(input, true);

            Console.Write($"Culture: [en-US]: ");
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) {
                input = "en-US";
            }

            CultureInfo culture;
            culture = new CultureInfo(input);

            Console.Write($"CSV Delimeter [,]: ");
            input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) {
                input = ",";
            }

            string delimeter = input;
            using (var reader = new StreamReader(fileName)) {
                using (var csv = new CsvReader(reader, new Configuration() { Delimiter = delimeter })) {
                    var records = csv.GetRecords<CSVLineItem>().ToList();

                    IList<string> accounts = records.Select(r => r.Account).Distinct().ToList();
                    IDictionary<string, string> accountTypes = new Dictionary<string, string>();
                    foreach (string account in accounts) {
                        Console.WriteLine($"Specify account type for '{account}' [Asset/Liability] use [a/l] for short: ");
                        input = Console.ReadLine();
                        if (input.ToLower() == "asset" || input.ToLower() == "assets" || input.ToLower() == "a") {
                            accountTypes[account] = "Assets";
                        } else if (input.ToLower() == "liability" || input.ToLower() == "liabilities" || input.ToLower() == "l") {
                            accountTypes[account] = "Liabilities";
                        } else {
                            throw new Exception("Unsupported account type.");
                        }
                    }

                    IList<Transaction> groupedTransactions = Program.GroupLineItems(records, accountTypes, useClear, culture);
                    string ledger = Program.CreateLedger(groupedTransactions);

                    File.WriteAllText(outputFileName, ledger);
                }
            }
        }

        /// <summary>
        /// Takes the list of CSV line items from YNAB and groups them into in-memory objects based on the (Split i/n) memo.
        /// This method also removes the duplicate transfer transactions
        /// </summary>
        /// <param name="lineItems">The list of CSV line items from a YNAB export</param>
        /// <param name="accountTypes">The mapping of account to its type (Asset/Liability)</param>
        /// <param name="useClear">Whether or not to include the clear fields (*/!) in output</param>
        /// <param name="culture">The culture to use when parsing the amounts</param>
        /// <returns>A list of transactions with their own line item(s) depending on if the transaction is a split or not</returns>
        public static IList<Transaction> GroupLineItems(
            IEnumerable<CSVLineItem> lineItems,
            IDictionary<string, string> accountTypes,
            bool useClear,
            CultureInfo culture) {
            IList<Transaction> groupedTransactions = new List<Transaction>();
            IDictionary<int, CSVLineItem> splitTransactions = new Dictionary<int, CSVLineItem>();
            int splitNumber = 0;
            int currentNumberOfSplits = 0;

            Action appendBufferedSplitTransactions = () => {
                if (splitTransactions.Any()) {
                    if (splitTransactions.Count != currentNumberOfSplits) {
                        throw new Exception("Missing a split!");
                    }

                    IList<CSVLineItem> toAdd = splitTransactions.Values.ToList();
                    groupedTransactions.Add(new Transaction(accountTypes, useClear, culture, toAdd));
                    splitTransactions.Clear();
                }
            };

            //// for each CSV line item, see if it is part of a split transaction or the entire transaction
            //// if it is not a split, just add it to the list of transactions
            //// if it is a split, add it to a split transaction buffer until all splits for transaction found
            //// this code handles splits out of order: 3/3, 2/3, 1/3; 2/2 1/2; or in order: 1/2, 2/2
            //// this code has to handle multiple splits from different transactions next to eachother
            //// e.g. Split (1/2), Split (2/2), Split (1/3), Split (2/3), Split (3/3)
            //// It does this by noticing if the split numerator has been seen before
            foreach (var record in lineItems) {
                if (record.Memo.Contains("(Split")) {
                    Match m = Regex.Match(record.Memo, @"\(Split (\d+)/(\d+)");
                    if (!m.Success || m.Groups.Count != 3) {
                        throw new Exception("Unexpected split memo!");
                    }

                    splitNumber = int.Parse(m.Groups[1].Value);
                    if (!splitTransactions.Any()) {
                        currentNumberOfSplits = int.Parse(m.Groups[2].Value);
                    }

                    if (splitTransactions.ContainsKey(splitNumber)) {
                        // back to back splits, clear existing splits
                        if (splitTransactions.Count != currentNumberOfSplits) {
                            throw new Exception("Missing a split!");
                        }

                        groupedTransactions.Add(new Transaction(accountTypes, useClear, culture, splitTransactions.Values.OrderBy(v => v.Memo).ToList()));
                        splitTransactions.Clear();

                        currentNumberOfSplits = int.Parse(m.Groups[2].Value);
                    }

                    splitTransactions.Add(splitNumber, record);
                } else {
                    appendBufferedSplitTransactions();
                    groupedTransactions.Add(new Transaction(accountTypes, useClear, culture, record));
                }
            }

            // append any remaining split transaction
            appendBufferedSplitTransactions();

            // remove the transfers that have an inflow
            // since YNAB isn't double entry accounting, transfers are listed twice, once per account
            // this removes all tranfers from the account that recieves the money and keeps the transfer
            // in the the account the money is withdrawn from
            groupedTransactions = groupedTransactions.Where(t => t.LineItems.Count > 1 || !t.LineItems.First().Payee.Contains("Transfer : ") || (t.LineItems.First().Payee.Contains("Transfer : ") && t.LineItems.First().HasOutflow)).ToList();

            return groupedTransactions;
        }

        /// <summary>
        /// Creates the text of the plain text accounting file
        /// </summary>
        /// <param name="transactions">The list of transactions to include in the ledger</param>
        /// <returns>The contents of the ledger file as a string</returns>
        public static string CreateLedger(IList<Transaction> transactions) {
            StringBuilder sb = new StringBuilder();
            var finalData = transactions.OrderBy(t => t.Date)
                                        .ThenBy(gt => gt.LineItems.Sum(t => t.InflowAmount))
                                        .ThenBy(gt => gt.LineItems.Sum(t => t.OutflowAmount))
                                        .ThenBy(gt => gt.LineItems.First().Payee);
            foreach (Transaction groupedTransaction in finalData) {
                sb.AppendLine(groupedTransaction.ToString());
            }

            return $"{sb.ToString().Trim()}{Environment.NewLine}";
        }

        /// <summary>
        /// Converts a string from 'Y', 'y', 'N', or 'n' to a boolean true or false.
        /// If the value is empty then <paramref name="valWhenEmpty" /> is returned.
        /// </summary>
        /// <param name="input">The input string to parse</param>
        /// <param name="valWhenEmpty">The value to return when <paramref name="input" /> is empty</param>
        /// <exception cref="Exception">Thrown when <paramref name="input" /> is not in an approved form</exception>
        /// <returns>true if the input is 'Y', or 'y'; false if 'N', or 'n'</returns>
        private static bool StringToBool(string input, bool valWhenEmpty) {
            if (string.IsNullOrWhiteSpace(input)) {
                return valWhenEmpty;
            } else {
                if (input.Trim().ToUpper() == "Y") {
                    return true;
                } else if (input.Trim().ToUpper() == "N") {
                    return false;
                } else {
                    throw new Exception("Unexpected input value.");
                }
            }
        }
    }
}