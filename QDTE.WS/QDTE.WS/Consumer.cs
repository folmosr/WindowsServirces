using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Diagnostics;
using Microsoft.Practices.Unity;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Unity;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using WSPlantilla.SVCDocumentosElectronicos;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System.IO;
using System.Xml.Serialization;

namespace WSPlantilla
{
    public class Consumer
    {
        private SVCDocumentosElectronicos.DocumentosElectronicosClient client;
        Configurador configurador;

        public Consumer(Configurador parentConfigurador)
        {
            client = new SVCDocumentosElectronicos.DocumentosElectronicosClient();
            configurador = parentConfigurador;
        }

        public void MessageReceived()
        {
            MessageQueue queue = new MessageQueue(configurador.InitialQueueName);
            queue.ReceiveCompleted += new ReceiveCompletedEventHandler(OnMessageReceived);
            queue.BeginReceive();
        }

        public void OnMessageReceived(Object source, ReceiveCompletedEventArgs e)
        {
            XmlSerializer s = new XmlSerializer(typeof(SVCDocumentosElectronicos.EnviosPendientesPosteoDIAN));
            MessageQueue queue = (MessageQueue)source;
            try
            {
                Message msg = queue.EndReceive(e.AsyncResult);
                msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(SVCDocumentosElectronicos.EnviosPendientesPosteoDIAN) });

                SVCDocumentosElectronicos.EnviosPendientesPosteoDIAN documento = (SVCDocumentosElectronicos.EnviosPendientesPosteoDIAN)msg.Body;

                configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Mensaje recibido en la cola, posteando documento a la DIAN {0} a la DIAN", msg.Body), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });

                SVCDocumentosElectronicos.MensajeWCFOfBoolean respuesta = client.PostearDocumentoPendiente(documento);

                if (respuesta.CodigoError != Util.CODIGO_EXITOSO)
                {
                    Util.InsertarError(respuesta);
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Error al postear documento {0}, mensaje de error {1}", documento.Id, respuesta.MensajeError), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });
                }
                else
                {
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Documento {0} autorizado por la DIAN, respuesta {1}", documento.Id, respuesta.MensajeHumano), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });
                }
            }
            catch (System.Exception ex)
            {
                configurador.ExceptionHandler.HandleException(ex, Util.POLITICA_ERRORES, out Exception nExption);
            }
            finally
            {
                queue.BeginReceive();
            }
        }

    }
}
