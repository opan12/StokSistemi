using System;
using System.Collections.Generic; // List için gerekli
using System.IO;
using StokSistemi.Data;
using StokSistemi.Models;

namespace StokSistemi.Service
{
    public class LogService
    {
        private int logIdCounter = 1; // Log ID sayacı
        private List<Log> _logEntries; // Log girişlerini saklamak için bir liste
        private readonly ApplicationDbContext _context;


        public LogService(ApplicationDbContext context)
        {
            _logEntries = new List<Log>(); // Log girişlerini başlat
            _context = context;
        }

        // Log kaydı oluşturma
        public void Log(string customerId, string logType, string customerType, string productName, int quantity, string result)
        {
            // Log kaydı oluştur
            var logEntry = new Log
            {
                CustomerID = customerId,
                LogType = logType,
                CustomerType = customerType,
                ProductName = productName,
                Quantity = quantity,
                TransactionTime = DateTime.Now, // İşlem zamanını ayarla
                ResultMessage = result
            };

            // Log kaydını veritabanına ekle
          
                _context.Logs.Add(logEntry); // Log kaydını ekle
                _context.SaveChanges(); // Değişiklikleri kaydet
            
        }

        // Log girişini formatlama
       
        // Tüm logları döndürme metodu
        public List<Log> GetAllLogs()
        {
            return _logEntries; // Log girişlerini döndür
        }
    }
}
