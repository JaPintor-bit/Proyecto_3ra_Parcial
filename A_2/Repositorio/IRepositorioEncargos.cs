namespace A_2.Repositorio;

using A_2.Models;

public interface IRepositorioEncargos
{
    Task<List<Encargo>> GetAll();
    Task<Encargo?> Get(int id);
    Task<Encargo> Add(Encargo entity, List<(int HerramientaId, int Cantidad)> herramientas);
    Task Update(Encargo entity, List<(int HerramientaId, int Cantidad)> herramientas);
    Task Delete(int id);
    Task ToggleComplete(int id);
}
