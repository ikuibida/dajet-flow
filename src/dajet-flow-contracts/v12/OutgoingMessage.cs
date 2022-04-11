using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaJet.Flow.Contracts.V12
{
    /// <summary>
    /// Табличный интерфейс исходящей очереди сообщений
    /// (непериодический независимый регистр сведений)
    /// </summary>
    [Table("РегистрСведений.ИсходящаяОчередь")]
    [Version(12)] public sealed class OutgoingMessage : IMessage
    {
        /// <summary>
        /// "МоментВремени" Порядковый номер сообщения (может генерироваться средствами СУБД) - numeric(19,0)
        /// </summary>
        [Column("МоментВремени")][Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MessageNumber { get; set; } = 0L;
        /// <summary>
        /// "Идентификатор" Уникальный идентификатор сообщения - binary(16)
        /// </summary>
        [Column("Идентификатор")][Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Uuid { get; set; }
        /// <summary>
        /// "Заголовки" Заголовки сообщения в формате JSON { "ключ": "значение" } - nvarchar(max)
        /// </summary>
        [Column("Заголовки")] public string Headers { get; set; } = string.Empty;
        /// <summary>
        /// "ДатаВремя" Время создания сообщения - datetime2
        /// </summary>
        [Column("ДатаВремя")] public DateTime DateTimeStamp { get; set; } = DateTime.Now;
        /// <summary>
        /// "ИдентификаторОбъекта" Уникальный идентификатор объекта 1С в теле сообщения - binary(16)
        /// </summary>
        [Column("Ссылка")] public Guid Reference { get; set; } = Guid.Empty;
    }
}