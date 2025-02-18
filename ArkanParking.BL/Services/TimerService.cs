using System;
using System.Timers;
using ArkanParking.BL.Interfaces;

namespace ArkanParking.BL.Services 
{
    public class TimerService : ITimerService, IDisposable
    {
        private readonly Timer timer;
        private Action currentAction;
        private bool isDisposed;

        public event ElapsedEventHandler Elapsed;

        public double Interval
        {
            get => timer.Interval;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Інтервал має бути більше нуля.", nameof(value));
                timer.Interval = value;
            }
        }

        public bool IsActive => timer.Enabled;  

        public TimerService()
        {
            timer = new Timer();
            timer.AutoReset = true;
            timer.Elapsed += OnElapsed;
            isDisposed = false;
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                currentAction?.Invoke();
                Elapsed?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка зворотного виклику таймера: {ex.Message}");
            }
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (timer.Enabled)
            {
                Console.WriteLine("Таймер уже працює.");
                return;
            }
            timer.Start();
            Console.WriteLine($"Таймер запустився о {DateTime.Now}. Інтервал: {Interval}мс");
        }

        public void Stop()
        {
            ThrowIfDisposed();
            timer.Stop();
            Console.WriteLine($"Таймер зупинився на {DateTime.Now}");
        }

        public void Start(Action action, int interval)
        {
            ThrowIfDisposed();
            
            if (interval <= 0)
                throw new ArgumentException("Інтервал має бути більше нуля.", nameof(interval));

            currentAction = action ?? throw new ArgumentNullException(nameof(action));
            Interval = interval;
            
            Start();
        }

        public void StartCharging(Action chargeAction)
        {
            ThrowIfDisposed();

            if (chargeAction == null)
                throw new ArgumentNullException(nameof(chargeAction));

            Stop();

            const int chargingInterval = 60000; 
            currentAction = () =>
            {
                try
                {
                    chargeAction();
                    Console.WriteLine($"Дія зарядки виконана о {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка заряджання: {ex.Message}");
                }
            };

            Start(currentAction, chargingInterval);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    Stop();
                    timer.Elapsed -= OnElapsed;
                    timer.Dispose();
                }
                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(TimerService));
            }
        }
    }
}