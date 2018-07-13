using MailKit;
using MailKit.Net.Imap;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using LectorDeCorreo.SVCDocumentosElectronicos;
using MailKit.Net.Smtp;
using System.Timers;
using LectorDeCorreo.SVCSujetoContable;

namespace LectorDeCorreo
{
    public class Producer
    {
        #region Private Fields

        Timer aTimer;
        SVCDocumentosElectronicos.DocumentosElectronicosClient documentClient;
        MailKit.IMailFolder inbox;
        int interval;
        Logger logger;
        string password;
        int port;
        string reportBugsAddress;
        string reportBugsName;
        string server;
        int smtpPort;
        SVCSujetoContable.SujetoContableClient sujetoContableClient;
        string userAlias;
        string userName;

        #endregion Private Fields

        #region Public Constructors

        public Producer(Logger logger)
        {
            this.server = ConfigurationManager.AppSettings["Servidor"];
            this.port = Convert.ToInt16(ConfigurationManager.AppSettings["Puerto"]);
            this.userName = ConfigurationManager.AppSettings["CuentaCorreo"];
            this.password = ConfigurationManager.AppSettings["Password"];
            this.userAlias = ConfigurationManager.AppSettings["CuentaAlias"];
            this.reportBugsName = ConfigurationManager.AppSettings["CuentaBugsAlias"];
            this.reportBugsAddress = ConfigurationManager.AppSettings["CuentaBugsAddress"];
            this.smtpPort = Convert.ToInt16(ConfigurationManager.AppSettings["smtpPort"]);
            this.interval = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalToProduce"]);
            this.logger = logger;
            sujetoContableClient = new SVCSujetoContable.SujetoContableClient();
            documentClient = new SVCDocumentosElectronicos.DocumentosElectronicosClient();
            aTimer = new Timer();
        }

        #endregion Public Constructors


        #region Public Methods

        public void Start()
        {

            aTimer.Elapsed += new ElapsedEventHandler(Fecth);
            aTimer.Interval = this.interval;
            aTimer.Enabled = true;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Encargado de cargar Documento XML
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="respuesta"></param>
        /// <param name="respuestaCarga"></param>
        /// <param name="docCargado"></param>
        /// <param name="sujetoContable"></param>
        /// <param name="representacion"></param>
        private void CargarDocumentoXML(XPathDocument doc, out MensajeWCFOfDocumentoElectronico respuestaCarga, out DocumentoElectronico docCargado, SVCSujetoContable.SujetoContable sujetoContable, StringBuilder representacion)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(representacion))
            {
                doc.CreateNavigator().WriteSubtree(xmlWriter);

            }
            respuestaCarga = documentClient.CargarXMLCompra(sujetoContable.Id, representacion.ToString(), "CORREO", -2);
            if (respuestaCarga.CodigoError != Logger.CODIGO_EXITOSO)
            {
                docCargado = null;
                this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Imposible cargar documento, error: código {0}, mensaje {1}", respuestaCarga.CodigoError, respuestaCarga.MensajeHumano), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
            }
            else
            {
                docCargado = respuestaCarga.Contenido.First();
                this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Documento cargado N°:{0}", docCargado.Id.ToString()), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
            }
        }

        /// <summary>
        /// Encargado de Cargar PDF a documento Electrónico
        /// </summary>
        /// <param name="docCargado"></param>
        /// <param name="respuestaCargaPDF"></param>
        /// <param name="summary"></param>
        /// <param name="attachment"></param>
        /// <param name="mime"></param>
        private void CargarPDF(DocumentoElectronico docCargado, out SVCDocumentosElectronicos.MensajeWCFOfBoolean respuestaCargaPDF, IMessageSummary summary, BodyPartBasic attachment)
        {
            MimePart mime = null;
            this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Se encuentra PDF adjunto para documento N°:{0}", docCargado.Id.ToString()), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
            mime = (MimePart)inbox.GetBodyPart(summary.UniqueId, attachment);
            using (MemoryStream stream = new MemoryStream())
            {
                mime.Content.DecodeTo(stream);
                stream.Position = 0;
                respuestaCargaPDF = documentClient.CargarPDFCompra(docCargado.Id, stream.ToArray(), "pdf", -2);
                if (respuestaCargaPDF.CodigoError != Logger.CODIGO_EXITOSO)
                {
                    this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Imposible cargar PDF adjunto para documento N°:{0}, error:{1}", docCargado.Id.ToString(), respuestaCargaPDF.MensajeError), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                }
                else
                {
                    this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("PDF adjunto cargado para documento N°:{0}", docCargado.Id.ToString()), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                }

            }
        }

        /// <summary>
        /// Elimina email
        /// </summary>
        /// <param name="summary"></param>
        private void DeleteMessage(IMessageSummary summary)
        {
            inbox.AddFlags(summary.UniqueId, MessageFlags.Deleted, true);
            inbox.Expunge();
        }

        /// <summary>
        /// Obtiene y filtra email con documentos XML como adjunto
        /// </summary>
        private void Fecth(object source, ElapsedEventArgs e)
        {
            aTimer.Enabled = false;
            while (true)
            {
                using (var client = new ImapClient())
                {

                    client.Connect(this.server, this.port, true);
                    client.Authenticate(this.userName, this.password);

                    inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);
                    this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Conectado a la casilla de correo indicada {0}", this.userName), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });

                    FilterMessage();

                    client.Disconnect(true);
                }
                System.Threading.Thread.Sleep(this.interval);
            }
        }

        /// <summary>
        /// Toma solo mensajes con XML adjuntos
        /// </summary>
        private void FilterMessage()
        {
            string nit = string.Empty;
            XPathDocument doc = null;
            StringBuilder representacion = new StringBuilder();

            SVCSujetoContable.MensajeWCFOfSujetoContable respuesta;
            SVCDocumentosElectronicos.MensajeWCFOfDocumentoElectronico respuestaCarga;
            SVCDocumentosElectronicos.DocumentoElectronico docCargado;
            SVCDocumentosElectronicos.MensajeWCFOfBoolean respuestaCargaPDF;
            SVCSujetoContable.SujetoContable sujetoContable;

            foreach (var summary in inbox.Fetch(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.BodyStructure))
            {

                if (summary.Body is BodyPartMultipart multipart)
                {
                    var attachments = multipart.BodyParts.OfType<BodyPartBasic>();

                    foreach (var item in attachments)
                    {
                        this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("MIME TYPE:{0}", item.ContentType.MimeType), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                    }
                    if (attachments.Count() > 0)
                    {
                        var attachment = attachments.FirstOrDefault(x => x.ContentType.MimeType == "text/xml" || x.FileName.Contains(".xml"));
                        if (attachment != null)
                        {

                            GetNit(out nit, out doc, summary, attachment);

                            if (nit == string.Empty)
                            {
                                this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Party Identification no encontrada en XML descargado"), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                                continue;
                            }

                            respuesta = sujetoContableClient.ObtenerPorNit(Convert.ToInt64(nit));

                            if (respuesta.CodigoError != Logger.CODIGO_EXITOSO)
                            {
                                string info = String.Format("NIT {0} no se encuentra registrado como Party Identification", nit);
                                this.logger.LogWriter.Write(new LogEntry() { Message = info, Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                                ReportMessage(summary, attachments, info);
                                DeleteMessage(summary);
                                continue;
                            }

                            this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Party Identification en XML:{0}", nit), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                            sujetoContable = respuesta.Contenido.First();

                            CargarDocumentoXML(doc, out respuestaCarga, out docCargado, sujetoContable, representacion);

                            if (respuestaCarga.CodigoError != Logger.CODIGO_EXITOSO)
                            {
                                ReportMessage(summary, attachments, respuestaCarga);
                                DeleteMessage(summary);
                                continue;
                            }
                            /**
                             * Se procesa PDF
                             *
                             */
                            attachment = attachments.FirstOrDefault(x => x.ContentType.MimeType == "application/pdf" || x.FileName.Contains(".pdf"));
                            if (attachment != null)
                            {
                                CargarPDF(docCargado, out respuestaCargaPDF, summary, attachment);
                            }
                            DeleteMessage(summary);
                            this.logger.LogWriter.Write(new LogEntry() { Message = String.Format("Se elimina mensaje:{0}", summary.UniqueId.ToString()), Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Get NIT from XML Adjunto
        /// </summary>
        /// <param name="nit"></param>
        /// <param name="doc"></param>
        /// <param name="manager"></param>
        /// <param name="navigator"></param>
        /// <param name="query"></param>
        /// <param name="summary"></param>
        /// <param name="attachment"></param>
        /// <param name="mime"></param>
        private void GetNit(out string nit, out XPathDocument doc, IMessageSummary summary, BodyPartBasic attachment)
        {
            XmlNamespaceManager manager = null;
            XPathNavigator navigator = null;
            XPathExpression query = null;
            MimePart mime = null;
            mime = (MimePart)inbox.GetBodyPart(summary.UniqueId, attachment);
            using (MemoryStream stream = new MemoryStream())
            {
                mime.Content.DecodeTo(stream);
                stream.Position = 0;
                doc = new XPathDocument(stream);
            }

            navigator = doc.CreateNavigator();
            query = navigator.Compile("//fe:AccountingCustomerParty/fe:Party/cac:PartyIdentification/cbc:ID");
            manager = new XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("fe", "http://www.dian.gov.co/contratos/facturaelectronica/v1");
            manager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            manager.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            query.SetContext(manager);
            nit = navigator.SelectSingleNode(query).Value;
        }

        /// <summary>
        /// Manda email reporteando error
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="attachments"></param>
        /// <param name="info"></param>
        private void ReportMessage(IMessageSummary summary, IEnumerable<BodyPartBasic> attachments, string info)
        {
            try
            {
                var builder = new BodyBuilder()
                {
                    TextBody = info
                };
                send(summary, attachments, builder);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Cuando un documento no se puede cargar (Por x razón) este método se encargará
        /// de reenviar dicho documento a otro correo
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="respuestaCarga"></param>
        private void ReportMessage(IMessageSummary summary, IEnumerable<BodyPartBasic> attachments, MensajeWCFOfDocumentoElectronico respuestaCarga)
        {
            try
            {

                var builder = new BodyBuilder()
                {
                    TextBody = String.Format("El intento de carga de documento adjunto al mensage {0} ha fallado! motivo: {1}",
                         summary.UniqueId.ToString(), respuestaCarga.MensajeHumano
                    )
                };
                send(summary, attachments, builder);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Conecta al servidor y arma mensaje y lo envía 
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="attachments"></param>
        /// <param name="builder"></param>
        private void send(IMessageSummary summary, IEnumerable<BodyPartBasic> attachments, BodyBuilder builder)
        {
            var message = new MimeMessage();
            MimePart mime = null;
            message.From.Add(new MailboxAddress(this.userAlias, this.userName));
            message.To.Add(new MailboxAddress(this.reportBugsName, this.reportBugsAddress));
            message.Subject = string.Format(Logger.BUG_SUBJECT);
            foreach (var item in attachments)
            {
                mime = (MimePart)inbox.GetBodyPart(summary.UniqueId, item);
                builder.Attachments.Add(mime);
            }
            message.Body = builder.ToMessageBody();

            SendMessage(message);

            this.logger.LogWriter.Write(new LogEntry() { Message = builder.TextBody, Categories = new List<string> { "General" }, Priority = 1, ProcessName = Logger.PROCESS_NAME });
        }

        /// <summary>
        /// Manda correo
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(MimeMessage message)
        {
            using (var client = new SmtpClient())
            {
                client.Connect(this.server, this.smtpPort, MailKit.Security.SecureSocketOptions.Auto);
                client.Authenticate(this.userName, this.password);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        #endregion Private Methods
    }
}
