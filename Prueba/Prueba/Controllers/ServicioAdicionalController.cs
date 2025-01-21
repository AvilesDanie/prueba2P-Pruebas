using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Prueba.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicioAdicionalController : ControllerBase
    {
        private readonly AppDBContext _appDbContext;

        public ServicioAdicionalController(AppDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // Método reutilizable para incluir relaciones comunes
        private IQueryable<ServicioAdicional> GetServiciosAdicionalesWithIncludes()
        {
            return _appDbContext.ServiciosAdicionales
                .Include(s => s.Reserva)
                .ThenInclude(r => r.Cliente)
                .Include(s => s.Reserva.Habitacion);
        }

        // Método reutilizable para validar un servicio
        private async Task<(bool isValid, string message)> ValidateServicioAdicional(ServicioAdicional servicio)
        {
            if (string.IsNullOrWhiteSpace(servicio.Descripcion))
                return (false, "La descripción del servicio es obligatoria");

            var descripcionExistente = await _appDbContext.ServiciosAdicionales
                .AnyAsync(s => s.Descripcion == servicio.Descripcion);

            if (descripcionExistente)
                return (false, "Ya existe un servicio adicional con la misma descripción");

            if (servicio.Costo <= 0 || servicio.Costo > 200)
                return (false, "El costo debe ser entre 0 y 200");

            var reserva = await _appDbContext.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Habitacion)
                .FirstOrDefaultAsync(r => r.Id == servicio.Reserva.Id);

            if (reserva == null)
                return (false, "La reserva asociada no existe");

            servicio.Reserva = reserva;
            return (true, string.Empty);
        }

        [HttpGet]
        public async Task<IActionResult> GetServiciosAdicionales()
        {
            var servicios = await GetServiciosAdicionalesWithIncludes().ToListAsync();
            return Ok(servicios);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetServicioAdicional(int id)
        {
            var servicio = await GetServiciosAdicionalesWithIncludes()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null) return Ok("El servicio no existe");
            return Ok(servicio);
        }

        [HttpPost]
        public async Task<IActionResult> CreateServicioAdicional(ServicioAdicional servicio)
        {
            var (isValid, message) = await ValidateServicioAdicional(servicio);
            if (!isValid) return Ok(message);

            await _appDbContext.ServiciosAdicionales.AddAsync(servicio);
            await _appDbContext.SaveChangesAsync();

            var servicioCreado = await GetServiciosAdicionalesWithIncludes()
                .FirstOrDefaultAsync(s => s.Id == servicio.Id);

            return Ok(servicioCreado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditServicioAdicional(int id, ServicioAdicional servicio)
        {
            if (id != servicio.Id) return Ok("El ID del servicio adicional no coincide");

            var servicioExistente = await GetServiciosAdicionalesWithIncludes()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicioExistente == null) return Ok("El servicio adicional no existe");

            var (isValid, message) = await ValidateServicioAdicional(servicio);
            if (!isValid) return Ok(message);

            servicioExistente.Descripcion = servicio.Descripcion;
            servicioExistente.Costo = servicio.Costo;
            servicioExistente.Reserva = servicio.Reserva;

            _appDbContext.ServiciosAdicionales.Update(servicioExistente);
            await _appDbContext.SaveChangesAsync();

            var servicioActualizado = await GetServiciosAdicionalesWithIncludes()
                .FirstOrDefaultAsync(s => s.Id == id);

            return Ok(servicioActualizado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServicioAdicional(int id)
        {
            var servicio = await _appDbContext.ServiciosAdicionales.FindAsync(id);

            if (servicio == null) return Ok("El servicio adicional no existe");

            _appDbContext.ServiciosAdicionales.Remove(servicio);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { message = $"Servicio adicional con ID {id} eliminado correctamente" });
        }
    }
}
