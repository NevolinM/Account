using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    [Table("student")]
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Surname { get; set; }

        [Required]
        public string Name { get; set; }

        public string Patronymic { get; set; }

        [Column("GroupsID")]
        public int GroupsID { get; set; }
    }
}
