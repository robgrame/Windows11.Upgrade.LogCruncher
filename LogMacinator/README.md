# Log Cruncher | Windows 11 Upgrade Log Analyzer

## Overview
Hai grosse
Log Cruncher è uno strumento progettato per analizzare e processare file di log in modo efficiente. È particolarmente utile per gestire grandi quantità di log generati durante l'aggiornamento a Windows 11, fornendo output strutturati in formato JSON per una facile consultazione e analisi, oppure salvati direttamente all'interno di un DB.

## Features
- **Elaborazione dei log in modalità console**: Esegui il tool manualmente per processare i file di log specificati.
- **Esecuzione come servizio**: Automatizza l'elaborazione dei log ogni N minuti.
- **Output strutturato**: I file di output vengono salvati in formato JSON, organizzati in una cartella dedicata per ogni computer.
- **Configurazione personalizzabile**: Specifica i percorsi dei log e dell'output tramite un file di configurazione.
- **Compatibilità con Windows 11**: Ottimizzato per l'ambiente Windows 11 e il runtime .NET 9.

## Requirements
- **Sistema operativo**: Windows 11.
- **Runtime**: .NET Core 9.0. Puoi scaricarlo da [qui](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-9.0.3-windows-x64-installer).
- **Permessi**: Accesso in lettura/scrittura ai percorsi specificati nel file di configurazione.

## Usage
### Configurazione
Prima di utilizzare Log Cruncher, configura il file `appsettings.json` modificando la sezione `LogProcessorSettings`:



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

