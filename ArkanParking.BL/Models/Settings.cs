using System;
using System.Collections.Generic;

namespace ArkanParking.BL.Models;
// TODO:створи клас Settings.
// Деталі реалізації залишаються на твій розсуд, вони повинні лише відповідати вимогам домашнього завдання.

public static class Settings
{
        public const int ParkingCapacity = 10;
        public const decimal PenaltyCoefficient = 2.5m;
        public const int FeePeriodSeconds = 5;
        public const int LogPeriodSeconds = 60;

        public static decimal GetFee(VehicleType type)
        {
            return type switch
            {
                VehicleType.PassengerCar => 2.0m,
                VehicleType.Truck => 5.0m,
                VehicleType.Bus => 3.5m,
                VehicleType.Motorcycle => 1.5m,
                _ => throw new ArgumentException("Невірний тип транспорту")
            };
        }
}
