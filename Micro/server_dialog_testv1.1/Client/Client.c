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
#define DEFAULT_SERVER_IP "192.168.1.6"
#define DEFAULT_SERVER_PORT "1500"

char    buf[BUFLEN];	/* transmission buffer */
char	rbuf[BUFLEN];	/* reception buffer */

/* FUNCTION PROTOTYPES */
int mygetline(char * line, size_t maxline, char *prompt);
int start_socket_connection(char *ip, char *port);
int client_register(int socket);
unsigned long int client_timesync(int socket);

/* GLOBAL VARIABLES */
char *prog_name;

int main(int argc, char *argv[])
{
    int s;
    char strport[4];
    s = start_socket_connection(DEFAULT_SERVER_IP, DEFAULT_SERVER_PORT);

	/* client registration procedure */
	int port;
	while(1)
	{
		if ((port = client_register(s)) != -1)
		{
			printf("[*] REGISTERED correctly! Port received is [%d].\n", port);
			printf("[*] DISCONNECTION from registration server.\n");
			close(s);
			break;
		}else{
			printf("[x] Error in the registration... RETRYING! \n");
		}
	}

    sprintf(strport, "%d", port);
	printf("[!] STARTED connecting to %s:%s.\n", DEFAULT_SERVER_IP, strport);

	/* create the room socket */
    s = start_socket_connection(DEFAULT_SERVER_IP, strport);

	/* client syncronization procedure */
	unsigned long int timestamp;
	while(1)
	{
		if ((timestamp = client_timesync(s)) != -1)
		{
			printf("[*] SYNCRONIZED correctly! Timestamp is [%lu].\n", timestamp);
			close(s);
			break;
		}else{
			printf("[x] Error in the syncronization... RETRYING! \n");
		}
	}

	printf("[!] STARTED main process\n");
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

int start_socket_connection(char *ip, char *port)
{
	uint16_t	   		tport_n, tport_h;	/* server port number (net/host order) */
	int 		   		skt;
	int		       		result;				/* variable to check the build status */
	struct sockaddr_in	saddr;				/* server address structure */
	struct in_addr		sIPaddr; 			/* server IP address. structure */

	result = inet_aton(ip, &sIPaddr);
	if (!result)
	{
		printf("(!) Error: Invalid address.\n");
		return -1;
	}

	if (sscanf(port, "%"SCNu16, &tport_h)!= 1)
	{
		printf("(!) Error: Invalid port number.\n");
		return -1;
	}
	tport_n = htons(tport_h);

    skt = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if(skt<0)
    {
        printf("\n(!) Error: Impossible to open socket.\n");
        return -1;
    }

    bzero(&saddr, sizeof(saddr));
    saddr.sin_family = AF_INET;
    saddr.sin_port = tport_n;
    saddr.sin_addr = sIPaddr;

    if(connect(skt, (struct sockaddr *) &saddr, sizeof(saddr))!=0)
    {
        printf("(!) Error: Impossible to connect with the server.\n");
        return -1;
    }else{
        printf("(*) Connected to target address.");
    }

    return skt;
}



int client_register(int socket)
{
	int n=0, port=0;

	while(!(strcmp(buf,"CLOSE")==0))
	{
		size_t	len;
		mygetline(buf, BUFLEN, "Enter command: ");
		len = strlen(buf);
		if(writen(socket, buf, len) != len)
		{
			printf("Write error\n");
			break;
		}

		printf("waiting for the response...\n\r");
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
		else {
			rbuf[n]='\0';
			printf("Received : [%s]\n", rbuf);
			if(strstr(rbuf,"OKPORT") != NULL)
			{
				printf("[*] PORT ok!\n");
				sscanf(rbuf, "OKPORT(%d)\r\n", &port);
				port = ntohs(port);
			}
			return port;
		}
	}
	return -1;
}

unsigned long int client_timesync(int socket)
{
	int n=0;
	unsigned long int timestamp = 0;

	while(!(strcmp(rbuf,"CLOSE")==0))
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
		{
			rbuf[n]='\0';
			printf("Received : [%s]\n", rbuf);

			if(strstr(rbuf, "PING") != NULL){
				printf("[*] PING received!\n");
				strcpy(buf, "PONG\r\n");
				len = strlen(buf);
				if(writen(socket, buf, len) != len)
				{
					printf("Write error\n");
					return 0;
				}
				printf("[*] PONG sent!\n");
			}

			if(strstr(rbuf,"CLOCK") != NULL)
			{
				printf("[*] CLOCK ok!\n");
				sscanf(rbuf, "CLOCK(%lu)\r\n", &timestamp);
				return timestamp;
			}
		}
	}
	return 0;
}
