using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DaJet.Flow.Contracts.V1
{
    /// <summary>
    /// Табличный интерфейс входящей очереди сообщений
    /// (непериодический независимый регистр сведений)
    /// </summary>
    [Table("РегистрСведений.ВходящаяОчередь")]
    [Version(1)] public sealed class IncomingMessage : IMessage
    {
        /// <summary>
        /// "НомерСообщения" Порядковый номер сообщения (может генерироваться средствами СУБД) - numeric(19,0)
        /// </summary>
        [Column("НомерСообщения", Order = 0, TypeName = "numeric(19,0)")]
        [Key][DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MessageNumber { get; set; } = 0L;
        /// <summary>
        /// "Заголовки" Заголовки сообщения в формате JSON { "ключ": "значение" } - nvarchar(max)
        /// </summary>
        [Column("Заголовки", TypeName = "nvarchar(max)")] public string Headers { get; set; } = string.Empty;
        /// <summary>
        /// "Отправитель" Код или UUID отправителя сообщения - nvarchar(36)
        /// </summary>
        [Column("Отправитель", TypeName = "nvarchar(36)")] public string Sender { get; set; } = string.Empty;
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
        [Column("ДатаВремя")] public DateTime DateTimeStamp { get; set; } = DateTime.MinValue;
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