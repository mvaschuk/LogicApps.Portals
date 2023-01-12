namespace LogicApps.Portals.Plugin
{
    public enum PluginStage
    {
        /// <summary>
        /// Stage in the pipeline for plug-ins that are to execute before the main system operation. 
        /// Plug-ins registered in this stage may execute outside the database transaction.
        /// </summary>
        PreValidation = 10,

        /// <summary>
        /// Stage in the pipeline for plug-ins that are to execute before the main system operation. 
        /// Plug-ins registered in this stage are executed within the database transaction.
        /// </summary>
        PreOperation = 20,

        /// <summary>
        /// In-transaction main operation of the system, such as create, update, delete, and so on. 
        /// No custom plug-ins can be registered in this stage. For internal use only.
        /// </summary>
        MainOperation = 30,

        /// <summary>
        /// Stage in the pipeline for plug-ins which are to execute after the main operation. 
        /// Plug-ins registered in this stage are executed within the database transaction.
        /// </summary>
        PostOperation = 40,

        /// <summary>
        /// Stage in the pipeline for plug-ins which are to execute after the main operation. 
        /// Plug-ins registered in this stage may execute outside the database transaction. 
        /// This stage only supports Microsoft Dynamics CRM 4.0 based plug-ins.
        /// </summary>
        PostOperation_Deprecated = 50
    }
}
