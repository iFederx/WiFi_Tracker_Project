/*
 *	File Client0.c
 *      ECHO TCP CLIENT with the following feqatures:
 *      - Gets server IP address and port from keyboard 
 *      - LINE/ORIENTED:
 *        > continuously reads lines from keyboard
 *        > sends each line to the server
 *        > waits for response and diaplays it
 *      - Terminates when the "close" or "stop" line is entered
 */


#include     <stdlib.h>
#include     <string.h>
#include     <inttypes.h>
#include     "errlib.h"
#include     "sockwrap.h"

#define BUFLEN	128 /* BUFFER LENGTH */

/* FUNCTION PROTOTYPES */
int mygetline(char * line, size_t maxline, char *prompt);
int iscloseorstop(char *buf);

/* GLOBAL VARIABLES */
char *prog_name;


int main(int argc, char *argv[])
{
    char     	   buf[BUFLEN];		/* transmission buffer */
    char	   rbuf[BUFLEN];	/* reception buffer */

    uint16_t	   tport_n, tport_h;	/* server port number (net/host ord) */

    int		   s;
    int		   result;
    struct sockaddr_in	saddr;		/* server address structure */
    struct in_addr	sIPaddr; 	/* server IP addr. structure */


    prog_name = argv[0];

   /* input IP address and port of server */
    mygetline(buf, BUFLEN, "Enter host IPv4 address (dotted notation) : ");
    result = inet_aton(buf, &sIPaddr);
    if (!result)
	err_quit("Invalid address");

    mygetline(buf, BUFLEN, "Enter port : ");
    if (sscanf(buf, "%" SCNu16, &tport_h)!=1)
	err_quit("Invalid port number");
    tport_n = htons(tport_h);

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
    printf("Enter line 'close' or 'stop' to close connection and stop client.\n");
	
    for (buf[0]='\0' ; !iscloseorstop(buf); )
    {
        size_t	len;

        mygetline(buf, BUFLEN, "Enter line (max 127 char): ");
	strcat(buf,"\n");
	len = strlen(buf);
	if(writen(s, buf, len) != len)
	{
	    printf("Write error\n");
	    break;
	}
	}
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

/* Checks if the content of buffer buf equals the "close" or "stop" line */
int iscloseorstop(char *buf)
{
	return (!strcmp(buf, "close\n") || !strcmp(buf, "stop\n"));
}
