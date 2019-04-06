Socket server per debug della ESP32


WEAKNESS
ad ogni nuovo bind che fa la ESP dati sfalzati (ne riceve per il numero di stessi bind che fa la scheda)
insomma inizia a creare più socket associati allo stesso ip+port (credo...) oppure manca solo un flush del 
buffer in ricezione dopo che stampa nella textbox.

NOTA:
l'ip viene autorilevato, solo la porta va specificata da codice! entrambi vengono poi mostrati per andarli a 
inserire nella esp.	