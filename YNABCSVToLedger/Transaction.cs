namespace YNABCSVToLedger {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using CsvHelper.Configuration.Attributes;

    /// <summary>
    /// A representation of a transaction, could be a split transaction
    /// </summary>
    public class Transaction {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction" /> class
        /// </summary>
        /// <param name="accountTypes">The mapping of account to its type (Asset/Liability)</param>
        /// <param name="useClear">Whether or not to include the clear fields (*/!) in output</param>
        /// <param name="culture">The culture to use when parsing the amounts</param>
        /// <param name="lineItems">A list of line items to include in the transaction</param>
        public Transaction(
            IDictionary<string, string> accountTypes,
            bool useClear,
            CultureInfo culture,
            IList<CSVLineItem> lineItems) : this(accountTypes, useClear, culture) {
            if (lineItems == null) {
                throw new ArgumentNullException(nameof(lineItems), "Line items are required.");
            } else if (!lineItems.Any()) {
                throw new Exception("No transactions!");
            } else if (lineItems.GroupBy(t => new { t.Flag, t.CheckNumber, t.Date, t.Cleared, t.RunningBalance }).Count() != 1) {
                throw new Exception("These should all be the same.");
            }

            var firstRecord = lineItems.First();
            this.Flag = firstRecord.Flag;
            this.CheckNumber = firstRecord.CheckNumber;
            this.Date = firstRecord.Date;
            this.Cleared = firstRecord.Cleared;
            this.RunningBalance = firstRecord.RunningBalance;
            foreach (var record in lineItems) {
                this.LineItems.Add(new LineItem(record, this.Culture));
            }

            this.LineItems = this.LineItems.OrderBy(li => li.InflowAmount)
                                           .ThenBy(li => li.OutflowAmount)
                                           .ThenBy(li => li.Payee)
                                           .ToList();

            if (this.LineItems.Any(r => !r.HasOutflow && !r.HasInflow)) {
                throw new Exception("A single transaction can't be have both inflow and outflow");
            } else if (this.LineItems.Count() > 1 && this.LineItems.Any(l => l.Payee.Contains("Transfer : "))) {
                throw new Exception("A split transfer is not supported.");
            } else if (this.LineItems.Any(r => r.HasOutflow && r.HasInflow)) {
                Console.WriteLine("Warning: record doesn't transfer any money.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction" /> class
        /// </summary>
        /// <param name="accountTypes">The mapping of account to its type (Asset/Liability)</param>
        /// <param name="useClear">Whether or not to include the clear fields (*/!) in output</param>
        /// <param name="culture">The culture to use when parsing the amounts</param>
        /// <param name="lineItem">A line items to include in the transaction</param>
        public Transaction(
            IDictionary<string, string> accountTypes,
            bool useClear,
            CultureInfo culture,
            CSVLineItem lineItem) : this(accountTypes, useClear, culture) {
            this.Flag = lineItem.Flag;
            this.CheckNumber = lineItem.CheckNumber;
            this.Date = lineItem.Date;
            this.Cleared = lineItem.Cleared;
            this.RunningBalance = lineItem.RunningBalance;
            LineItem transactionLineItem = new LineItem(lineItem, this.Culture);
            this.LineItems.Add(transactionLineItem);

            if (!transactionLineItem.HasOutflow && !transactionLineItem.HasInflow) {
                Console.WriteLine("Warning: record doesn't transfer any money.");
            } else if (transactionLineItem.HasOutflow && transactionLineItem.HasInflow) {
                Console.WriteLine("A single transaction has both inflow and outflow.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction" /> class
        /// </summary>
        /// <param name="accountTypes">The mapping of account to its type (Asset/Liability)</param>
        /// <param name="useClear">Whether or not to include the clear fields (*/!) in output</param>
        /// <param name="culture">The culture to use when parsing the amounts</param>
        private Transaction(IDictionary<string, string> accountTypes, bool useClear, CultureInfo culture) {
            this.AccountTypes = accountTypes;
            this.UseClear = useClear;
            this.Culture = culture;
            this.LineItems = new List<LineItem>();
        }

        /// <summary>
        /// Gets a mapping of the account types in the ledger
        /// </summary>
        public IDictionary<string, string> AccountTypes { get; }

        /// <summary>
        /// Gets the culture to use when parsing the amounts
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets a value indicating whether or not to include the clear fields (*/!) in payee field
        /// </summary>
        public bool UseClear { get; }

        /// <summary>
        /// Gets or sets the flag as specified from YNAB.
        /// Usually a color: Red, Orange, Yellow, Green, Blue, Purple
        /// </summary>
        public string Flag { get; set; }

        /// <summary>
        /// Gets or sets the check number for the line item
        /// </summary>
        [Name("Check Number")]
        public string CheckNumber { get; set; }

        /// <summary>
        /// Gets or sets the date the transaction occurred
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the cleared status as reported by YNAB: C/U
        /// </summary>
        public string Cleared { get; set; }

        /// <summary>
        /// Gets or sets the running balance of the account
        /// </summary>
        [Name("Running Balance")]
        public string RunningBalance { get; set; }

        /// <summary>
        /// Gets or sets total amount changed with this transaction, including all inflow and outflow
        /// </summary>
        public decimal TotalAmount => this.LineItems.Any() ? this.LineItems.Sum(t => t.InflowAmount - t.OutflowAmount) : 0M;

        /// <summary>
        /// Gets the list of line items in the transaction
        /// </summary>
        public IList<LineItem> LineItems { get; }

        /// <summary>
        /// The transaction as it will appear in the ledger
        /// </summary>
        /// <returns>A string representation of the transaction</returns>
        public override string ToString() {
            NumberFormatInfo pattern = (NumberFormatInfo)this.Culture.NumberFormat.Clone();
            pattern.CurrencyNegativePattern = 1;
            StringBuilder sb = new StringBuilder();
            bool hasMultipleLineItems = this.LineItems.Count > 1;
            bool hasMultiplePayees = this.LineItems.Select(t => t.Payee).Distinct().Count() > 1;
            bool hasMultipleAccounts = this.LineItems.Select(t => t.Account).Distinct().Count() > 1;
            bool isTransfer = this.LineItems.Any(i => i.Payee.Contains("Transfer : "));

            string date = this.Date.ToString("yyyy-MM-dd");
            string cleared = this.UseClear ? (this.Cleared == "C" ? "* " : "! ") : string.Empty;
            string checkNumber = !string.IsNullOrWhiteSpace(this.CheckNumber) ? $"({this.CheckNumber.Trim()}) " : string.Empty;
            string payee;
            if (hasMultiplePayees) {
                payee = "Multiple Payees";
            } else if (isTransfer) {
                payee = string.Empty;
            } else {
                payee = this.LineItems.First().Payee;
            }

            string line1 = $"{date} {checkNumber}{cleared}{payee}";
            sb.AppendLine(line1.Trim());

            // if it's a single transaction and has a memo, include it under the first line
            if (!hasMultipleLineItems && !string.IsNullOrWhiteSpace(this.LineItems.First().Memo)) {
                sb.AppendLine($" ; {this.LineItems.First().Memo.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(this.Flag)) {
                sb.AppendLine($" ; :{this.Flag}:");
            }

            string accountType = this.AccountTypes[this.LineItems.First().Account];
            sb.AppendLine($" {accountType}:{this.LineItems.First().Account}  {this.TotalAmount.ToString("C", pattern)}");

            foreach (var transaction in this.LineItems) {
                // don't include the memo only one line item because it's included below the payee line
                string memo = transaction.MemoWithoutSplit;
                bool hasMemo = !string.IsNullOrWhiteSpace(memo) && hasMultipleLineItems;
                string commentPrefix = hasMultiplePayees || hasMemo ? " ;" : string.Empty;
                string payeeComment = hasMultiplePayees ? $"Payee: {transaction.Payee}" : null;
                accountType = this.AccountTypes[transaction.Account];
                memo = !hasMemo ? null : $"{(hasMultiplePayees ? "," : string.Empty)} {memo}";

                if (transaction.HasInflow) {
                    sb.AppendLine($" Income:{transaction.Payee}  -{transaction.InflowAmount.ToString("C", pattern)}");
                    if (hasMultipleAccounts) {
                        sb.AppendLine($" {accountType}:{transaction.Account}  {transaction.InflowAmount.ToString("C", pattern)}");
                    }
                } else if (isTransfer) {
                    // transfer payee is an account
                    string transferPayee = transaction.Payee.Replace("Transfer : ", string.Empty);
                    string transferAccountType = this.AccountTypes[transferPayee];
                    sb.AppendLine($" {transferAccountType}:{transferPayee}  {transaction.OutflowAmount.ToString("C", pattern)}");
                } else {
                    sb.AppendLine($" Expenses:{transaction.MasterCategory}:{transaction.SubCategory}  {transaction.Outflow}{commentPrefix}{payeeComment}{memo}");
                }
            }

            return sb.ToString();
        }
    }
}