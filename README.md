# GetFrame

Извлечение кадра из видео и сохранение в PNG

Приложение для Android и Windows (реализация UI на Avalonia) 

Функционал: Выбрать видео (MP4 и другие контейнеры по возможности), выбрать номер кадра и показать превью, сохранить кадр в PNG.  Язык  интерфейса и сообщений Английский.

Сделай страничку Settings (по кнопке SettingsCommand на MainView.axaml) для Windows и Android. Бэкэнд ISettingsService.cs/SettingsService.cs для сохранения настроек в файле.

Версия для Android содержит кнопку возврата на предыдущую страницу и  настройки: 
1) Переключатель тем (Dark, Light) default Dark
2) Выбор папки для сохранения PNG

Версия для Android содержит кнопку возврата на предыдущую страницу и  настройки: 
1) Переключатель тем (Dark, Light) default Dark
2) Выбор папки для сохранения PNG
2) Выбор FFmpeg.exe path.

Используй для кнопок
        <Button Grid.Column="0" Content="📂" Command="{Binding OpenCommand}" />
        <Button Grid.Column="2" Content="⚙️" Command="{Binding SettingsCommand}" />
        <Button Grid.Column="3" Content="💾" Command="{Binding SaveCommand}" />
иконки  open-*.svg и settings-*.svg, save-*.svg в зависимости от темы.


Для успешно сохраненной PNG над "Button Content="Cancel"" разместить Button с текстом "Open folder" который будет открывать:
Для Windows папку с сохраненным PNG В проводнике и выделять сохраненный PNG файл.
Для Android, если возможно, открыть папку с сохраненным PNG в файловом менеджере и выделить сохраненный PNG файл. 
Если это невозможно, то просто открыть папку с сохраненным PNG.
Либо открыть сохраненный PNG в приложении по умолчанию, если это возможно.


