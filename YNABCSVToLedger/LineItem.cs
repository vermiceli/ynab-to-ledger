namespace YNABCSVToLedger {
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class representing a line item in the ledger
    /// </summary>
    public class LineItem {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineItem" /> class
        /// </summary>
        /// <param name="lineItem">The line item as specified from the YNAB-exported CSV</param>
        /// <param name="culture">The culture to use when parsing the amounts</param>
        public LineItem(CSVLineItem lineItem, CultureInfo culture) {
            this.CultureInfo = culture;

            this.Account = lineItem.Account;
            this.Payee = lineItem.Payee;
            this.Category = lineItem.Category;
            this.Inflow = string.IsNullOrWhiteSpace(lineItem.Inflow) ? string.Empty : lineItem.Inflow.Trim();
            this.MasterCategory = lineItem.MasterCategory;
            this.Memo = lineItem.Memo;
            this.Outflow = string.IsNullOrWhiteSpace(lineItem.Outflow) ? string.Empty : lineItem.Outflow.Trim();
            this.Payee = lineItem.Payee;
            this.SubCategory = lineItem.SubCategory;

            // YNAB appends a . for currency symbols it abbreviates, e.g. 12,48ден.
            this.Inflow = this.Inflow.TrimEnd().TrimEnd(new char[] { '.' });
            this.Outflow = this.Outflow.TrimEnd().TrimEnd(new char[] { '.' });
        }

        /// <summary>
        /// Gets or sets the account that the money is coming into or coming out of
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Gets or sets the person who either received or paid the money specified
        /// </summary>
        public string Payee { get; set; }

        /// <summary>
        /// Gets or sets the complete category.
        /// It should be equal to <see cref="MasterCategory"/>:<see cref="SubCategory"/>
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the top-level category of the line item
        /// </summary>
        public string MasterCategory { get; set; }

        /// <summary>
        /// Gets or sets the specific category of the line item
        /// </summary>
        public string SubCategory { get; set; }

        /// <summary>
        /// Gets or sets a comment associated with the line item
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// Gets the comment without any leading (Split i/n) text
        /// </summary>
        public string MemoWithoutSplit {
            get {
                if (string.IsNullOrWhiteSpace(this.Memo)) {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(this.Memo) && this.Memo.IndexOf("Split") > 0) {
                    Match m = Regex.Match(this.Memo, @"\(Split (\d+)/(\d+)\)(.*)");
                    if (m.Success) {
                        if (m.Groups.Count == 4 && !string.IsNullOrWhiteSpace(m.Groups[3].Value)) {
                            string memo = m.Groups[3].Value;
                            if (string.IsNullOrWhiteSpace(memo)) {
                                return null;
                            }

                            return memo.Trim();
                        } else {
                            // no text was found after the '(Split i/n)' memo
                            return null;
                        }
                    }
                }

                return this.Memo.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the outflow of the transaction, if no outflow, it is $0.00 when using USD
        /// </summary>
        public string Outflow { get; set; }

        /// <summary>
        /// Gets or sets the outflow amount as a decimal
        /// </summary>
        /// <remarks>For some currencies YNAB appends a period (I assume to mean it's an abbreviation)</remarks>
        public decimal OutflowAmount => decimal.Parse(this.Outflow.Replace(this.CultureInfo.NumberFormat.CurrencySymbol, string.Empty), this.CultureInfo);

        /// <summary>
        /// Gets or sets the inflow of the transaction. If there is no inflow, it is $0.00 when using USD
        /// </summary>
        public string Inflow { get; set; }

        /// <summary>
        /// Gets or sets the inflow amount as a decimal
        /// </summary>
        /// <remarks>For some currencies YNAB appends a period (I assume to mean it's an abbreviation)</remarks>
        public decimal InflowAmount => decimal.Parse(this.Inflow.Replace(this.CultureInfo.NumberFormat.CurrencySymbol, string.Empty), this.CultureInfo);

        /// <summary>
        /// Gets or sets a value indicating whether or not the line item has any outflow
        /// </summary>
        public bool HasOutflow => !string.IsNullOrWhiteSpace(this.Outflow) && this.OutflowAmount > 0;

        /// <summary>
        /// Gets or sets a value indicating whether or not the line item has any inflow
        /// </summary>
        public bool HasInflow => !string.IsNullOrWhiteSpace(this.Inflow) && this.InflowAmount > 0;

        /// <summary>
        /// Gets the culture to use when parsing the numbers
        /// </summary>
        private CultureInfo CultureInfo { get; }
    }
}