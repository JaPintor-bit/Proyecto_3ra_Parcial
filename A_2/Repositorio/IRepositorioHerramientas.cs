namespace A_2.Repositorio;

using A_2.Models;

public interface IRepositorioHerramientas
{
    Task<List<Herramienta>> GetAll();
    Task<Herramienta?> Get(int id);
    Task<Herramienta> Add(Herramienta entity);
    Task Update(Herramienta entity);
    Task Delete(int id);
    Task ActualizarCantidad(int id, int nuevaCantidad);
}
