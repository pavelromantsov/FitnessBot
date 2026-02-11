using FitnessBot.Core.DataAccess.Models;
using LinqToDB;
using LinqToDB.Data;

namespace FitnessBot.Infrastructure.DataAccess
{
    public class PgDataContext : DataConnection
    {
        public PgDataContext(string connectionString)
            : base(ProviderName.PostgreSQL, connectionString)
        {
        }

        public ITable<UserModel> Users => this.GetTable<UserModel>();       
        public ITable<ActivityModel> Activities => this.GetTable<ActivityModel>();    
        public ITable<MealModel> Meals => this.GetTable<MealModel>();      
        public ITable<DailyGoalModel> DailyGoals => this.GetTable<DailyGoalModel>();  
        public ITable<BmiRecordModel> BmiRecords => this.GetTable<BmiRecordModel>();  
        public ITable<ErrorLogModel> ErrorLogs => this.GetTable<ErrorLogModel>();   
        public ITable<ChangeLogModel> ChangeLogs => this.GetTable<ChangeLogModel>();  
        public ITable<ContentItemModel> ContentItems => this.GetTable<ContentItemModel>(); 
        public ITable<NotificationModel> Notifications => this.GetTable<NotificationModel>();

    }
}
