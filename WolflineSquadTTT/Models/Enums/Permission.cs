using System.ComponentModel;
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
        /// Allows user to give other users rights. Requires the specific right itself or it cant be given.
        /// </summary>
        [PermissionGroup("Administrative")] ManageRights = 4,
        /// <summary>
        /// Allows user to view the Test SQL endpoint.
        /// </summary>
        ViewTestSQL = 5,

        /// <summary>
        /// Allows user to do anything. Use with caution!
        /// </summary>
        [PermissionGroup("Sueradmin")] SuperAdministrator = int.MaxValue   // Danger! Overrides any other permission. Superadmin can do anything.
    }
}
