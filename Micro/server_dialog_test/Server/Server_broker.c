/* Simple Socket Server */
/* 
 * Main Function: Server setup the socket connection and
 * start listen in the port in input. 
 * Every time a Client ask to connect Server make a fork, crating a child
 * process that will handle the exchange of messages.
 * The Server after the sync message will only print the messages received frm client*/

#include 	<unistd.h>
#include    <stdlib.h>
#include    <stdio.h>
#include    <string.h>
#include    <inttypes.h>
#include    <sys/stat.h>
#include    <sys/wait.h>
#include    "../errlib.h"
#include    "../sockwrap.h"

#define DEFAULT_SERVER_IP     "192.168.1.223"
#define DEFAULT_SERVER_PORT   "1500"

#define BUFLEN	128 			/* BUFFER LENGTH */
#define maxConn 1; 			    /* fix #max of connections */
char    buf[BUFLEN];			/* transmission buffer */
char	rbuf[BUFLEN];			/* reception buffer */

/* FUNCTION PROTOTYPES */
int mygetline(char * line, size_t maxline, char *prompt);
void server_service(int socket);
void send_error(int socket);
void zombie_handler();

/* GLOBAL VARIABLES */
char *prog_name;
struct stat st;

int main(int argc, char *argv[])
{
    int		            conn_request_skt;		/* passive socket */
    uint16_t 	        lport_n, lport_h;		/* port used by server (net/host ord.) */
    int	 	            s;			        	/* connected socket */
    int                 bklog = maxConn;     	/* max # of connections  */
    socklen_t 	        addrlen;
    struct sockaddr_in 	saddr, caddr;	        /* server and client addresses */
    int		            childpid;				/* pid of child process */

	/* store the prog name in the caller */
    prog_name = argv[0];

    /* get server port number */
    if (sscanf(DEFAULT_SERVER_PORT, "%hu", &lport_h)!=1)
        printf("[!] Error Invalid port number");
    lport_n = htons(lport_h);

    /* create the socket */
    s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    printf("[*] Socket Created\n");

    /* bind the socket to any local IP address */
    bzero(&saddr, sizeof(saddr));
    saddr.sin_family      = AF_INET;
    saddr.sin_port        = lport_n;
    saddr.sin_addr.s_addr = INADDR_ANY;     //Server ip_adrr is 0.0.0.0

    /* bind */
    /* Specify the port and the local adress we are listening to */
    if ( bind(s, (struct sockaddr *) &saddr, sizeof(saddr)))
    {
        printf("[!] Error: Socket Bind Error.");
        exit(1);
    }
    else
        printf("[*] Socket Binded\n");

    /* listen */
    /* mark the referred socket as passive, that is a socket that will
     * be used to accept incoming connection through accept() */
    if ( listen(s, bklog))
    {
        printf("[!] Error: Socket Listening Error.");
        exit(1);
    }
    else
        printf("[*] Socket listening for connections at %s:%s\n", DEFAULT_SERVER_IP, DEFAULT_SERVER_PORT);
    conn_request_skt = s;
    /* main server loop */
    for (;;)
    {
        /* accept next connection */
        addrlen = sizeof(struct sockaddr_in);
        s = accept(conn_request_skt, (struct sockaddr *) &caddr, &addrlen);
        printf("Connection from socket [%d] accepted... \n",s);
        
        
        /* fork a new process to serve the client on the new connection */
        if((childpid=fork())<0) 
        { 
			/* fork() return -1 to the parent if error accours in creating the process */
            printf("[!] Error: Task fork() failed");
            close(s);
        }
        else if (childpid > 0)
        { 
            /* fork() return >0 (which is childID) to the parent process */
            /* parent process */
            close(s);
            /* close connected socket handling the zombie too */	
            signal(SIGCHLD,zombie_handler);
        }
        else
        {
			/* fork() return 0 to the child process */
            /* child process */
            /* here we close the passive one and we enter the server_service  */
            close(conn_request_skt);	/* close passive socket */
            /* main child service */        
            server_service(s);
            close(s);
            break;
        }             
    }
    exit(0);
}

/* this is the main function of the program, it execute the service required by the server 
 * service consist in sending a message to the client at the beginning and then in listaning
 * in the buffer for messages (of any kind) til the CLOSE message... 
 * params: as params we have the identifier of the socket connected
 * return: void
 * */
void server_service(int socket){ 
	size_t len;
    char mac[17];

    //receive "REGISTER REQUEST"
    while((len=recv(socket, rbuf, BUFLEN, 0))>0)
    {
        rbuf[len]='\0';
        printf("[+] Received : [%s]\n", rbuf);
        if(strstr(rbuf,"REGISTER") != NULL)
        {
            printf("[*] MAC ok!\n");
            char* pos = strstr(rbuf,"(");
            memcpy(mac,pos+1,17);
            break;
        }
    }
    printf("[*] request received from [%s]\n", mac);

    char socket_string[20];
    sprintf(socket_string, "%d", htons(1500+socket));
    strcpy(buf, "OKPORT(");
    strcat(buf, strcat(socket_string,")\r\n"));
    printf("[!] message to send created\n");

    len = strlen(buf);
    if(write(socket, buf, len) != len)
    {
        printf("[!] Error: Socket Write error\n");
        exit(1);
    }
    printf("[*] Request sended to port [%d]\n", (1500+socket));

    printf("[*] Client registered Successfully... DISCONNECTION\n");

}

/* procedure for sending the "err" message to the client
 * 		this is a programming decision...because this part is repeated so much time 
 * 		in the code and writing like this i'm optimizing the code structure 
 * params: as params we have the identifier of the socket connected
 * return: void (it doesnt expect a return variable)
 * */
void send_error(int socket)
{
    size_t msg_len=0; 
    strcpy(buf, "-ERR\r\n");
    msg_len = strlen(buf);
    writen(socket,buf,msg_len);
}

/* procedure for handling the zombie process
 * params: none
 * return: void (it doesnt expect a return variable)
 * */
void zombie_handler()
{
    //wait all child termination
    while(waitpid((pid_t)(-1),0,WNOHANG)>0){}
    printf("(!!)I'm killing the zombie!\n");
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


