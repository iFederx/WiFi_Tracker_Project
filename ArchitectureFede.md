Architettura della mia parte


//congigurazione
Innanzitutto, all'avvio del software, c'è la fase di configurazione: vengono gestite una per una le schedine e salvate con la loro posizione e stanza, come oggetti Station.
Le schedine richiedono di essere registrate al server tramite una richiesta al socket passivo.



//elaborazione
Quando tutte e schedine sono configurate, queste cominciano a raccogliere pacchetti.
Scaduto il tempo di raccolta pacchetti, le schedine mandano una richiesta di accoppiamento al server (sempre nel socket in ascolto).
Il server, riconoscerà le schedine registratesi in fase di configurazione dal MAC. Se la schedina sarà riconosciuta, allora le verrà mandato un OK e verrà creato un socket (con accept) che verrà passato ad un thread.
Quando la schedina riceve l'ok inizia a mandare tutti i pacchetti raccolti (mettersi d'accordo sul formato di questi).
