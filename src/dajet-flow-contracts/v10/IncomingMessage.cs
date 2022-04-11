using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaJet.Flow.Contracts.V10
{
    /// <summary>
    /// Табличный интерфейс входящей очереди сообщений
    /// (непериодический независимый регистр сведений)
    /// </summary>
    [Table("РегистрСведений.ВходящаяОчередь")]
    [Version(10)] public sealed class IncomingMessage : IMessage
    {
        /// <summary>
        /// "МоментВремени" Порядковый номер сообщения (может генерироваться средствами СУБД) - numeric(19,0)
        /// </summary>
        [Column("МоментВремени", Order = 0, TypeName = "numeric(19,0)")]
        [Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MessageNumber { get; set; } = 0L;
        /// <summary>
        /// "Идентификатор" Уникальный идентификатор сообщения - binary(16)
        /// </summary>
        [Column("Идентификатор", Order = 1, TypeName = "binary(16)")]
        [Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Uuid { get; set; } = Guid.NewGuid();
        /// <summary>
        /// "Отправитель" Код или UUID отправителя сообщения - nvarchar(36)
        /// </summary>
        [Column("Отправитель")] public string Sender { get; set; } = string.Empty;
        /// <summary>
        /// "ТипСообщения" Тип сообщения, например, "Справочник.Номенклатура" - nvarchar(1024)
        /// </summary>
        [Column("ТипСообщения")] public string MessageType { get; set; } = string.Empty;
        /// <summary>
        /// "ТелоСообщения" Тело сообщения в формате JSON или XML - nvarchar(max)
        /// </summary>
        [Column("ТелоСообщения")] public string MessageBody { get; set; } = string.Empty;
        /// <summary>
        /// "ТипОперации" Тип операции: INSERT, UPDATE или DELETE - nvarchar(6)
        /// </summary>
        [Column("ТипОперации")] public string OperationType { get; set; } = string.Empty;
        /// <summary>
        /// "ДатаВремя" Время создания сообщения - datetime2
        /// </summary>
        [Column("ДатаВремя")] public DateTime DateTimeStamp { get; set; } = DateTime.Now;
        /// <summary>
        /// "ОписаниеОшибки" Описание ошибки, возникшей при обработке сообщения - nvarchar(1024)
        /// </summary>
        [Column("ОписаниеОшибки")] public string ErrorDescription { get; set; } = string.Empty;
        /// <summary>
        /// "КоличествоОшибок" Количество неудачных попыток обработки сообщения - numeric(2,0)
        /// </summary>
        [Column("КоличествоОшибок")] public int ErrorCount { get; set; } = 0;
    }
}