using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Transactions;
using ArkanParking.BL.Interfaces;
using ArkanParking.BL.Models;

namespace ArkanParking.BL.Services
{
    public class ParkingService : IParkingService
{
    private readonly List<Vehicle> _vehicles;
    private decimal _parkingBalance;
    private readonly List<TransactionInfo> _transactions;
    private readonly int _capacity;
    private readonly ILogService _logService;
    private readonly ITimerService _withdrawTimer;
    private readonly ITimerService _logTimer;

    private static ParkingService _instance;
    private static readonly object _lock = new object();

    public ParkingService(ITimerService withdrawTimer, ITimerService logTimer, ILogService logService)
    {
        _withdrawTimer = withdrawTimer ?? throw new ArgumentNullException(nameof(withdrawTimer));
        _logTimer = logTimer ?? throw new ArgumentNullException(nameof(logTimer));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _capacity = Settings.ParkingCapacity;

        lock (_lock)
        {
            if (_instance == null)
            {
                _vehicles = new List<Vehicle>();
                _transactions = new List<TransactionInfo>();
                _parkingBalance = 0;
                _instance = this;

                InitializeTimers();
            }
            else
            {
                _vehicles = _instance._vehicles;
                _transactions = _instance._transactions;
                _parkingBalance = _instance._parkingBalance;
            }
        }
    }

        private void LogParkingState(string action, string details = "")
        {
            var message = $"[Парковка] {action}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | {details}";
            }
            _logService.Write(message);
        }

        private void LogTransaction(Vehicle vehicle, decimal amount, string type)
        {
            var message = $"[Транзакція] {type} | " +
                         $"Транспорт: {vehicle.Id} | " +
                         $"Сума: {amount} у.о. | " +
                         $"Тип: {vehicle.VehicleType} | " +
                         $"Поточний баланс: {vehicle.Balance}";
            _logService.Write(message);
        }

        private void InitializeTimers()
        {
            _withdrawTimer.Interval = Settings.FeePeriodSeconds * 6000;
            _withdrawTimer.Elapsed += (sender, e) => ChargeVehicles();
            _withdrawTimer.Start();

            _logTimer.Interval = Settings.LogPeriodSeconds * 1000;
            _logTimer.Elapsed += OnLogTimerElapsed;
            _logTimer.Start();

            LogParkingState("Таймери ініціалізовано");
        }

        private void OnLogTimerElapsed(object sender, EventArgs e)
        {
            lock (_lock)
            {
                LogParkingState("Статус", $"Поточний баланс: {_parkingBalance} у.о. | Вільних місць: {GetFreePlaces()}");
            }
        }

        public decimal GetBalance() => _parkingBalance;

    public int GetCapacity() => _capacity;

    public int GetFreePlaces() => _capacity - _vehicles.Count;

    public ReadOnlyCollection<Vehicle> GetVehicles() => _vehicles.AsReadOnly();

        public void AddVehicle(Vehicle vehicle)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));

            lock (_lock)
            {
                if (_vehicles.Count >= _capacity)
                {
                    LogParkingState("Помилка додавання", $"Транспорт {vehicle.Id} | Причина: Парковка заповнена");
                    throw new InvalidOperationException("Парковка заповнена!");
                }
                if (_vehicles.Any(v => v.Id == vehicle.Id))
                {
                    LogParkingState("Помилка додавання", $"Транспорт {vehicle.Id} | Причина: Дублікат ID");
                    throw new ArgumentException("Транспортний засіб з таким ID вже існує.");
                }

                _vehicles.Add(vehicle);
                LogParkingState("Додано транспорт",
                    $"ID: {vehicle.Id} | Тип: {vehicle.VehicleType} | Баланс: {vehicle.Balance}");
            }
        }

        public void RemoveVehicle(string vehicleId)
        {
            lock (_lock)
            {
                var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId);
                if (vehicle == null)
                {
                    LogParkingState("Помилка видалення", $"Транспорт {vehicleId} | Причина: Не знайдено");
                    throw new ArgumentException("Транспортний засіб незнайдено!");
                }
                if (vehicle.Balance < 0)
                {
                    LogParkingState("Помилка видалення",
                        $"Транспорт {vehicleId} | Причина: Негативний баланс ({vehicle.Balance})");
                    throw new InvalidOperationException("Неможливо видалити транспортний засіб з негативним балансом.");
                }
                _vehicles.Remove(vehicle);
                LogParkingState("Видалено транспорт",
                    $"ID: {vehicle.Id} | Тип: {vehicle.VehicleType} | Фінальний баланс: {vehicle.Balance}");
            }
        }


        public void TopUpVehicle(string vehicleId, decimal amount)
        {
            if (amount <= 0)
            {
                LogParkingState("Помилка поповнення", $"Транспорт {vehicleId} | Причина: Некоректна сума ({amount})");
                throw new ArgumentException("Сума поповнення повинна бути позитивною.");
            }

            lock (_lock)
            {
                var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId);
                if (vehicle == null)
                {
                    LogParkingState("Помилка поповнення", $"Транспорт {vehicleId} | Причина: Не знайдено");
                    throw new ArgumentException("Транспортний засіб незнайдено!");
                }

                decimal oldBalance = vehicle.Balance;
                vehicle.AddBalance(amount);
                LogTransaction(vehicle, amount, "Поповнення");
                LogParkingState("Баланс оновлено",
                    $"ID: {vehicle.Id} | Було: {oldBalance} | Поповнення: +{amount} | Стало: {vehicle.Balance}");
            }
        }

        public void ChargeVehicles()
        {
            lock (_lock)
            {
                LogParkingState("Початок стягнення оплати", $"Кількість транспорту: {_vehicles.Count}");

                foreach (var vehicle in _vehicles)
                {
                    decimal fee = Settings.GetFee(vehicle.VehicleType);
                    decimal totalCharge;
                    string chargeType;

                    if (vehicle.Balance >= fee)
                    {
                        vehicle.DeductBalance(fee);
                        _parkingBalance += fee;
                        totalCharge = fee;
                        chargeType = "Стандартна оплата";
                    }
                    else
                    {
                        decimal deficit = fee - vehicle.Balance;
                        decimal penalty = deficit * Settings.PenaltyCoefficient;
                        vehicle.DeductBalance(fee + penalty);
                        _parkingBalance += fee + penalty;
                        totalCharge = fee + penalty;
                        chargeType = "Оплата зі штрафом";
                    }

                    var transaction = new TransactionInfo
                    {
                        VehicleId = vehicle.Id,
                        TransactionDate = DateTime.Now,
                        Sum = totalCharge,
                        VehicleType = vehicle.VehicleType
                    };

                    _transactions.Add(transaction);
                    LogTransaction(vehicle, totalCharge, chargeType);
                }

                LogParkingState("Завершення стягнення оплати",
                    $"Оброблено транспорту: {_vehicles.Count} | Новий баланс парковки: {_parkingBalance}");
            }
        }

        public TransactionInfo[] GetLastParkingTransactions()
    {
        lock (_lock)
        {
            return _transactions
                .OrderByDescending(t => t.TransactionDate)
                .ToArray();
        }
    }

    public string ReadFromLog() => _logService.Read();

    public void Dispose()
    {
        lock (_lock)
        {
            _withdrawTimer?.Dispose();
            _logTimer?.Dispose();
            _logService?.Dispose();
            
            if (this == _instance)
            {
                _instance = null;
                _vehicles?.Clear();
                _transactions?.Clear();
                _parkingBalance = 0;
            }
        }
    }
}
}