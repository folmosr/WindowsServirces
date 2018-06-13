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
            XmlSerializer s = new XmlSerializer(typeof(SVCDocumentosElectronicos.EnviosPendientesEnDIAN));
            MessageQueue queue = (MessageQueue)source;
            try
            {
                Message msg = queue.EndReceive(e.AsyncResult);
                msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(SVCDocumentosElectronicos.EnviosPendientesEnDIAN) });

                SVCDocumentosElectronicos.EnviosPendientesEnDIAN documento = (SVCDocumentosElectronicos.EnviosPendientesEnDIAN)msg.Body;

                configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Mensaje recibido en la cola, consultado estado de documento en DIAN {0} ", msg.Body), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });

                SVCDocumentosElectronicos.MensajeWCFOfEstadoDocumento respuesta = client.ConsultaEstadoDocumento(documento);

                if (respuesta.CodigoError != Util.CODIGO_EXITOSO)
                {
                    Util.InsertarError(respuesta);
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Error al consultar estado de documento {0}, mensaje de error {1}", documento.Id, respuesta.MensajeError), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });
                }
                else
                {
                    configurador.LogWriter.Write(new LogEntry() { Message = String.Format("Documento {0} consultado en DIAN, respuesta {1}", documento.Id, respuesta.Contenido.First().ConsultaTransaccionDocumento.First().DescripcionEstado), Categories = new List<string> { "eventLog" }, Priority = 1, ProcessName = "Leyendo Queue" });
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
