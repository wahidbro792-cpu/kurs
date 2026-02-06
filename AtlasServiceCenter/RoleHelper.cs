using System;

namespace AtlasServiceCenter
{
    public static class RoleHelper
    {
        private static bool IsRole(string roleName, params string[] expected)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            foreach (var role in expected)
            {
                if (string.Equals(roleName, role, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsOwner(string roleName) =>
            IsRole(roleName, "Владелец", "Руководитель компании");

        public static bool IsAdmin(string roleName) =>
            IsRole(roleName, "Администратор", "Администратор системы");

        public static bool IsReceptionManager(string roleName) =>
            IsRole(roleName, "Менеджер", "Менеджер приёмки", "Менеджер приема");

        public static bool IsMaster(string roleName) =>
            IsRole(roleName, "Мастер");

        public static bool IsCashier(string roleName) =>
            IsRole(roleName, "Кассир", "Менеджер выдачи", "Кассир/Менеджер выдачи");

        public static bool CanManageUsers(string roleName) =>
            IsOwner(roleName) || IsAdmin(roleName);

        public static bool CanManageEmployees(string roleName) =>
            IsOwner(roleName);

        public static bool CanAccessSalary(string roleName) =>
            IsOwner(roleName);

        public static bool CanCreateOrders(string roleName) =>
            IsAdmin(roleName) || IsReceptionManager(roleName);

        public static bool CanDeleteOrders(string roleName) =>
            IsAdmin(roleName) || IsReceptionManager(roleName);

        public static bool CanEditDiscount(string roleName) =>
            IsOwner(roleName) || IsCashier(roleName);

        public static bool CanEditPrices(string roleName) =>
            IsOwner(roleName) || IsAdmin(roleName) || IsReceptionManager(roleName);

        public static bool CanAssignMaster(string roleName) =>
            IsOwner(roleName) || IsAdmin(roleName) || IsReceptionManager(roleName);

        public static bool CanEditStatus(string roleName) =>
            IsOwner(roleName) || IsAdmin(roleName) || IsReceptionManager(roleName) || IsMaster(roleName);

        public static bool CanEditCoreOrderFields(string roleName) =>
            IsOwner(roleName) || IsAdmin(roleName) || IsReceptionManager(roleName);
    }
}
