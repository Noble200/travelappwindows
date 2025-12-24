using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Allva.Desktop.Models;
using Allva.Desktop.Views.Admin.MenuHamburguesa;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio que gestiona los paneles disponibles en el menu hamburguesa
    /// Centraliza el registro de todos los modulos de configuracion
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
        /// IMPORTANTE: Agregar aqui cada nuevo panel que se cree
        /// </summary>
        private void RegistrarPanelesDisponibles()
        {
            // Panel de Usuarios Allva (Administradores)
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "usuarios_allva",
                Titulo = "Usuarios Allva",
                Descripcion = "Gestion de administradores Allva",
                Orden = 1,
                Habilitado = true,
                TipoVista = typeof(UsuariosAllvaView)
            });

            // Panel de Edicion FrontOffice (antes Packs de Alimentos)
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "edicion_frontoffice",
                Titulo = "Edicion FrontOffice",
                Descripcion = "Edicion de contenido del FrontOffice",
                Orden = 5,
                Habilitado = true,
                TipoVista = typeof(EdicionFrontOfficeView)
            });

            // Panel de APIs
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "apis",
                Titulo = "APIs",
                Descripcion = "Configuracion de APIs externas",
                Orden = 10,
                Habilitado = true,
                TipoVista = typeof(APIsConfigView)
            });

            // Panel de Configuracion General
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "configuracion",
                Titulo = "Configuracion",
                Descripcion = "Configuracion general del sistema",
                Orden = 20,
                Habilitado = true,
                TipoVista = typeof(ConfiguracionGeneralView)
            });
        }

        public void RegistrarItem(MenuHamburguesaItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("El Id del item no puede estar vacio");

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
            return item?.Titulo?.ToUpper() ?? "CONFIGURACION";
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