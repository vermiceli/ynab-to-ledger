namespace YNABCSVToLedger {
    using System;
    using CsvHelper.Configuration.Attributes;

    /// <summary>
    /// Represents a line item from the YNAB-exported CSV file
    /// </summary>
    public class CSVLineItem {
        /// <summary>
        /// Gets or sets the account that the money is coming into or coming out of
        /// </summary>
        public string Account { get; set; }

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
        [Name("Master Category")]
        public string MasterCategory { get; set; }

        /// <summary>
        /// Gets or sets the specific category of the line item
        /// </summary>
        [Name("Sub Category")]
        public string SubCategory { get; set; }

        /// <summary>
        /// Gets or sets a comment associated with the line item
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// Gets or sets the outflow of the transaction. If there is no outflow, it is $0.00 when using USD
        /// </summary>
        public string Outflow { get; set; }

        /// <summary>
        /// Gets or sets the inflow of the transaction. If there is no inflow, it is $0.00 when using USD
        /// </summary>
        public string Inflow { get; set; }

        /// <summary>
        /// Gets or sets the cleared status as reported by YNAB: C/U
        /// </summary>
        public string Cleared { get; set; }

        /// <summary>
        /// Gets or sets the running balance of the account
        /// </summary>
        [Name("Running Balance")]
        public string RunningBalance { get; set; }
    }
}