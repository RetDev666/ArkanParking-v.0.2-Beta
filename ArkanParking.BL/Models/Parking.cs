using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ArkanParking.BL.Interfaces;
using ArkanParking.BL.Services;

namespace ArkanParking.BL.Models;
// TODO: створи клас Parking.
// Деталі реалізації залишаються на твій розсуд, вони повинні лише відповідати вимогам
// завдання та бути узгодженими з іншими класами та тестами.
    public class Parking : IParkingService
    {
        private readonly List<Vehicle> vehicles;
        private decimal parkingBalance;
        private readonly int capacity;
        private readonly ILogService logService;

        public Parking(int capacity, string logPath)
        {
            this.capacity = capacity;
            vehicles = new List<Vehicle>();
            parkingBalance = 0;
            logService = new LogService(logPath);
        }

        public decimal GetBalance() => parkingBalance;

        public int GetCapacity() => capacity;

        public int GetFreePlaces() => capacity - vehicles.Count;

        public ReadOnlyCollection<Vehicle> GetVehicles() => vehicles.AsReadOnly();

        public void AddVehicle(Vehicle vehicle)
        {
            if (vehicles.Count >= capacity)
            {
                throw new InvalidOperationException("Парковка заповнена");
            }
            if (vehicles.Any(v => v.Id == vehicle.Id))
            {
                throw new ArgumentException("Транспортний засіб з таким ID вже існує");
            }
            vehicles.Add(vehicle);
            logService.Write($"Транспорт {vehicle.Id} додано до паркінгу");
        }

        public void RemoveVehicle(string vehicleId)
        {
            var vehicle = vehicles.FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null)
            {
                throw new ArgumentException("Транспортний засіб незнайдено");
            }
            if (vehicle.Balance < 0)
            {
                throw new InvalidOperationException("Неможливо видалити транспортний засіб з негативним балансом");
            }
            vehicles.Remove(vehicle);
            logService.Write($"Транспорт {vehicle.Id} видалено з паркінгу");
        }

        public void TopUpVehicle(string vehicleId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Сума поповнення повинна бути позитивною");
            }
            var vehicle = vehicles.FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null)
            {
                throw new ArgumentException("Транспортний засіб незнайдено");
            }
            vehicle.AddBalance(amount);
            logService.Write($"Поповнено баланс {vehicle.Id} на {amount} у.о.");
        }

        public void ChargeVehicles()
        {
            foreach (var vehicle in vehicles)
            {
                decimal fee = Settings.GetFee(vehicle.VehicleType);
                if (vehicle.Balance >= fee)
                {
                    vehicle.DeductBalance(fee);
                    parkingBalance += fee;
                    logService.Write($"Стягнено {fee} з автомобіля {vehicle.Id}. Залишок балансу: {vehicle.Balance}");
                }
                else
                {
                    decimal deficit = fee - vehicle.Balance;
                    decimal penalty = deficit * Settings.PenaltyCoefficient;
                    vehicle.DeductBalance(fee + penalty);
                    parkingBalance += fee + penalty;
                    logService.Write($"Стягнено {fee + penalty} (з штрафом) з транспортного засобу {vehicle.Id}. Баланс став негативним");
                }
            }
        }

        public TransactionInfo[] GetLastParkingTransactions()
        {
            return Array.Empty<TransactionInfo>();
        }

        public string ReadFromLog() => logService.Read();

        public void Dispose() => logService.Dispose();
    }