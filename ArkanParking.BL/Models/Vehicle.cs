using System;
using System.Text.RegularExpressions;

namespace ArkanParking.BL.Models;
// TODO: створи клас Vehicle.
// Властивості: Id (типу string), VehicleType (типу VehicleType), Balance (типу decimal).
// Формат ідентифікатора описаний у завданні.
// Id та VehicleType не повинні змінюватись після встановлення значень.
// Властивість Balance має змінюватись лише в проєкті CoolParking.BL.
// Тип конструктора показаний у тестах, і він повинен мати валідацію, яка також зрозуміла з тестів.
// Статичний метод GenerateRandomRegistrationPlateNumber повинен повертати випадково згенерований унікальний ідентифікатор.


    public class Vehicle
    {
        public string Id { get; }
        public VehicleType VehicleType { get; }
        public decimal Balance { get; private set; }

        public Vehicle(string id, VehicleType vehicleType, decimal balance)
        {
            if (!IsValidId(id))
            {
                throw new ArgumentException("Неправильний формат номерного знаку");
            }
            if (balance < 0)
            {
                throw new ArgumentException("Початковий баланс не може бути від'ємним");
            }
            Id = id;
            VehicleType = vehicleType;
            Balance = balance;
        }

        public void AddBalance(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Сума поповнення повинна бути позитивною");
            }
            Balance += amount;
        }

        public void DeductBalance(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Сума списання повинна бути позитивною");
            }
            Balance -= amount;
        }

        public static string GenerateRandomRegistrationPlateNumber()
        {
            var random = new Random();
            string letters = $"{(char)random.Next('A', 'Z' + 1)}{(char)random.Next('A', 'Z' + 1)}";
            string numbers = random.Next(1000, 9999).ToString();
            string lastLetters = $"{(char)random.Next('A', 'Z' + 1)}{(char)random.Next('A', 'Z' + 1)}";
            return $"{letters}-{numbers}-{lastLetters}";
        }

        private static bool IsValidId(string id)
        {
            string pattern = @"^[A-Z]{2}-\d{4}-[A-Z]{2}$";
            return Regex.IsMatch(id, pattern);
        }
    }

