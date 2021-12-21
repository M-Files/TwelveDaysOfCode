using MFiles.Extensibility.Applications;
using MFiles.Extensibility.Framework.Applications;
using System;
using System.Collections.Generic;

namespace ExifData
{
    /// <summary>
    /// The main application.
    /// </summary>
    public class MyApplication : VaultApplication
    {
        #region Default implementations (do not typically need to be edited)

        /// <summary>
        /// The intelligence service.
        /// </summary>
        private MyIntelligenceService myIntelligenceService = null;

        /// <summary>
        /// Creates or recreates services for this application.
        /// </summary>
        /// <param name="operationContext">The context for this operation.</param>
        protected override void CreateServices(IOperationContext operationContext)
        {
            // Uncomment the following line to trigger just-in-time debugging when this application gets loaded.
            //System.Diagnostics.Debugger.Launch();

            // Create a new RepositoryConnector.
            this.myIntelligenceService = new MyIntelligenceService(operationContext, this);
        }

        /// <summary>
        /// The services the application offers.
        /// </summary>
        public override IEnumerable<IService> Services => new IService[] { this.myIntelligenceService };

        #endregion
    }
}
