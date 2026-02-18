namespace Domain.Models
{
    /// <summary>
    /// Represents the status of an order in the fulfillment process.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Order has been received and is awaiting processing.
        /// </summary>
        Received = 0,

        /// <summary>
        /// Order is being picked from the warehouse.
        /// </summary>
        Picking = 1,

        /// <summary>
        /// Order has been dispatched for delivery.
        /// </summary>
        Dispatched = 2,

        /// <summary>
        /// Order has been delivered to the customer.
        /// </summary>
        Delivered = 3
    }
}
