using ParceleMais.Serialization;

namespace ParceleMais.Orders.Models;

public enum OrderStatus
{
    Undefined = 0,
    Analysing = 1,
    Approved = 2,
    UnavailableBalance = 3,
    AnalysisExpired = 4,
    PendingPayment = 5,
    BiometryRefused = 6,
    BiometryApproved = 7,
    PaymentRefused = 8,
    Purchased = 9,
    Unauthorized = 10,
    PendingAuthorization = 11,
    AwaitingRegistration = 12,
    SaleNotStarted = 13,
    Canceled = 14,
    Billing = 15,
    Completed = 16,
    Frozen = 17,
    PendingPaymentConfirmation = 18,
    Disbursed = 19,

    [UnknownValue]
    Unknown = -1
}
