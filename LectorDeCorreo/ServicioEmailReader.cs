using Microsoft.Practices.EnterpriseLibrary.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LectorDeCorreo
{
    public partial class ServicioEmailReader : ServiceBase
    {
        Logger logger;

        public ServicioEmailReader()
        {
            InitializeComponent();
            logger = new Logger();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                logger.LogWriter.Write(new LogEntry() { Message = String.Format("Conectándose al servidor/cuenta de correo"), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                Producer producer = new Producer(logger);
                producer.Start();
            }
            catch (Exception ex)
            {
                logger.ExceptionHandler.HandleException(ex, Logger.POLITICA_ERRORES, out Exception nExption);
            }
        }

        protected override void OnStop()
        {
        }
    }
}
