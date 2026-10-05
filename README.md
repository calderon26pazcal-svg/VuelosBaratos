# Buscador de vuelos baratos (grafo ponderado + Dijkstra)

Guía de Prácticas #04 - Estructura de Datos - Universidad Estatal Amazónica.

## Cómo ejecutar
Requiere .NET 8 SDK.

    dotnet run

El programa carga `vuelos.txt` (formato `Origen;Destino;Precio;Aerolinea`) y muestra un menú:
listar ciudades, listar vuelos, consultar vuelos desde una ciudad, ruta más barata,
medición de tiempo y prueba de rendimiento con grafos sintéticos.

## Estructura
- `GrafoVuelos.cs`: grafo dirigido y ponderado (lista de adyacencia) y Dijkstra.
- `Program.cs`: menú y reportería.
- `vuelos.txt`: base de datos ficticia (12 ciudades, 37 vuelos).
