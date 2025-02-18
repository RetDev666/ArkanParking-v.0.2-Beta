using System;
using System.Transactions;
using ArkanParking.BL.Models;

namespace ArkanParking.BL.Interfaces
{
    public interface ILogService : IDisposable
    {
        string LogPath { get; }
        void Write(string logInfo);
        string Read();
        void LogTransaction(Transaction p0);
        string ReadLog();
        void LogTransaction(TransactionInfo p0);
    }
}