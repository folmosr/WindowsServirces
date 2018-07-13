using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Unity;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LectorDeCorreo
{
    public class Logger
    {
        public static readonly string CODIGO_EXITOSO = "E_00";
        public static readonly string BUG_SUBJECT = "ERROR EN CARGA DE DOCUMENTO XML";
        public static readonly string POLITICA_ERRORES = "ServicesPolicy";
        public static readonly string PROCESS_NAME = "Lecturas y/o procesamiento de correos";
        #region fields

        private IUnityContainer _container = new UnityContainer().AddNewExtension<EnterpriseLibraryCoreExtension>();
        ExceptionManager _exceptionHandler = null;
        LogWriter _LogWriter = null;
        #endregion

        public ExceptionManager ExceptionHandler
        {
            get
            {
                if (_exceptionHandler == null)
                {
                    _exceptionHandler = _container.Resolve<ExceptionManager>();
                }
                return _exceptionHandler;
            }
        }

        public LogWriter LogWriter
        {
            get
            {
                if (_LogWriter == null)
                {
                    _LogWriter = _container.Resolve<LogWriter>();
                }

                return _LogWriter;
            }
        }
    }
}
