using MFiles.Extensibility.ExternalObjectTypes;
using System;

namespace ImportGists
{
    /// <summary>
    /// An implementation of IExternalObjectTypeConnection.
    /// Parts for deleting data.
    /// </summary>
    public partial class DataSourceConnection
    {
        /// <summary>
        /// Returns true if the data source supports deleting.
        /// </summary>
        public override bool CanDelete()
        {
            return false;
        }

        /// <summary>
        /// Validates that the delete statement is valid in this data source.
        /// </summary>
        /// <param name="extidColumn">Information about the column that is currently used as the external id.</param>
        /// <returns>True if the delete and selectExtId statements are valid for this data source.</returns>
        public override bool ValidateDeleteStatement(ColumnDefinition extidColumn)
        {
            // TODO: Validate the delete statement, if appropriate.
            return true;
        }

        /// <summary>
        /// Deletes item from the data source.
        /// </summary>
        /// <param name="deleteStatement">Data source specific configuration string
        /// that describes how to perform the deletion for validation</param>
        /// <param name="extid">The external id of the item to delete.</param>
        public override void DeleteItem(ColumnValue extid)
        {
            // TODO: Delete the item.  extid.Value should be object's external ID.
            throw new NotImplementedException();
        }
    }
}
