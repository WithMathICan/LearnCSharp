using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFPractice01.Models {
    public class Operation {
        [Key]
        public int Id { get; set; }

        public Guid OperationId { get; set; }
    }
}
