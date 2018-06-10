import * as child_process from 'child_process';
import * as path from 'path';
import * as fs from 'fs';
import * as net from 'net';

/**
 * Manages home server health
 */
export default class ProcessManager {
    process: child_process.ChildProcess;
    logPath: string;

    constructor(private binPath: string, private etcPath: string, private processName: string) {
        this.logPath = path.join(this.etcPath, 'log.txt');
    }

    public start(): void {
        // Launch process
        this.process = child_process.spawn(path.join(this.binPath, this.processName), ['', '-wrk', this.etcPath], {
            stdio: 'ignore'
        });

        this.process.once('exit', (code: number, signal: string) => {
            this.log('Server process closed with code ' + code + ", signal " + signal);
        });

        this.process.on('err', (err) => {
            this.log('Server process FAIL TO START: ' + err.message);
        });

        console.log('Home server started.');
    }

    private log(msg: string): void {
        fs.appendFileSync(this.logPath, msg + '\n');
    }

    private async kill(): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            this.process.once('exit', () => {
                resolve();
            });
            //this.process.kill('SIGINT');
            this.sendMessage({ command: "kill" });
        });
    }

    public async restart(): Promise<void> {
        await this.kill();
        console.log('Home server killed. Restarting...');
        this.start();
    }

    public sendMessage(data: any): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            // Make request to server
            let pipe = net.connect('\\\\.\\pipe\\NETHOME', () => {
                // Connected
                pipe.setNoDelay(true);
                pipe.setDefaultEncoding('utf8');
                
                let resp = '';
        
                let respond = () => {
                    pipe.destroy();
                    resolve(JSON.parse(resp));
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
                pipe.write(JSON.stringify(data) + '\r\n');
            });
        });
    }
}
