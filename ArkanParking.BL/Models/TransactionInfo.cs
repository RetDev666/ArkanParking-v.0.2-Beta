using System;

namespace ArkanParking.BL.Models;
// TODO: створи структуру TransactionInfo.
// Обов'язково реалізуй властивість Sum (типу decimal) — використовується у тестах.
// Інші деталі реалізації залишаються на твій розсуд, вони повинні лише відповідати вимогам домашнього завдання.

public struct TransactionInfo
{
    public decimal Sum { get; set; }
    public string VehicleId { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime TransactionDate { get; set; }
    public VehicleType VehicleType { get; set; }

    public TransactionInfo(decimal sum, string vehicleId, DateTime timestamp)
    {
        Sum = sum;
        VehicleId = vehicleId;
        Timestamp = timestamp;
        TransactionDate = DateTime.Now;
        VehicleType = VehicleType.PassengerCar;
    }
}