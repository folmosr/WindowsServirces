using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Unity;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace WSPlantilla
{
    public static class Util
    {

        public static readonly string CODIGO_EXITOSO = "E_00";
        public static readonly string POLITICA_ERRORES = "PostingDocumentsPolicy";

        public static Boolean InsertarError(SVCDocumentosElectronicos.MensajeWCFOfEnviosDeDocumentosPendiente entidad)
        {
            try
            {
                Message msg = new Message();
                MessageQueue queue = new MessageQueue(@"FormatName:Direct=OS:" + ConfigurationManager.AppSettings["NombreColaMSMQError"]);
                string queueName = ConfigurationManager.AppSettings["NombreColaMSMQError"];

                if (!MessageQueue.Exists(queueName))
                {
                    MessageQueue.Create(queueName, true);
                }

                msg.Body = entidad;

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    queue.Send(msg, MessageQueueTransactionType.Automatic);
                    scope.Complete();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public static Boolean InsertarError(SVCDocumentosElectronicos.MensajeWCFOfBoolean entidad)
        {
            try
            {
                Message msg = new Message();
                MessageQueue queue = new MessageQueue(@"FormatName:Direct=OS:" + ConfigurationManager.AppSettings["NombreColaMSMQError"]);
                string queueName = ConfigurationManager.AppSettings["NombreColaMSMQError"];

                if (!MessageQueue.Exists(queueName))
                {
                    MessageQueue.Create(queueName, true);
                }

                msg.Body = entidad;

                using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    queue.Send(msg, MessageQueueTransactionType.Automatic);
                    scope.Complete();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

    }
}
