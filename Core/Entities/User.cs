using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using ColumnAttribute = LinqToDB.Mapping.ColumnAttribute;
using NotNullAttribute = LinqToDB.Mapping.NotNullAttribute;
using TableAttribute = System.ComponentModel.DataAnnotations.Schema.TableAttribute;


namespace FitnessBot.Core.Entities
{
    [Table("users")]
    public class User
    {
        public long Id { get; set; }
        public long TelegramId { get; set; }
        public string Name { get; set; } = null!;
        public UserRole Role { get; set; } = UserRole.User;
        public int? Age { get; set; }
        public string? City { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }
}
