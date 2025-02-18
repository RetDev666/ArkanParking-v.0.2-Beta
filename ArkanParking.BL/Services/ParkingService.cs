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

    private void InitializeTimers()
    {
        _withdrawTimer.Interval = Settings.FeePeriodSeconds * 6000;
        _withdrawTimer.Elapsed += (sender, e) => ChargeVehicles();
        _withdrawTimer.Start();

        _logTimer.Interval = Settings.LogPeriodSeconds * 1000;
        _logTimer.Elapsed += OnLogTimerElapsed;
        _logTimer.Start();
    }

    private void OnLogTimerElapsed(object sender, EventArgs e)
    {
        lock (_lock)
        {
            _logService.Write($"Поточний баланс парковки: {_parkingBalance}");
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
                throw new InvalidOperationException("Парковка заповнена!");
            }
            if (_vehicles.Any(v => v.Id == vehicle.Id))
            {
                throw new ArgumentException("Транспортний засіб з таким ID вже існує.");
            }

            _vehicles.Add(vehicle);
            _logService.Write($"Додано транспортний засіб: {vehicle.Id}");
        }
    }

    public void RemoveVehicle(string vehicleId)
    {
        lock (_lock)
        {
            var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null)
            {
                throw new ArgumentException("Транспортний засіб незнайдено!");
            }
            if (vehicle.Balance < 0)
            {
                throw new InvalidOperationException("Неможливо видалити транспортний засіб з негативним балансом.");
            }
            _vehicles.Remove(vehicle);
            _logService.Write($"Видалено транспортний засіб: {vehicle.Id}");
        }
    }

    public void TopUpVehicle(string vehicleId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Сума поповнення повинна бути позитивною.");
        }

        lock (_lock)
        {
            var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId);
            if (vehicle == null)
            {
                throw new ArgumentException("Транспортний засіб незнайдено!");
            }
            vehicle.AddBalance(amount);
            _logService.Write($"Поповнено баланс {vehicle.Id} на {amount} у.о.");
        }
    }

    public void ChargeVehicles()
    {
        lock (_lock)
        {
            foreach (var vehicle in _vehicles)
            {
                decimal fee = Settings.GetFee(vehicle.VehicleType);
                decimal totalCharge;

                if (vehicle.Balance >= fee)
                {
                    vehicle.DeductBalance(fee);
                    _parkingBalance += fee;
                    totalCharge = fee;
                }
                else
                {
                    decimal deficit = fee - vehicle.Balance;
                    decimal penalty = deficit * Settings.PenaltyCoefficient;
                    vehicle.DeductBalance(fee + penalty);
                    _parkingBalance += fee + penalty;
                    totalCharge = fee + penalty;
                }

                var transaction = new TransactionInfo
                {
                    VehicleId = vehicle.Id,
                    TransactionDate = DateTime.Now,
                    Sum = totalCharge,
                    VehicleType = vehicle.VehicleType
                };

                _transactions.Add(transaction);
            }
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