using Query.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Models
{
    public class ConnectionEntry
    {
        [Required]
        public string? ConnectionString { get; set; }

        public DbEngine? Engine { get; set; }
    }
}
