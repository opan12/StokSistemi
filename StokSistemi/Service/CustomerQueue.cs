namespace StokSistemi.Service
{
    using System.Collections.Generic;
    using System.Threading;
    using StokSistemi.Models;

    public class CustomerQueue
    {
        private readonly Queue<Customer> _customerQueue = new Queue<Customer>();

        public void AddCustomerToQueue(Customer customer)
        {
            lock (_customerQueue)
            {
                _customerQueue.Enqueue(customer);
            }
        }

        public Customer GetNextCustomer()
        {
            lock (_customerQueue)
            {
                return _customerQueue.Count > 0 ? _customerQueue.Dequeue() : null;
            }
        }
    }

}
/*
 * namespace YAZLAB2.Service
{
    using System.Collections.Generic;
    using System.Threading;
    using YAZLAB2.Models;

    public class CustomerQueue
    {
        private readonly Queue<Customer> _customerQueue = new Queue<Customer>();
        private readonly Mutex _mutex = new Mutex();

        public void AddCustomerToQueue(Customer customer)
        {
            _mutex.WaitOne();
            try
            {
                _customerQueue.Enqueue(customer);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public Customer GetNextCustomer()
        {
            _mutex.WaitOne();
            try
            {
                return _customerQueue.Count > 0 ? _customerQueue.Dequeue() : null;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public int Count
        {
            get
            {
                _mutex.WaitOne();
                try
                {
                    return _customerQueue.Count;
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }
    }
}*/