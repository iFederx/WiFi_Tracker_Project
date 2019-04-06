/*
 *	File Client0.c
 *      ECHO TCP CLIENT with the following feqatures:
 *      - Gets server IP address and port from ARGUMENTS
 *      - LINE/ORIENTED:
 *	  > wait the accept message from the server (start of exchange of mess)	
 *        > continuously reads lines from keyboard
 *        > sends each line to the server
 *      - Terminates when the "close" or "stop" line is entered
 */


#include     <stdlib.h>
#include     <string.h>
#include     <inttypes.h>
#include     "../errlib.h"
#include     "../sockwrap.h"

#define BUFLEN	128 	/* BUFFER LENGTH */
char    buf[BUFLEN];	/* transmission buffer */
char	rbuf[BUFLEN];	/* reception buffer */

/* FUNCTION PROTOTYPES */
int mygetline(char * line, size_t maxline, char *prompt);
void client_service(int socket);

/* GLOBAL VARIABLES */
char *prog_name;


int main(int argc, char *argv[])
{
    uint16_t	   tport_n, tport_h;	/* server port number (net/host ord) */

    int		   s;
    int		   result;
    struct sockaddr_in	saddr;		/* server address structure */
    struct in_addr	sIPaddr; 	/* server IP addr. structure */


    prog_name = argv[0];

   //store the name of the program, in that case ./Client0
    prog_name = argv[0];
    
    /* argc contains the main arguments so the caller parameters...
     * lets store 'em, but before, we check if the minimum of them are reached */    
    if(argc < 3)
    {
        printf("Usage: %s <server_addr> <port number>\n", prog_name); 
        exit(1);
    } 
    
    /* inet_aton convert into bynary the address 
     * and check if is valid, ret 1 if yes 0 is not accepted */
     /* get ip_addr */
    result = inet_aton(argv[1], &sIPaddr);                                              
    if (!result)
	   err_quit("Invalid address");

	/* get port */
    if (sscanf(argv[2], "%"SCNu16, &tport_h)!=1)
	   err_quit("Invalid port number");
    tport_n = htons(tport_h);       //htons convert to byte the port

    /* create the socket */
    printf("Creating socket\n");
    s = Socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    printf("done. Socket fd number: %d\n",s);

    /* prepare address structure */
    bzero(&saddr, sizeof(saddr));
    saddr.sin_family = AF_INET;
    saddr.sin_port   = tport_n;
    saddr.sin_addr   = sIPaddr;

    /* connect */
    showAddr("Connecting to target address", &saddr);
    Connect(s, (struct sockaddr *) &saddr, sizeof(saddr));
    printf("done.\n");

    /* main client loop */
	client_service(s);
	close(s);
	
	exit(0);
}

/* Gets a line of text from standard input after having printed a prompt string 
   Substitutes end of line with '\0'
   Empties standard input buffer but stores at most maxline-1 characters in the
   passed buffer
*/
int mygetline(char *line, size_t maxline, char *prompt)
{
	char	ch;
	size_t 	i;

	printf("%s", prompt);
	for (i=0; i< maxline-1 && (ch = getchar()) != '\n' && ch != EOF; i++)
		*line++ = ch;
	*line = '\0';
	while (ch != '\n' && ch != EOF)
		ch = getchar();
	if (ch == EOF)
		return(EOF);
	else    return(1);
}


void client_service(int socket)
{
	int n=0;

	while(!(strcmp(buf,"CLOSE")==0))
    {
        size_t	len;

		printf("waiting for the message from the server...\n\r");
		n=recv(socket, rbuf, BUFLEN-1, 0);
		if (n==0)
		{
			printf("Error: Server shut-down during transfering, data will be broken.\n");
			break;
		}
		else if(n<0)
		{
			printf("Write Error.\n");
			break;
		}
		else
			printf("Received : [%s]\n", rbuf);

        mygetline(buf, BUFLEN, "Enter command: ");
		len = strlen(buf);
		if(writen(socket, buf, len) != len)
		{
			printf("Write error\n");
			break;
		}

	}
}
