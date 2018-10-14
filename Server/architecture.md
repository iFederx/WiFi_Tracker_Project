In MainWindow effettuare lo startup del server.
Le stazioni quando si accendono come prima cosa cercano il server ripetutamente finchè non lo trovano, che poi cerca il loro mac in un file in cui tiene le posizioni delle stazioni connesse in passato e se non lo trova chiede di inserire la nuova stazione attraverso un prompt all utente.
Ogni posizione è in CENTIMETRI ed ha origine in basso a sinistra.

I vari thread del server ricevono insiemi di pacchetti ad intervalli regolari. Essi condividono una struttura threadsafe (equivalente a una TreeMap Java?) contenente i pacchetti:
essa permette di:
	-Ottenere un pacchetto, dato il suo hash e senderMAC, in stile HashMap
	-Accedere ai pacchetti per ordine di ricezione (dal più lontano al più vicino nel tempo)
Quando un singolo pacchetto è stato ricevuto da tutte le stazioni, oppure quando è in coda da più di tot, viene rimosso dalla struttura ed inviato a sendToAnalysisQueue().

Punti pericolosi lato thread safeness:
	-Inserzione in lista delle ricezioni- la lista non è threadsafe, ma si può passare ad una lista di quel tipo
	-Controllo lunghezza lista=numero alive stations
	-Potenzialmente inserzione e ricerca delle stazioni nella rispettiva lista
	-Invio del pacchetto a StreamingAnalyze: solo un thread deve inviare il pacchetto in analisi.

StreamingAnalyze dal canto suo si occuperà di analizzare il pacchetto tracciando il dispositivo ed effettuando il computo delle varie statistiche in modalità "streaming", riempiendo le varie strutture dati visualizzate poi dal thread GUI.
