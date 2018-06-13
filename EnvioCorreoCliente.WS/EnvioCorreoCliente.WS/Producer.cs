using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Unity;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Transactions;
using System.Xml.Serialization;

namespace WSPlantilla
{
    public class Producer
    {

        SVCDocumentosElectronicos.DocumentosElectronicosClient client;
        MessageQueue queue;
        Configurador configurador;

        public Producer(Configurador parentConfigurador)
        {
            configurador = parentConfigurador;
            queue = new MessageQueue(configurador.InitialQueueName);
            client = new SVCDocumentosElectronicos.DocumentosElectronicosClient();
            configurador.LogWriter.Write(new LogEntry() { Message = "Instanciando Producer" });
        }

        public void BeginSendingDocuments()
        {
            Timer aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnBeginSendDocuments);
            aTimer.Interval = Convert.ToInt32(configurador.TimeInterval);
            aTimer.Enabled = true;
        }

        private void OnBeginSendDocuments(object source, ElapsedEventArgs e)
        {
            SVCDocumentosElectronicos.MensajeWCFOfEnviosDeDocumentosPendiente respuesta = null;
            try
            {

                respuesta = client.ObtenerEnviosCorreoPendiente();
                if (respuesta.CodigoError != Util.CODIGO_EXITOSO)
                {
                    Util.InsertarError(respuesta);
                }

                if (respuesta.Contenido.Length > 0)
                {
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Documentos pedientes de enviar al cliente {0}", respuesta.Contenido.Length.ToString()), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Alimentando Queue" });
                    if (queue.GetAllMessages().Length > 0)
                    {
                        queue.Purge();
                    }

                    foreach (SVCDocumentosElectronicos.EnviosDeDocumentosPendiente entidad in respuesta.Contenido)
                    {

                        Message msg = new Message(entidad);
                        using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
                        {
                            queue.Send(msg, MessageQueueTransactionType.Automatic);
                            scope.Complete();
                            configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Se mandó a la cola {0}, el documento {1} pendiente de envío al cliente", queue.QueueName, entidad.DocumentoId), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Alimentando Queue" });
                        }
                        msg = null;
                    }
                }
            }
            catch (Exception ex)
            {
                configurador.ExceptionHandler.HandleException(ex, Util.POLITICA_ERRORES, out Exception nExption);
            }
            finally
            {
                respuesta = null;
            }

        }

    }
}
