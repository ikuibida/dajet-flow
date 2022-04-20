using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaJet.Flow.Contracts
{
    /// <summary>
    /// Табличный интерфейс исходящей очереди сообщений
    /// (непериодический независимый регистр сведений)
    /// </summary>
    [Table("РегистрСведений.ИсходящаяОчередь")]
    [Version(1)] public sealed class OutgoingMessage : IMessage
    {
        /// <summary>
        /// "НомерСообщения" Порядковый номер сообщения (может генерироваться средствами СУБД) - numeric(19,0)
        /// </summary>
        [Column("МоментВремени", Order = 0, TypeName = "numeric(19,0)")]
        [Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MessageNumber { get; set; } = 0L;
        /// <summary>
        /// "Идентификатор" Подстраховка на случай дублирования значения в измерении "НомерСообщения".
        /// Требуется для кода на 1С:Предприятие 8 (тип данных - УникальныйИдентификатор). - binary(16)
        /// </summary>
        [Column("Идентификатор", Order = 1, TypeName = "binary(16)")]
        [Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Uuid { get; set; } = Guid.Empty;
        /// <summary>
        /// "Заголовки" Заголовки сообщения в формате JSON { "ключ": "значение" } - nvarchar(max)
        /// </summary>
        [Column("Заголовки", TypeName = "nvarchar(max)")] public string Headers { get; set; } = string.Empty;
        /// <summary>
        /// "Отправитель" Код или UUID отправителя сообщения - nvarchar(36)
        /// </summary>
        [Column("Отправитель")] public string Sender { get; set; } = string.Empty;
        /// <summary>
        /// "Получатели" Коды или UUID получателей сообщения в формате CSV - nvarchar(max)
        /// </summary>
        [Column("Получатели")] public string Recipients { get; set; } = string.Empty;
        /// <summary>
        /// "ТипСообщения" Тип сообщения, например, "Справочник.Номенклатура" - nvarchar(1024)
        /// </summary>
        [Column("ТипСообщения", TypeName = "nvarchar(1024)")] public string MessageType { get; set; } = string.Empty;
        /// <summary>
        /// "ТелоСообщения" Тело сообщения в формате JSON или XML - nvarchar(max)
        /// </summary>
        [Column("ТелоСообщения", TypeName = "nvarchar(max)")] public string MessageBody { get; set; } = string.Empty;
        /// <summary>
        /// "ДатаВремя" Время создания сообщения - datetime2
        /// </summary>
        [Column("ДатаВремя", TypeName = "datetime2")] public DateTime DateTimeStamp { get; set; } = DateTime.MinValue;
    }
}