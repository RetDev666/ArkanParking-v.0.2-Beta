using System;
using System.Text;
using ArkanParking.BL.Interfaces;
using ArkanParking.BL.Models;
using ArkanParking.BL.Services;

namespace ArkanParking.BL.ConsoleApp
{
    class Program
    {
        static Program()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }

        static void Main(string[] args)
        {
            ILogService logService = new LogService("Transactions.log");
            ITimerService withdrawTimer = new TimerService();
            ITimerService logTimer = new TimerService();
            IParkingService parkingService = new ParkingService(withdrawTimer, logTimer, logService);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("--- Меню Паркінгу ---");
                Console.WriteLine("1. Показати поточний баланс паркінгу");
                Console.WriteLine("2. Показати кількість вільних місць");
                Console.WriteLine("3. Додати транспортний засіб");
                Console.WriteLine("4. Видалити транспортний засіб");
                Console.WriteLine("5. Поповнити баланс транспортного засобу");
                Console.WriteLine("6. Показати всі транспортні засоби");
                Console.WriteLine("7. Прочитати лог транзакцій");
                Console.WriteLine("8. Перевірити статус таймерів.");
                Console.WriteLine("0. Вийти");

                Console.Write("Оберіть опцію: ");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Console.WriteLine($"Поточний баланс паркінгу: {parkingService.GetBalance()} у.о.");
                        break;
                    case "2":
                        Console.WriteLine($"Вільних місць: {parkingService.GetFreePlaces()} з {parkingService.GetCapacity()}");
                        break;
                    case "3":
                        AddVehicleMenu(parkingService);
                        break;
                    case "4":
                        RemoveVehicleMenu(parkingService);
                        break;
                    case "5":
                        TopUpVehicleMenu(parkingService);
                        break;
                    case "6":
                        ShowAllVehicles(parkingService);
                        break;
                    case "7":
                        Console.WriteLine(parkingService.ReadFromLog());
                        break;
                    case "8":
                        ShowTimerStatus(withdrawTimer, logTimer);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Невірна опція. Спробуйте ще раз.");
                        break;
                }
                Console.WriteLine("Натисніть будь-яку клавішу для продовження...");
                Console.ReadKey();
            }
        }

        private static void ShowTimerStatus(ITimerService withdrawTimer, ITimerService logTimer)
        {
            Console.WriteLine("\n--- Статус таймерів ---");
            
            Console.WriteLine("Таймер списання коштів:");
            Console.WriteLine($"- Інтервал: {withdrawTimer.Interval} мс");
            Console.WriteLine($"- Активний: {(withdrawTimer.IsActive ? "Так" : "Ні")}");
            
            Console.WriteLine("\nТаймер логування:");
            Console.WriteLine($"- Інтервал: {logTimer.Interval} мс");
            Console.WriteLine($"- Активний: {(logTimer.IsActive ? "Так" : "Ні")}");
        }

        private static void AddVehicleMenu(IParkingService parkingService)
        {
            Console.Write("Введіть ID транспортного засобу: ");
            string id = Console.ReadLine();
            Console.Write("Оберіть тип (1.Легкова машина, 2.Грузова машина, 3.Автобус, 4.Мотоцикил): ");
            string typeInput = Console.ReadLine();
            switch (typeInput)
            {
                case "1":
                   typeInput = "PassengerCar";
                    break;
                case "2":
                    typeInput = "Truck";
                    break;
                case "3":
                    typeInput = "Bus";
                    break;
                case "4":
                    typeInput = "Motorcycle";
                    break;
                default:
                    Console.WriteLine("Невірно вибарана опція!");
                    break;
            }
            Console.Write("Введіть початковий баланс: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal balance) || balance < 0)
            {
                Console.WriteLine("Невірний формат балансу.");
                return;
            }

            if (Enum.TryParse<VehicleType>(typeInput, out var type))
            {
                try
                {
                    Vehicle vehicle = new Vehicle(id, type, balance);
                    parkingService.AddVehicle(vehicle);
                    Console.WriteLine("Транспортний засіб додано успішно.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Невірний тип транспортного засобу.");
            }
        }

        private static void RemoveVehicleMenu(IParkingService parkingService)
        {
            Console.Write("Введіть ID транспортного засобу: ");
            string id = Console.ReadLine();
            try
            {
                parkingService.RemoveVehicle(id);
                Console.WriteLine("Транспортний засіб видалено успішно.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
            }
        }

        private static void TopUpVehicleMenu(IParkingService parkingService)
        {
            Console.Write("Введіть ID транспортного засобу: ");
            string id = Console.ReadLine();
            Console.Write("Введіть суму поповнення: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal amount) && amount > 0)
            {
                try
                {
                    parkingService.TopUpVehicle(id, amount);
                    Console.WriteLine("Баланс успішно поповнено.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Невірна сума поповнення.");
            }
        }

        private static void ShowAllVehicles(IParkingService parkingService)
        {
            var vehicles = parkingService.GetVehicles();
            
            if (vehicles.Count == 0)
            {
                Console.WriteLine("Паркінг порожній.");
            }
            else
            {
                foreach (var vehicle in vehicles)
                {
                    Console.WriteLine($"ID: {vehicle.Id}, Тип: {vehicle.VehicleType}, Баланс: {vehicle.Balance} у.о.");
                }
            }
        }
    }
}