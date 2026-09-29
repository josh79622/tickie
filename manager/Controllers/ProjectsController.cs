using Tickie.Manager.Data;
using Microsoft.AspNetCore.Mvc;

namespace Tickie.Manager.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly TickieDbContext _dbContext;
    public ProjectsController(TickieDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
