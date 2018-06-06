import * as net from 'net';

let sendToServer = (data: string, handler: (data: string) => void) => {
    // Make request to server
    let pipe = net.connect('\\\\.\\pipe\\NETGARDEN', () => {
        // Connected
        pipe.setNoDelay(true);
        pipe.setDefaultEncoding('utf8');
        
        let resp = '';

        let respond = () => {
            pipe.destroy();
            handler(resp);
        };

        pipe.on('data', data => {
            resp += data.toString();
            if (resp.charCodeAt(resp.length - 1) === 13) {
                respond();
            }
        });
        pipe.once('end', data => {
            respond();
        });
        
        // Send request
        pipe.write(data);
    });
}

export { sendToServer };