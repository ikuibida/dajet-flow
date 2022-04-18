### DaJet Flow - messaging pipeline framework

DaJet Flow это платформа для построения конвейеров обработки сообщений (пайплайнов).

Конвейер обработки данных (пайплайн) состоит из следующих блоков:
- Источник данных
- Получатель данных
- Обработчик сообщения
- Трансформер сообщения

Каждый конвейер обязательно имеет блок источника в его начале и блок приёмника в его конце.
Между ними может находиться любое количество обработчиков и трансформеров.
Отличия между обработчиками и трансформерами заключается в том, что
обработчик не меняет тип сообщения, то есть такой блок на входе получает
один тип сообщения и на выходе отдаёт такой же тип сообщения.
Трансформер выполняет преобразование одного типа сообщения в другой.

Например, источник SQL Server может выдать сообщение типа "ИсходящееСообщение",
затем трансформер преобразует его в тип сообщения "СообщениеRabbitMQ" и, наконец,
приёмник RabbitMQ записывает сообщение своего типа в очередь RabbitMQ.

В качестве источников и приёмников данных могут быть:
- SQL Server
- PostgreSQL
- RabbitMQ
- Apache Kafka

Конвейер обработки данных может быть построен между двумя произвольными
источниками и приёмниками, как одного типа так, так и разного.
Например:
- SQL Server - SQL Server
- SQL Server - PostgreSQL
- SQL Server - RabbitMQ
- RabbitMQ - Apache Kafka
- и т.д. и т.п.

Конфигурирование конвейера выполняется при помощи файла appsettings.json.

Выполнение обработчиков и трансформеров сообщений выполняется в том порядке,
в котором они указаны в файле конфигурации конвейера.

В одном файле конфигурации можно создать любое количество конвейеров.
Каждый конвейер запускается в отдельном фоновом потоке операционной системы
службой DaJet.Flow.Service.exe.

Пример конфигурационного файла **appsettings.json**: в данном случае настроено
два конвейера. Один передаёт данные из PostgreSQL в RabbitMQ,
а второй - из RabbitMQ в SQL Server.
```json
{
  "Pipelines": [
    {
      "Name": "Pipeline 1: pg -> rmq",
      "IsActive": true,
      "Source": {
        "Type": "PostgreSQL",
        "Consumer": "DaJet.PostgreSQL.Consumer`1",
        "Message": "DaJet.Flow.Contracts.OutgoingMessage",
        "DataMapper": "DaJet.PostgreSQL.DataMappers.OutgoingMessageDataMapper",
        "Options": {
          "ConnectionString": "Host=localhost;Port=5432;Database=dajet_exchange_pg;Username=postgres;Password=postgres;",
          "QueueObject": "РегистрСведений.DaJet_ИсходящаяОчередь",
          "MessagesPerTransaction": "1000"
        }
      },
      "Target": {
        "Type": "RabbitMQ",
        "Producer": "DaJet.RabbitMQ.Producer",
        "Message": "DaJet.RabbitMQ.Message",
        "Options": {
          "RoutingKey": "РИБ.N001.MAIN"
        }
      },
      "Handlers": [
        "DaJet.RabbitMQ.DbToRmqTransformer"
      ]
    },
    {
      "Name": "Pipeline 2: rmq -> ms",
      "IsActive": true,
      "Source": {
        "Type": "RabbitMQ",
        "Consumer": "DaJet.RabbitMQ.Consumer",
        "Message": "DaJet.RabbitMQ.Message",
        "Options": {
          "RoutingKey": "РИБ.N001.MAIN"
        }
      },
      "Target": {
        "Type": "SqlServer",
        "Producer": "DaJet.SqlServer.Producer`1",
        "Message": "DaJet.Flow.Contracts.IncomingMessage",
        "DataMapper": "DaJet.SqlServer.DataMappers.IncomingMessageDataMapper",
        "Options": {
          "ConnectionString": "Data Source=zhichkin;Initial Catalog=dajet_exchange_ms;Integrated Security=True",
          "QueueObject": "РегистрСведений.DaJet_ВходящаяОчередь"
        }
      },
      "Handlers": [
        "DaJet.RabbitMQ.RmqToDbTransformer"
      ]
    }
  ]
}
```

**На текущий момент времени проект находится в стадии прототипа.**

Работа прототипа тестировалась на эталонной конфигурации **dajet.cf**
(см. каталог проекта "1с"). Тестировалось создание следующих конвейеров:
- SQL Server - SQL Server
- SQL Server - PostgreSQL
- PostgreSQL - SQL Server
- PostgreSQL - RabbitMQ
- RabbitMQ - SQL Server
Источник и приёмник для Apache Kafka находятся пока что в разработке.

Дополнительные блоки конвейера могут быть разработаны пользователями
платформы самостоятельно. Планируется реализация механизма подключения
таких блоков в произвольный конвейер в качестве плагинов (plug-in).