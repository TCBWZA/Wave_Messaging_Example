namespace MessagingExample
{
    /// <summary>
    /// Configuration settings for database seeding.
    /// </summary>
    public class SeedSettings
    {
        /// <summary>
        /// Gets or sets whether database seeding is enabled.
        /// </summary>
        public bool EnableSeeding { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of customers to seed.
        /// </summary>
        public int CustomerCount { get; set; } = 20;

        /// <summary>
        /// Gets or sets the number of products to seed.
        /// </summary>
        public int ProductCount { get; set; } = 20;

        /// <summary>
        /// Gets or sets the number of orders to seed.
        /// </summary>
        public int OrderCount { get; set; } = 50;

        /// <summary>
        /// Gets or sets the minimum number of phone numbers per customer.
        /// </summary>
        public int MinPhoneNumbersPerCustomer { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum number of phone numbers per customer.
        /// </summary>
        public int MaxPhoneNumbersPerCustomer { get; set; } = 3;

        /// <summary>
        /// Gets or sets the minimum number of order items per order.
        /// </summary>
        public int MinOrderItemsPerOrder { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum number of order items per order.
        /// </summary>
        public int MaxOrderItemsPerOrder { get; set; } = 5;
    }
}

