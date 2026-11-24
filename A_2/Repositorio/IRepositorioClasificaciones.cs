namespace A_2.Repositorio;

using A_2.Models;

// Tema 10 - Diapositiva 6: Interfaz para el repositorio
public interface IRepositorioClasificaciones
{
    Task<List<Clasificacion>> GetAll();
    Task<Dictionary<int, int>> ContarHerramientasPorClasificacion();
    Task<Clasificacion?> Get(int id);
    Task<Clasificacion> Add(Clasificacion entity);
    Task Update(Clasificacion entity);
    Task Delete(int id);
}
