using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace VuelosBaratos;

public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var grafo = new GrafoVuelos();
        string archivo = Path.Combine(AppContext.BaseDirectory, "vuelos.txt");

        if (!File.Exists(archivo))
        {
            Console.WriteLine($"No se encontró el archivo de vuelos: {archivo}");
            return;
        }

        grafo.CargarDesdeArchivo(archivo);
        Console.WriteLine("=== BUSCADOR DE VUELOS BARATOS (grafo ponderado + Dijkstra) ===");
        Console.WriteLine($"Base cargada: {grafo.TotalCiudades} ciudades y {grafo.TotalVuelos} vuelos.");

        bool salir = false;
        while (!salir)
        {
            Console.WriteLine();
            Console.WriteLine("1. Listar ciudades (nodos)");
            Console.WriteLine("2. Listar todos los vuelos (lista de adyacencia)");
            Console.WriteLine("3. Consultar vuelos directos desde una ciudad");
            Console.WriteLine("4. Buscar la ruta más barata");
            Console.WriteLine("5. Medir tiempo de ejecución (base cargada)");
            Console.WriteLine("6. Prueba de rendimiento con grafos sintéticos");
            Console.WriteLine("7. Salir");
            Console.Write("Opción: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1": ListarCiudades(grafo); break;
                case "2": ListarVuelos(grafo); break;
                case "3": ConsultarDesde(grafo); break;
                case "4": BuscarRuta(grafo); break;
                case "5": MedirBaseCargada(grafo); break;
                case "6": PruebaRendimiento(); break;
                case "7": salir = true; break;
                default: Console.WriteLine("Opción no válida."); break;
            }
        }
    }

    static string Dinero(decimal valor) => "$" + valor.ToString("F2", CultureInfo.InvariantCulture);

    static string? LeerCiudad(GrafoVuelos grafo, string mensaje)
    {
        Console.Write(mensaje);
        var texto = Console.ReadLine()?.Trim() ?? "";
        if (!grafo.ExisteCiudad(texto))
        {
            Console.WriteLine($"La ciudad \"{texto}\" no existe en la base de datos.");
            return null;
        }
        return texto;
    }

    static void ListarCiudades(GrafoVuelos grafo)
    {
        Console.WriteLine($"\nCiudades registradas ({grafo.TotalCiudades}):");
        int n = 1;
        foreach (var ciudad in grafo.Ciudades)
            Console.WriteLine($"  {n++}. {ciudad} (salen {grafo.VuelosDesde(ciudad).Count} vuelos)");
    }

    static void ListarVuelos(GrafoVuelos grafo)
    {
        Console.WriteLine($"\nVuelos registrados ({grafo.TotalVuelos}):");
        foreach (var ciudad in grafo.Ciudades)
        {
            Console.WriteLine($"{ciudad}:");
            var vuelos = grafo.VuelosDesde(ciudad);
            if (vuelos.Count == 0) Console.WriteLine("  (sin vuelos de salida)");
            foreach (var v in vuelos)
                Console.WriteLine($"  -> {v.Destino,-12} {Dinero(v.Precio),8}  {v.Aerolinea}");
        }
    }

    static void ConsultarDesde(GrafoVuelos grafo)
    {
        var ciudad = LeerCiudad(grafo, "Ciudad de origen: ");
        if (ciudad == null) return;

        var vuelos = grafo.VuelosDesde(ciudad).OrderBy(v => v.Precio).ToList();
        Console.WriteLine($"\nVuelos directos desde {ciudad} (del más barato al más caro):");
        if (vuelos.Count == 0) Console.WriteLine("  No hay vuelos de salida.");
        foreach (var v in vuelos)
            Console.WriteLine($"  -> {v.Destino,-12} {Dinero(v.Precio),8}  {v.Aerolinea}");
    }

    static void BuscarRuta(GrafoVuelos grafo)
    {
        var origen = LeerCiudad(grafo, "Ciudad de origen: ");
        if (origen == null) return;
        var destino = LeerCiudad(grafo, "Ciudad de destino: ");
        if (destino == null) return;

        var resultado = grafo.RutaMasBarata(origen, destino);
        if (resultado == null)
        {
            Console.WriteLine($"\nNo existe ninguna ruta de {origen} a {destino}.");
            return;
        }

        Console.WriteLine($"\nRuta más barata de {origen} a {destino}:");
        if (resultado.Vuelos.Count == 0)
        {
            Console.WriteLine("  El origen y el destino son la misma ciudad.");
        }
        else
        {
            int paso = 1;
            foreach (var v in resultado.Vuelos)
                Console.WriteLine($"  {paso++}. {v.Origen} -> {v.Destino}  {Dinero(v.Precio)}  ({v.Aerolinea})");
        }
        Console.WriteLine($"Costo total: {Dinero(resultado.CostoTotal)} | Escalas: {Math.Max(0, resultado.Vuelos.Count - 1)}");
    }

    static double MedirPromedioMs(Action accion, int repeticiones)
    {
        accion(); // calentamiento (JIT)
        var reloj = Stopwatch.StartNew();
        for (int i = 0; i < repeticiones; i++) accion();
        reloj.Stop();
        return reloj.Elapsed.TotalMilliseconds / repeticiones;
    }

    static void MedirBaseCargada(GrafoVuelos grafo)
    {
        var origen = LeerCiudad(grafo, "Ciudad de origen: ");
        if (origen == null) return;
        var destino = LeerCiudad(grafo, "Ciudad de destino: ");
        if (destino == null) return;

        const int repeticiones = 10000;
        double ms = MedirPromedioMs(() => grafo.RutaMasBarata(origen, destino), repeticiones);
        Console.WriteLine($"\nTiempo promedio de Dijkstra ({repeticiones} repeticiones): {ms:F5} ms");
    }

    static void PruebaRendimiento()
    {
        Console.WriteLine("\nPrueba con grafos sintéticos (5 vuelos de salida por ciudad):");
        Console.WriteLine($"{"Ciudades",10} {"Vuelos",10} {"Tiempo promedio (ms)",24}");

        foreach (int ciudades in new[] { 100, 1000, 10000, 50000 })
        {
            var g = GrafoVuelos.Sintetico(ciudades, 5);
            string origen = "C0";
            string destino = $"C{ciudades - 1}";
            int repeticiones = ciudades <= 1000 ? 100 : 10;

            double ms = MedirPromedioMs(() => g.RutaMasBarata(origen, destino), repeticiones);
            Console.WriteLine($"{g.TotalCiudades,10} {g.TotalVuelos,10} {ms,24:F3}");
        }
    }
}
