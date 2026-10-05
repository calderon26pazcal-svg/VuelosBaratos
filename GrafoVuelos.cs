using System.Globalization;

namespace VuelosBaratos;

/// <summary>Un vuelo directo: arista dirigida y ponderada (peso = precio).</summary>
public record Vuelo(string Origen, string Destino, decimal Precio, string Aerolinea);

/// <summary>Resultado de la búsqueda: costo total y vuelos en orden.</summary>
public record ResultadoRuta(decimal CostoTotal, List<Vuelo> Vuelos);

/// <summary>
/// Grafo dirigido y ponderado representado con lista de adyacencia.
/// Nodos = ciudades, aristas = vuelos directos, peso = precio.
/// </summary>
public class GrafoVuelos
{
    private readonly Dictionary<string, List<Vuelo>> _adyacencia =
        new(StringComparer.OrdinalIgnoreCase);

    public int TotalCiudades => _adyacencia.Count;
    public int TotalVuelos { get; private set; }

    public IEnumerable<string> Ciudades =>
        _adyacencia.Keys.OrderBy(c => c, StringComparer.OrdinalIgnoreCase);

    public bool ExisteCiudad(string ciudad) => _adyacencia.ContainsKey(ciudad);

    /// <summary>Agrega un vuelo (arista). Crea las ciudades (nodos) si no existen. O(1).</summary>
    public void AgregarVuelo(string origen, string destino, decimal precio, string aerolinea)
    {
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo (Dijkstra lo exige).");

        if (!_adyacencia.ContainsKey(origen)) _adyacencia[origen] = new List<Vuelo>();
        if (!_adyacencia.ContainsKey(destino)) _adyacencia[destino] = new List<Vuelo>();

        _adyacencia[origen].Add(new Vuelo(origen, destino, precio, aerolinea));
        TotalVuelos++;
    }

    /// <summary>Carga la base de datos desde un archivo de texto. Devuelve las líneas inválidas.</summary>
    public int CargarDesdeArchivo(string ruta)
    {
        int invalidas = 0;
        int numero = 0;

        foreach (var linea in File.ReadLines(ruta))
        {
            numero++;
            var texto = linea.Trim();
            if (texto.Length == 0 || texto.StartsWith('#')) continue;

            var partes = texto.Split(';');
            if (partes.Length != 4 ||
                !decimal.TryParse(partes[2].Trim(), NumberStyles.Number,
                                  CultureInfo.InvariantCulture, out var precio) ||
                precio < 0)
            {
                Console.WriteLine($"  Línea {numero} ignorada (formato inválido): {texto}");
                invalidas++;
                continue;
            }

            AgregarVuelo(partes[0].Trim(), partes[1].Trim(), precio, partes[3].Trim());
        }

        return invalidas;
    }

    /// <summary>Vuelos directos que salen de una ciudad. O(1) para ubicar la lista.</summary>
    public IReadOnlyList<Vuelo> VuelosDesde(string ciudad) =>
        _adyacencia.TryGetValue(ciudad, out var lista) ? lista : new List<Vuelo>();

    /// <summary>
    /// Ruta de menor costo entre dos ciudades (algoritmo de Dijkstra con cola de prioridad).
    /// Complejidad: O((V + E) log V). Devuelve null si no existe ruta.
    /// </summary>
    public ResultadoRuta? RutaMasBarata(string origen, string destino)
    {
        if (!_adyacencia.ContainsKey(origen) || !_adyacencia.ContainsKey(destino))
            return null;

        var costo = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var previo = new Dictionary<string, Vuelo>(StringComparer.OrdinalIgnoreCase);
        var cola = new PriorityQueue<string, decimal>();

        costo[origen] = 0m;
        cola.Enqueue(origen, 0m);

        while (cola.TryDequeue(out var actual, out var costoActual))
        {
            // Entrada desactualizada: ya se encontró un camino más barato.
            if (costoActual > costo[actual]) continue;

            // El destino ya quedó fijado con su costo mínimo.
            if (string.Equals(actual, destino, StringComparison.OrdinalIgnoreCase)) break;

            foreach (var vuelo in _adyacencia[actual])
            {
                decimal nuevoCosto = costoActual + vuelo.Precio;
                if (!costo.TryGetValue(vuelo.Destino, out var costoConocido) ||
                    nuevoCosto < costoConocido)
                {
                    costo[vuelo.Destino] = nuevoCosto;
                    previo[vuelo.Destino] = vuelo;
                    cola.Enqueue(vuelo.Destino, nuevoCosto);
                }
            }
        }

        if (!costo.ContainsKey(destino)) return null;

        // Reconstrucción de la ruta recorriendo los vuelos previos hacia atrás.
        var vuelos = new List<Vuelo>();
        var ciudad = destino;
        while (previo.TryGetValue(ciudad, out var vueloPrevio))
        {
            vuelos.Insert(0, vueloPrevio);
            ciudad = vueloPrevio.Origen;
        }

        return new ResultadoRuta(costo[destino], vuelos);
    }

    /// <summary>Genera un grafo sintético (conectado) para pruebas de rendimiento.</summary>
    public static GrafoVuelos Sintetico(int ciudades, int vuelosPorCiudad, int semilla = 42)
    {
        var grafo = new GrafoVuelos();
        var azar = new Random(semilla);

        for (int i = 0; i < ciudades - 1; i++)
            grafo.AgregarVuelo($"C{i}", $"C{i + 1}", azar.Next(20, 500), "Sintetica");

        for (int i = 0; i < ciudades; i++)
        {
            for (int k = 0; k < vuelosPorCiudad; k++)
            {
                int j = azar.Next(ciudades);
                if (j != i)
                    grafo.AgregarVuelo($"C{i}", $"C{j}", azar.Next(20, 500), "Sintetica");
            }
        }

        return grafo;
    }
}
