using System;
using System.ComponentModel.DataAnnotations;

namespace Queues.Models
{
    public class ItemFila
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Currency is mandatory")]
        public string Moeda { get; set; }

        [Required(ErrorMessage = "Start date is mandatory")]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "End date is mandatory")]
        public DateTime DataFim { get; set; }

    }
}
