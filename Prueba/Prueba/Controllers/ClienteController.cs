using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Prueba.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        private readonly AppDBContext _appDbContext;
        public ClienteController(AppDBContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            var clientes = await _appDbContext.Clientes.ToListAsync();
            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCliente(int id)
        {
            var cliente = await _appDbContext.Clientes.FindAsync(id);
            if (cliente == null) return Ok("El cliente no existe");
            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCliente(Cliente cliente)
        {
            var validationResult = await ValidarCliente(cliente);
            if (!string.IsNullOrEmpty(validationResult))
            {
                return Ok(validationResult);
            }

            _appDbContext.Clientes.Add(cliente);
            await _appDbContext.SaveChangesAsync();
            return Ok(cliente);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditCliente(int id, Cliente cliente)
        {
            if (id != cliente.Id) return Ok("El ID del cliente no coincide");

            var clienteExistente = await _appDbContext.Clientes.FindAsync(id);
            if (clienteExistente == null) return Ok("El cliente no existe");

            var validationResult = await ValidarCliente(cliente, id);
            if (!string.IsNullOrEmpty(validationResult))
            {
                return Ok(validationResult);
            }

            clienteExistente.Nombre = cliente.Nombre;
            clienteExistente.Edad = cliente.Edad;
            clienteExistente.Cedula = cliente.Cedula;

            _appDbContext.Clientes.Update(clienteExistente);
            await _appDbContext.SaveChangesAsync();
            return Ok(clienteExistente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _appDbContext.Clientes.FindAsync(id);
            if (cliente == null)
                return Ok("El cliente no existe");

            var tieneReservas = await _appDbContext.Reservas.AnyAsync(r => r.Cliente.Id == id);
            if (tieneReservas)
                return Ok("El cliente no se puede eliminar porque está asociado a una o más reservas");

            _appDbContext.Clientes.Remove(cliente);
            await _appDbContext.SaveChangesAsync();
            return Ok(new { message = $"Cliente con ID {id} ha sido eliminado correctamente" });
        }


        private static bool EsCedulaValida(string cedula)
        {
            if (cedula.Length != 10 || !cedula.All(char.IsDigit))
            {
                return false;
            }

            int provincia = int.Parse(cedula.Substring(0, 2));
            if ((provincia < 1 || provincia > 24) && provincia != 30)
            {
                return false;
            }

            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;
            for (int i = 0; i < coeficientes.Length; i++)
            {
                int valor = coeficientes[i] * int.Parse(cedula[i].ToString());
                suma += valor >= 10 ? valor - 9 : valor;
            }

            int digitoVerificador = int.Parse(cedula[9].ToString());
            int residuo = suma % 10;
            int resultado = residuo == 0 ? 0 : 10 - residuo;
            return resultado == digitoVerificador;
        }

        private async Task<string?> ValidarCliente(Cliente cliente, int? id = null)
        {
            if (string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                return "El nombre es obligatorio";
            }
            if (cliente.Nombre.Any(char.IsDigit))
            {
                return "El nombre no debe contener números";
            }

            if (string.IsNullOrWhiteSpace(cliente.Apellido))
            {
                return "El apellido es obligatorio";
            }
            if (cliente.Apellido.Any(char.IsDigit))
            {
                return "El apellido no debe contener números";
            }

            if (string.IsNullOrWhiteSpace(cliente.Cedula))
            {
                return "La cédula es obligatoria";
            }
            if (!EsCedulaValida(cliente.Cedula))
            {
                return "La cédula ingresada no es válida";
            }

            if (cliente.Edad < 18 || cliente.Edad > 70)
            {
                return "La edad debe ser mayor o igual a 18 años y menor o igual a 70";
            }

            var clienteExistente = await _appDbContext.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == cliente.Cedula && (!id.HasValue || c.Id != id.Value));
            if (clienteExistente != null)
            {
                return "Ya existe un cliente con la misma cédula";
            }

            return null;
        }
    }
}
