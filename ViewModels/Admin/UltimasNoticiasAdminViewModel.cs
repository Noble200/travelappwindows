using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Models;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class UltimasNoticiasAdminViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        [ObservableProperty]
        private ObservableCollection<NoticiaItem> _noticias = new();

        [ObservableProperty]
        private NoticiaItem? _noticiaSeleccionada;

        [ObservableProperty]
        private bool _estaCargando;

        [ObservableProperty]
        private string _mensajeEstado = string.Empty;

        [ObservableProperty]
        private bool _hayMensaje;

        [ObservableProperty]
        private bool _mostrarPanelEdicion;

        [ObservableProperty]
        private bool _modoEdicion;

        [ObservableProperty]
        private string _tituloPanel = "Nueva Noticia";

        [ObservableProperty]
        private string _textoBotonGuardar = "Crear Noticia";

        // Campos del formulario
        [ObservableProperty]
        private string _tituloNoticia = string.Empty;

        [ObservableProperty]
        private string _contenidoNoticia = string.Empty;

        [ObservableProperty]
        private string _estadoSeleccionado = "Activa";

        [ObservableProperty]
        private string _ordenNoticia = "1";

        [ObservableProperty]
        private ObservableCollection<string> _estadosDisponibles = new() { "Activa", "Inactiva", "Borrador" };

        private int? _noticiaEditandoId;

        public IBrush MensajeBackground => HayMensaje
            ? (MensajeEsError ? Brushes.Red : new SolidColorBrush(Color.Parse("#28a745")))
            : Brushes.Transparent;

        private bool _mensajeEsError;
        public bool MensajeEsError
        {
            get => _mensajeEsError;
            set => SetProperty(ref _mensajeEsError, value);
        }

        public UltimasNoticiasAdminViewModel()
        {
            _ = CargarNoticiasAsync();
        }

        private async Task CargarNoticiasAsync()
        {
            EstaCargando = true;
            Noticias.Clear();

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT id, titulo, contenido, estado, orden, fecha_creacion
                    FROM noticias
                    ORDER BY orden ASC, fecha_creacion DESC", connection);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Noticias.Add(new NoticiaItem
                    {
                        Id = reader.GetInt32(0),
                        Titulo = reader.GetString(1),
                        Contenido = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Estado = reader.IsDBNull(3) ? "Activa" : reader.GetString(3),
                        Orden = reader.IsDBNull(4) ? 1 : reader.GetInt32(4),
                        FechaCreacion = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5)
                    });
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar noticias: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        [RelayCommand]
        private void NuevaNoticia()
        {
            ModoEdicion = false;
            TituloPanel = "Nueva Noticia";
            TextoBotonGuardar = "Crear Noticia";
            LimpiarFormulario();
            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private void EditarNoticia(NoticiaItem noticia)
        {
            if (noticia == null) return;

            ModoEdicion = true;
            TituloPanel = "Editar Noticia";
            TextoBotonGuardar = "Guardar Cambios";
            _noticiaEditandoId = noticia.Id;

            TituloNoticia = noticia.Titulo;
            ContenidoNoticia = noticia.Contenido;
            EstadoSeleccionado = noticia.Estado;
            OrdenNoticia = noticia.Orden.ToString();

            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private async Task EliminarNoticia(NoticiaItem noticia)
        {
            if (noticia == null) return;

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand("DELETE FROM noticias WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("@id", noticia.Id);
                await cmd.ExecuteNonQueryAsync();

                Noticias.Remove(noticia);
                MostrarMensaje("Noticia eliminada correctamente", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al eliminar: {ex.Message}", true);
            }
        }

        [RelayCommand]
        private void CancelarEdicion()
        {
            MostrarPanelEdicion = false;
            LimpiarFormulario();
        }

        [RelayCommand]
        private async Task GuardarNoticia()
        {
            if (string.IsNullOrWhiteSpace(TituloNoticia))
            {
                MostrarMensaje("El titulo es obligatorio", true);
                return;
            }

            EstaCargando = true;

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                if (!int.TryParse(OrdenNoticia, out int orden))
                    orden = 1;

                if (ModoEdicion && _noticiaEditandoId.HasValue)
                {
                    var cmd = new NpgsqlCommand(@"
                        UPDATE noticias
                        SET titulo = @titulo, contenido = @contenido, estado = @estado, orden = @orden
                        WHERE id = @id", connection);

                    cmd.Parameters.AddWithValue("@titulo", TituloNoticia);
                    cmd.Parameters.AddWithValue("@contenido", ContenidoNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@estado", EstadoSeleccionado);
                    cmd.Parameters.AddWithValue("@orden", orden);
                    cmd.Parameters.AddWithValue("@id", _noticiaEditandoId.Value);

                    await cmd.ExecuteNonQueryAsync();
                    MostrarMensaje("Noticia actualizada correctamente", false);
                }
                else
                {
                    var cmd = new NpgsqlCommand(@"
                        INSERT INTO noticias (titulo, contenido, estado, orden, fecha_creacion)
                        VALUES (@titulo, @contenido, @estado, @orden, @fecha)", connection);

                    cmd.Parameters.AddWithValue("@titulo", TituloNoticia);
                    cmd.Parameters.AddWithValue("@contenido", ContenidoNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@estado", EstadoSeleccionado);
                    cmd.Parameters.AddWithValue("@orden", orden);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

                    await cmd.ExecuteNonQueryAsync();
                    MostrarMensaje("Noticia creada correctamente", false);
                }

                MostrarPanelEdicion = false;
                LimpiarFormulario();
                await CargarNoticiasAsync();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al guardar: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void LimpiarFormulario()
        {
            TituloNoticia = string.Empty;
            ContenidoNoticia = string.Empty;
            EstadoSeleccionado = "Activa";
            OrdenNoticia = "1";
            _noticiaEditandoId = null;
        }

        private async void MostrarMensaje(string mensaje, bool esError)
        {
            MensajeEstado = mensaje;
            MensajeEsError = esError;
            HayMensaje = true;
            OnPropertyChanged(nameof(MensajeBackground));

            await Task.Delay(4000);
            HayMensaje = false;
        }
    }

    public class NoticiaItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string Estado { get; set; } = "Activa";
        public int Orden { get; set; } = 1;
        public DateTime FechaCreacion { get; set; }

        public string FechaCreacionTexto => FechaCreacion.ToString("dd/MM/yyyy HH:mm");

        public string EstadoTexto => Estado;

        public IBrush EstadoBackground => Estado switch
        {
            "Activa" => new SolidColorBrush(Color.Parse("#D4EDDA")),
            "Inactiva" => new SolidColorBrush(Color.Parse("#F8D7DA")),
            "Borrador" => new SolidColorBrush(Color.Parse("#FFF3CD")),
            _ => new SolidColorBrush(Color.Parse("#E2E3E5"))
        };

        public IBrush EstadoForeground => Estado switch
        {
            "Activa" => new SolidColorBrush(Color.Parse("#155724")),
            "Inactiva" => new SolidColorBrush(Color.Parse("#721C24")),
            "Borrador" => new SolidColorBrush(Color.Parse("#856404")),
            _ => new SolidColorBrush(Color.Parse("#383D41"))
        };
    }
}
