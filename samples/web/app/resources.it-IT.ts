export const Strings = { 
    "Device_StatusLoading": "Caricamento...",
    "Device_StatusError": "ERRORE",
    "Device_StatusPartiallyOnline": "Parzialmente Online",

    "Error": err => `Errore: ${err}`,

    "Garden_QuickCycle": "Programma veloce:",
    "Garden_Minutes": "Minuti: ",
    "Garden_AddImmediate": "Aggiungi ciclo manuale",
    "Garden_ClearImmediate": "Cancella",
    "Garden_StartImmediate": "Vai!",
    "Garden_StartedImmediate": "Avviato",
    "Garden_ImmediateError": err => `Non posso avviare: ${err}`,
    "Garden_Stopped": "Fermato!",
    "Garden_StopError": err => `Non posso fermare: ${err}`,
    "Garden_NextCycles": "Prossime irrigazioni:",
    "Garden_ScheduledProgram": args => `Programma ${args.name} schedulato ${args.scheduledTime}`,
    "Garden_RunningProgram": args => `Programma ${args.name} in esecuzione`,
    "Garden_QueuedProgram": args => `Programma ${args.name} in coda`,
    "Garden_FlowInfo": "Flusso:",
    "Garden_MissingConf": "Non configurato",
    "Garden_ErrorConf": err => `Errore accedendo alla configurazione: ${err}`,
    "Garden_ErrorSetConf": err => `Configurazione errata: ${err}`,
    "Garden_Suspended": " (sospeso)",
    "Garden_SuspendAll": "Sospendi per pioggia",
    "Garden_ResumeAll": "Ripristina dopo pioggia",
    "Garden_EditProgram": "Modifica programma",
    "Garden_SuspendedCheckbox": "Sospeso:",
    "Garden_DisabledCheckbox": "Disabilitato:",
    "Garden_StartAt": "Inizio:",
    "Garden_Duration": "Durata (min):",
    "Garden_SaveProgram": "Salva Programma",
    "Garden_ClearProgram": "Cancella",

    "Solar_ChartToday": "Andamento oggi",
    "Solar_Chart4days": "Andamento 4 giorni",
    "Solar_EnergyToday": "Energia oggi:",
    "Solar_EnergyTotal": "Energia totale:",
    "Solar_Updated": args => `Aggiornato ${args.currentTs}.`,
    "Solar_Peak1": "Picco di ",
    "Solar_Peak2": args => ` alle ${args.ts}`,
    "Solar_CurrentUsage": "Assorbimento attuale di ",
    "Solar_On": args => `Potenza: ${args.power}W`,
    "Solar_UnknownMode": args => `Errore: modalità sconosciuta: ${args.mode}`,
    "Solar_FaultNoGrid": "Mancanza rete"
};
