using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Data;
using LinqToDB;
using FitnessBot.Core.Entities;
using FitnessBot.Core.DataAccess.Models;

namespace FitnessBot.Infrastructure.DataAccess
{
    // DataConnection для PostgreSQL через linq2db
    public class PgDataContext : DataConnection
    {
        public PgDataContext(string connectionString)
            : base(ProviderName.PostgreSQL, connectionString)
        {
        }

        // типизированные таблицы
        public ITable<UserModel> Users => this.GetTable<UserModel>();        // users[file:139]
        public ITable<ActivityModel> Activities => this.GetTable<ActivityModel>();    // activities[file:139]
        public ITable<MealModel> Meals => this.GetTable<MealModel>();        // meals[file:139]
        public ITable<DailyGoalModel> DailyGoals => this.GetTable<DailyGoalModel>();   // daily_goals[file:139]
        public ITable<BmiRecordModel> BmiRecords => this.GetTable<BmiRecordModel>();   // bmi_records[file:139]
        public ITable<ErrorLogModel> ErrorLogs => this.GetTable<ErrorLogModel>();    // error_logs[file:139]
        public ITable<ChangeLogModel> ChangeLogs => this.GetTable<ChangeLogModel>();   // change_logs[file:139]
        public ITable<ContentItemModel> ContentItems => this.GetTable<ContentItemModel>(); // content_items[file:139]
        public ITable<NotificationModel> Notifications => this.GetTable<NotificationModel>();
    }
}
