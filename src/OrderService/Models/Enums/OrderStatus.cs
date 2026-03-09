namespace OrderService.Models.Enums
{
    /// <summary>
    /// TODO: Cancelled status will be expanded to support WSO2-routed refund flow in a future phase.
    /// </summary>
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }
}
