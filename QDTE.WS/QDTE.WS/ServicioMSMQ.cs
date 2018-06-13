using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Unity;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WSPlantilla
{
    public partial class ServicioMSMQ : ServiceBase
    {
        Configurador configurador;

        public ServicioMSMQ()
        {
            InitializeComponent();
            configurador = Configurador.Instance;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Creacion de la cola.
                if (!MessageQueue.Exists(configurador.InitialQueueName))
                {
                    MessageQueue.Create(configurador.InitialQueueName, true);
                }

                int cantidad = configurador.ThreadsQuantity;

                Producer producer = new Producer(configurador);
                Consumer consumer = new Consumer(configurador);

                for (int i = 0; i < cantidad; i++)
                {
                    Thread t = new Thread(new ThreadStart(consumer.MessageReceived))
                    {
                        IsBackground = true
                    };
                    t.Start();
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Creando el hilo {0} ", t.ManagedThreadId), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Inicializando" });
                }

                producer.BeginSendingDocuments();
            }
            catch (Exception ex)
            {
                configurador.ExceptionHandler.HandleException(ex, Util.POLITICA_ERRORES, out Exception nExption);
            }

        }

        protected override void OnStop()
        {
        }
    }
}
