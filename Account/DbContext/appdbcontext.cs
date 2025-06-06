using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.ComponentModel.DataAnnotations;
using Account.Models;

namespace Account.Data
{
    public class appdbcontext : DbContext
    {
        public appdbcontext(DbContextOptions<appdbcontext> options) : base(options)
        {
        }

        public DbSet<Disciplin> Disciplin { get; set; }
        public DbSet<Teacher> Teacher { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Student_Groups> StudentGroups { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<DisciplinMaterial> DisciplinMaterials { get; set; }
        public DbSet<DisciplinComment> DisciplinComments { get; set; }
        public DbSet<TeacherDisciplin> TeacherDisciplins { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }
        public DbSet<TaskGrade> TaskGrades { get; set; }
        public DbSet<CourseJournal> CourseJournals { get; set; }
        public DbSet<CourseJournalEntry> CourseJournalEntries { get; set; }
    }
}
