# Windows 11 Upgrade | Log Cruncher

Per utilizzarlo è necessario avere installato il runtime di .NET core 9.0 (https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-9.0.3-windows-x64-installer)

Prima di lanciarlo, apri il file di configurazione e modifica la sezione LogProcessorSettings.
Le voci da moficare sono 2:

- LogsRootPath: è il percorso in cui sono presenti i file di log da processare
- OutputPath: è il percorso in cui verranno salvati i file di output in formato JSON. Il tool creerà una cartella con il nome del computer in cui è stato eseguito il tool e all'interno di questa cartella verranno salvati i file di output.

     ```json
    "LogProcessorSettings": {
        "LogsRootPath": "C:\\MACINATOR\\Repository",
        "OutputPath": "C:\\MACINATOR\\"
    }
    ```
    N.B. I percorsi devono essere scritti con i doppi backslash "\\".

 L'eseguibile da lanciare è **LogCruncher.exe** presente nello zip  **LogMacinator.zip**.

 Per utilizzare il tool hai due possibilità:

 - Esegui il file **LogCruncher.exe** passando come paramentro _--console_ oppure _-c_: in questo caso il tool verrà eseguito in modalità console e processerà i file di log presenti nella cartella specificata nel file di configurazione.
 - Esegui il file **LogCruncher.exe** passando il paramentro _--service_ oppure _-s_: in questo caso il tool verrà eseguito come servizio ed elaborerà i file di log presenti nella cartella specificata nel file di configurazione ogni 5 minuti.

