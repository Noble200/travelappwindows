using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Allva.Desktop.Models;
using Allva.Desktop.Views.Admin.MenuHamburguesa;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio que gestiona los paneles disponibles en el menú hamburguesa
    /// Centraliza el registro de todos los módulos de configuración
    /// </summary>
    public class MenuHamburguesaService
    {
        private static MenuHamburguesaService? _instance;
        private readonly List<MenuHamburguesaItem> _items;

        public static MenuHamburguesaService Instance => _instance ??= new MenuHamburguesaService();

        private MenuHamburguesaService()
        {
            _items = new List<MenuHamburguesaItem>();
            RegistrarPanelesDisponibles();
        }

        /// <summary>
        /// Registra todos los paneles disponibles en la carpeta MenuHamburguesa
        /// IMPORTANTE: Agregar aquí cada nuevo panel que se cree
        /// </summary>
        private void RegistrarPanelesDisponibles()
        {
            // Panel de APIs
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "apis",
                Titulo = "APIs",
                Descripcion = "Configuración de APIs externas",
                Orden = 10,
                Habilitado = true,
                TipoVista = typeof(APIsConfigView)
            });

            // Panel de Configuración General (ejemplo para futuro)
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "configuracion",
                Titulo = "Configuración",
                Descripcion = "Configuración general del sistema",
                Orden = 20,
                Habilitado = true,
                TipoVista = typeof(ConfiguracionGeneralView)
            });
        }

        public void RegistrarItem(MenuHamburguesaItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("El Id del item no puede estar vacío");

            if (_items.Any(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
            {
                var existente = _items.First(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
                var index = _items.IndexOf(existente);
                _items[index] = item;
            }
            else
            {
                _items.Add(item);
            }
        }

        public List<MenuHamburguesaItem> ObtenerItemsHabilitados()
        {
            return _items
                .Where(x => x.Habilitado)
                .OrderBy(x => x.Orden)
                .ToList();
        }

        public List<MenuHamburguesaItem> ObtenerTodosLosItems()
        {
            return _items.OrderBy(x => x.Orden).ToList();
        }

        public MenuHamburguesaItem? ObtenerItemPorId(string id)
        {
            return _items.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public UserControl? CrearVistaParaItem(string id)
        {
            var item = ObtenerItemPorId(id);
            
            if (item?.TipoVista == null)
                return null;

            try
            {
                return Activator.CreateInstance(item.TipoVista) as UserControl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear vista para '{id}': {ex.Message}");
                return null;
            }
        }

        public UserControl? CrearVistaParaItem(MenuHamburguesaItem item)
        {
            if (item?.TipoVista == null)
                return null;

            try
            {
                return Activator.CreateInstance(item.TipoVista) as UserControl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear vista para '{item.Id}': {ex.Message}");
                return null;
            }
        }

        public string ObtenerTituloModulo(string id)
        {
            var item = ObtenerItemPorId(id);
            return item?.Titulo?.ToUpper() ?? "CONFIGURACIÓN";
        }

        public bool EsModuloMenuHamburguesa(string id)
        {
            return _items.Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public void SetHabilitado(string id, bool habilitado)
        {
            var item = ObtenerItemPorId(id);
            if (item != null)
            {
                item.Habilitado = habilitado;
            }
        }
    }
}