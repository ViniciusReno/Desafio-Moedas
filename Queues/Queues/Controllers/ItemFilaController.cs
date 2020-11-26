using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Queues.Data;
using Queues.Models;
using System.Threading.Tasks;

namespace Queues.Controllers
{
    [ApiController]
    [Route("v1/fila")]
    public class ItemFilaController : ControllerBase
    {
        [HttpGet]
        [Route("")]
        public async Task<ActionResult<ItemFila>> Get([FromServices] DataContext context)
        {
            var item = await context.ItemFilas.LastOrDefaultAsync();
            if (item != null)
            {
                context.ItemFilas.Remove(item);
                await context.SaveChangesAsync();
                return item;
            }

            return new ItemFila { Id = -1 };
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<ItemFila>> Post([FromServices] DataContext context, [FromBody] ItemFila model)
        {
            if (ModelState.IsValid)
            {
                context.ItemFilas.Add(model);
                await context.SaveChangesAsync();
                return model;
            }

            return BadRequest(ModelState);
        }
    }
}
