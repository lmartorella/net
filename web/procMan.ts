import * as child_process from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

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

        process.on('exit', (code: number, signal: string) => {
            this.log('Server process closed with code ' + code + ", signal " + signal);
        });

        process.on('err', (err) => {
            this.log('Server process FAIL TO START: ' + err.message);
        });

        console.log('Home server started.');
    }

    private log(msg: string): void {
        fs.appendFileSync(this.logPath, msg + '\n');
    }

    public async kill(): Promise<void> {
        // Send Ctrl+break
        let deferred;
        let p = new Promise<void>((resolve, reject) => {
            deferred = resolve;
        });
        this.process.on('exit', () => {
            deferred();
            console.log('Home server killed.');
        });
        this.process.kill('SIGINT');
        return p;
    }
}
