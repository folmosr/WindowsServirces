using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace WSPlantilla
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            // Obtener el nombre del servicio desde el archivo de configuracion.
            Configuration configuracion = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string nombreServicio = configuracion.AppSettings.Settings["NombreServicio"].Value;

            if (string.IsNullOrWhiteSpace(nombreServicio))
            {
                // Obtiene el nombre del assembly.
                nombreServicio = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            }

            if (string.IsNullOrWhiteSpace(nombreServicio))
            {
                // Coloca valor por defecto si no hay definicion.
                nombreServicio = "EnvioCorreoCliente.WS";
            }

            // Bautizar el servicio.
            this.serviceInstaller1.Description = nombreServicio;
            this.serviceInstaller1.DisplayName = nombreServicio;
            this.serviceInstaller1.ServiceName = nombreServicio;
        }
    }
}
