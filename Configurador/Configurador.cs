using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Unity;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSPlantilla
{
    public sealed class Configurador
    {
        #region fields

        private IUnityContainer _container = new UnityContainer().AddNewExtension<EnterpriseLibraryCoreExtension>();
        ExceptionManager _exceptionHandler = null;
        LogWriter _LogWriter = null;
        #endregion


        #region Public Properties
        public string InitialQueueName { get; private set; }
        public string FinalQueueName { get; private set; }
        public string ErrorQueueName { get; private set; }
        public int ThreadsQuantity { get; private set; }
        public string ServiceName { get; private set; }
        public int TimeInterval { get; private set; }


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

        #endregion

        private Configurador()
        {
            SetConfigurationsValues();
        }

        private void SetConfigurationsValues()
        {
            var initialQueueName = GetConfigurationValue("NombreColaMSMQInicio");
            var finalQueueName = GetConfigurationValue("NombreColaMSMQFinal");
            var errorQueueName = GetConfigurationValue("NombreColaMSMQError");
            var threadsQuantity = GetConfigurationValue("CantidadDeHilos");
            var serviceName = GetConfigurationValue("NombreServicio");
            var timeInterval = GetConfigurationValue("IntervalToProduce");


            InitialQueueName = initialQueueName;
            FinalQueueName = finalQueueName;
            ErrorQueueName = errorQueueName;
            ThreadsQuantity = GetPositiveIntValue(threadsQuantity);
            ServiceName = serviceName;
            TimeInterval = GetPositiveIntValue(timeInterval);
        }

        private string GetConfigurationValue(string configurationName)
        {
            var configurationValue = ConfigurationManager.AppSettings[configurationName];

            if (!IsValidConfigurationValue(configurationValue))
                throw new ApplicationException($"Configuración: {configurationName} no definida en archivo de configuración.");

            return configurationValue;
        }

        private bool IsValidConfigurationValue(string value)
        {
            return !(string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value));
        }

        private int GetPositiveIntValue(string value)
        {

            ToInt(value, out int number);
            IsPositiveIntValue(number);

            return number;


            void ToInt(string originalValue, out int returnValue)
            {
                if (!int.TryParse(originalValue, out returnValue))
                {
                    throw new ApplicationException("No se puede convertir valor a numérico");
                }
            }

            void IsPositiveIntValue(int numberToValidate)
            {
                if (numberToValidate <= 0)
                {
                    throw new ApplicationException("Valor deber ser mayor que cero");
                }
            }

        }

        public static Configurador Instance { get; } = new Configurador();
    }
}
