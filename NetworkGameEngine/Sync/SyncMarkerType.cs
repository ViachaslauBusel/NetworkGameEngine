using System.ComponentModel;

namespace NetworkGameEngine.Sync
{
    public enum SyncMarkerType
    {
        //Метка для синхронизации с клиентом
        Client = 1,
        //Метка для синхронизации с базой данных
        Database = 2,
        //Метка для дщкальной синхронизации на сервере
        Local = 3,
    }
}
