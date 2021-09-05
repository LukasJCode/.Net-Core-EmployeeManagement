using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.Models
{
	public class Employee
	{
		public int Id { get; set; }
		[Required]
		[MaxLength(50)]
		public string Name { get; set; }
		[NotMapped]
		public string EncryptedId { get; set; }
		[Required]
		[RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$",
			ErrorMessage = "Invalid Email Format")]
		[Display(Name ="Office Email")]
		public string Email { get; set; }
		[Required(ErrorMessage ="Please Select A Department")]
		public Dept? Department { get; set; }
		public string PhotoPath { get; set; }
	}
}
