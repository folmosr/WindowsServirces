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
            configurador.LogWriter.Write(new LogEntry() { Message = "Instanciando Consumer" });
        }

        public void MessageReceived()
        {
            MessageQueue queue = new MessageQueue(configurador.InitialQueueName);
            queue.ReceiveCompleted += new ReceiveCompletedEventHandler(OnMessageReceived);
            queue.BeginReceive();
        }

        public void OnMessageReceived(Object source, ReceiveCompletedEventArgs e)
        {
            XmlSerializer s = new XmlSerializer(typeof(SVCDocumentosElectronicos.EnviosDeDocumentosPendiente));
            MessageQueue queue = (MessageQueue)source;
            SVCDocumentosElectronicos.MensajeWCFOfBoolean respuesta = null;
            try
            {
                Message msg = queue.EndReceive(e.AsyncResult);
                msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(SVCDocumentosElectronicos.EnviosDeDocumentosPendiente) });

                SVCDocumentosElectronicos.EnviosDeDocumentosPendiente documento = (SVCDocumentosElectronicos.EnviosDeDocumentosPendiente)msg.Body;

                configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Mandando mensaje {0} al cliente", msg.Body), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });

                respuesta = client.EnviarCorreoDocumento(documento);

                if (respuesta.CodigoError != Util.CODIGO_EXITOSO)
                {
                    Util.InsertarError(respuesta);
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Error al enviar documento {0}, mensaje de error {1}", documento.DocumentoId.ToString(), respuesta.MensajeError), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });
                }
                else
                {
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Documento {0} envíado al cliente, respuesta {1}", documento.DocumentoId.ToString(), respuesta.MensajeHumano), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });
                }

            }
            catch (System.Exception ex)
            {
                configurador.ExceptionHandler.HandleException(ex, Util.POLITICA_ERRORES, out Exception nExption);
            }
            finally
            {
                respuesta = null;
                queue.BeginReceive();
            }
        }

    }
}
