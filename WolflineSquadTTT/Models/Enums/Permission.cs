using WolflineSquadTTT.Infrastructure;

namespace WolflineSquadTTT.Models.Enums
{
    public enum Permission
    {
        /// <summary>
        /// Allows user to create new polls.
        /// </summary>
        [PermissionGroup("Poll")] CreatePoll = 1,
        /// <summary>
        /// Allows user to edit polls.
        /// </summary>
        [PermissionGroup("Poll")] EditPoll = 2,
        /// <summary>
        /// Allows user to delete polls.
        /// </summary>
        [PermissionGroup("Poll")] DeletePoll = 3,
        /// <summary>
        /// Allows user to give other users rights. Requires the specific right itself or it cant be given (Except Superadmins).
        /// </summary>
        [PermissionGroup("Administrative")] ManageRights = 4,
        /// <summary>
        /// Allows user to view the Test SQL endpoint.
        /// </summary>
        ViewTestSQL = 5,
        /// <summary>
        /// Allows user to view play time statistics of everyone else.
        /// </summary>
        [PermissionGroup("Administrative")] ViewPlayerActivity = 6,
        /// <summary>
        /// Allows user to view and answer polls.
        /// </summary>
        [PermissionGroup("Poll")] ViewPolls = 7,
        /// <summary>
        /// Allows user to view individual poll responses (which user picked what).
        /// </summary>
        [PermissionGroup("Poll")] ViewIndividualResponses = 8,
        /// <summary>
        /// Allows user to view other players' Pointshop 2 inventories.
        /// </summary>
        [PermissionGroup("Pointshop 2")] ViewInventories = 9,
        /// <summary>
        /// Allows user to add and manage webhooks (e.g. market notifications).
        /// </summary>
        [PermissionGroup("Webhooks")] AddWebhooks = 10,
        /// <summary>
        /// Allows user to view the Pointshop 2 transaction history.
        /// </summary>
        [PermissionGroup("Pointshop 2")] ViewTransactions = 11,

        /// <summary>
        /// Allows user to do anything. Use with caution!
        /// </summary>
        [PermissionGroup("Superadmin")] SuperAdministrator = int.MaxValue   // Danger! Overrides any other permission. Superadmin can do anything.
    }
}
