# Makefile for echo client and server
#
EXE 	=	./Client/Client ./Client/Client_register ./Client/Client_timestamp ./Server/Server_broker ./Server/Server_messanger

SRC 	=	./Client/Client.c ./Client/Client_register.c ./Client/Client_timestamp.c ./Server/Server_broker.c ./Server/Server_messanger.c

FLAGS	=	-Wall

all:		$(SRC) sockwrap.o sockwrap.h errlib.o errlib.h
		for f in $(EXE); do (cc $(FLAGS) -o $$f $$f.c sockwrap.o errlib.o); done

sockwrap.o:	sockwrap.c sockwrap.h
		cc $(FLAGS) -c -o sockwrap.o sockwrap.c

errlib.o:	errlib.c errlib.h
		cc $(FLAGS) -c -o errlib.o errlib.c

clean:
		rm -f $(EXE) *.o
