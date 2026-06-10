namespace TeknikServisAPI.Models
{
    /// <summary>
    /// İş türü - nereden geldiğini/tipini belirtir
    /// DB'de int olarak saklanır
    /// </summary>
    public enum WorkItemKind
    {
        /// <summary>Talep (Request) - Çağrı/istek geldi</summary>
        Request = 0,
        
        /// <summary>Planlı İş - Kampüs gezisi, bakım vb.</summary>
        PlannedWork = 1,
        

    }

    /// <summary>
    /// Kanban sütunu / iş durumu
    /// DB'de int olarak saklanır
    /// </summary>
    public enum WorkItemStage
    {
        /// <summary>Backlog / Yapılacaklar</summary>
        Backlog = 0,
        
        
        /// <summary>Tamamlandı / Raporlandı</summary>
        Done = 2,
        
        /// <summary>İptal / Gerek Yok</summary>
        Cancelled = 3
    }

    /// <summary>
    /// Öncelik seviyesi
    /// DB'de int olarak saklanır
    /// </summary>
    public enum WorkItemPriority
    {
        /// <summary>Düşük öncelik</summary>
        Low = 0,
        
        /// <summary>Normal öncelik</summary>
        Normal = 1,
        
        /// <summary>Yüksek öncelik</summary>
        High = 2,
        
        /// <summary>Kritik / Acil</summary>
        Critical = 3
    }
}
