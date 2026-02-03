using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FitnessBot.Core.Abstractions;
using FitnessBot.Core.Entities;

namespace FitnessBot.Core.Services
{
    public class BmiService
    {
        private readonly IBmiRepository _bmiRepository;

        public BmiService(IBmiRepository bmiRepository)
        {
            _bmiRepository = bmiRepository;
        }

        public (double bmi, string category, string recommendation) Calculate(double heightCm, double weightKg)
        {
            double heightM = heightCm / 100.0;
            double bmi = weightKg / Math.Pow(heightM, 2);

            string category;
            if (bmi < 18.5) category = "Недостаточная масса тела";
            else if (bmi < 25) category = "Норма";
            else if (bmi < 30) category = "Избыточная масса тела";
            else category = "Ожирение";

            string recommendation = category switch
            {
                "Недостаточная масса тела" =>
                    "Рекомендуется набрать вес за счёт увеличения калорийности и белка.",
                "Норма" =>
                    "Рекомендуется поддерживать текущий вес и уровень активности.",
                "Избыточная масса тела" =>
                    "Рекомендуется снизить вес: уменьшить калорийность рациона и увеличить активность.",
                _ =>
                    "Рекомендуется обратиться к врачу и составить индивидуальный план снижения веса."
            };

            return (Math.Round(bmi, 1), category, recommendation);
        }

        public async Task<BmiRecord> SaveMeasurementAsync(long userId, double heightCm, double weightKg)
        {
            var (bmi, category, recommendation) = Calculate(heightCm, weightKg);

            var record = new BmiRecord
            {
                UserId = userId,
                HeightCm = heightCm,
                WeightKg = weightKg,
                Bmi = bmi,
                Category = category,
                Recommendation = recommendation,
                MeasuredAt = DateTime.UtcNow
            };

            await _bmiRepository.SaveAsync(record);
            return record;
        }

        public Task<BmiRecord?> GetLastAsync(long userId) => _bmiRepository.GetLastAsync(userId);
    }

}
