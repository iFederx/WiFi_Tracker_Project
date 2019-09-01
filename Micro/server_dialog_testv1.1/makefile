# Makefile for echo client and server
#
EXE 	=	./Client/Client ./Server/Server

SRC 	=	./Client/Client.c ./Server/Server.c

FLAGS	=	-Wall

all:		$(SRC) sockwrap.o sockwrap.h errlib.o errlib.h
		for f in $(EXE); do (cc $(FLAGS) -o $$f $$f.c sockwrap.o errlib.o); done

sockwrap.o:	sockwrap.c sockwrap.h
		cc $(FLAGS) -c -o sockwrap.o sockwrap.c

errlib.o:	errlib.c errlib.h
		cc $(FLAGS) -c -o errlib.o errlib.c

clean:
		rm -f $(EXE) *.o
