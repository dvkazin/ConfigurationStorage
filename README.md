# Сервис хранения конфигурации
Хранение конфигурации в произвольных файлах json

## Конфигурирование
Все настройки приложения, расположены в конфигурационном файле *appsettings.json*.

- **ProfilesDirectory** - каталог где хранится конфигурация
- **SecurityDirectory** - каталог hash паролем администратора.
- **TokenLifetime** - время(в минутах) жизни token, выдаваемого при успешной аутентификации.

## Организация хранения конфигурации
Конфигурация хранится в каталоге заданном в параметре **ProfilesDirectory** и имеет двухуровневую структуру.

1. Каталог - профиль.
2. Файл json - секция в профиле.

Для определения профиля по умолчанию, создаётся символическая ссылку с именем:  **Default** - для Wondows и **default** - для Linux.

Для создания символической ссылки в Windows, на профиль с именем PostgreSQL, используйте команду:
```shell
mklink /D C:\ConfigurationStorage\Profiles\Default C:\ConfigurationStorage\Profiles\PostgreSQL
```

Для конфигурирования службы предусмотрены API. 

## Установка

### Windows

Для установки приложения как службу, запустите от имени администратора файл **Install/Windows/install_as_service.cmd**
