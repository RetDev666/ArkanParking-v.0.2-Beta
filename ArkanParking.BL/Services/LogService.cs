using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using ArkanParking.BL.Interfaces;
using ArkanParking.BL.Models;

namespace ArkanParking.BL.Services;
// TODO: створи клас LogService, що реалізує інтерфейс ILogService.
// Є одна явна вимога — для методу читання, якщо файл не знайдено, потрібно викинути InvalidOperationException.
// Інші деталі реалізації залишаються на твій розсуд, але вони повинні відповідати вимогам інтерфейсу 
// та тестам. Наприклад, у LogServiceTests можна знайти необхідний формат конструктора.

public class LogService : ILogService
{
    public string LogPath { get; }
    private readonly object _lockObject = new object();

    public LogService(string logFilePath)
    {
        LogPath = logFilePath;
    }

    public void Write(string logMessage)
    {
        lock (_lockObject)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now}: {logMessage}{Environment.NewLine}");
        }
    }

    public string Read()
    {
        if (!File.Exists(LogPath))
        {
            throw new InvalidOperationException("Лог-файл не знайдено");
        }

        lock (_lockObject)
        {
            return File.ReadAllText(LogPath);
        }
    }

    public void LogTransaction(TransactionInfo transaction)
    {
        var logMessage = $"Транзакція: VehicleId={transaction.VehicleId}, " +
                       $"Сума: {transaction.Sum}, " +
                       $"Тип: {transaction.VehicleType}, " +
                       $"Дата: {transaction.TransactionDate}";
        Write(logMessage);
    }

    public void LogTransaction(Transaction transaction)
    {
        Write($"Системна транзакція: {transaction}");
    }

    public string ReadLog()
    {
        return Read();
    }

    public void Dispose()
    {
        
    }
}
