using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Models;
public class Favourite
{
    private Favourite(Guid employeeId, Employee employee, Guid employerId, Employer employer)
    {
        EmployeeId = employeeId;
        Employee = employee;
        EmployerId = employerId;
        Employer = employer;
    }
    public Favourite() : base() { }

    public Guid EmployeeId { get; }
    public Employee Employee { get; }
    public Guid EmployerId { get; }
    public Employer Employer { get; }

    public static Result<Favourite> Create(Employee employee, Employer employer)
    {
        return new Favourite(employee.Id, employee, employer.Id, employer);
    }
}
