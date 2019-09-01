Un semplice server TCP che manda messaggi al Server... 
(in teoria non serve ma nel caso avessi bisogno di debuggare il server perchè i socket 
funzionano bene per via degli ip prova con questo!)

per ricompilare:
	$ gcc errlib.c sockwrap.c Client_sender.c -o sender
	$ ./sender
