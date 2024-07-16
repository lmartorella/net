import child_process from 'child_process';
import fs from 'fs';
import path from 'path';
import { binDir, etcDir, logger } from './settings.mjs';
import { rawPublish, jsonRemoteCall } from './mqtt.mjs';

/**
 * Manages process health
 */
export class ManagedProcess {
    #processName;
    #killTopic;
    #frameworkDir;
    #errLogFile;
    #debug;

    constructor({ processName, killTopic, frameworkDir, debug }) {
        this.#processName = processName;
        this.#killTopic = killTopic;
        this.#frameworkDir = frameworkDir || "";
        this.logFile = path.join(etcDir, `${this.#processName}.log`);
        this.#errLogFile = path.join(etcDir, `${this.#processName}.err`);
        this.#debug = debug;
    }

    start() {
        // Already started
        if (this.process && this.process.pid) {
            throw new Error(`Server process ${this.#processName} already started`);
        }

        // Launch process
        const args = ['--wrk', etcDir];
        if (this.#debug) {
            args.push("--debug", "true");
        }
        const exe = path.join(binDir, this.#frameworkDir, `${this.#processName}.exe`);
        logger(`Starting ${exe} ${args.map(a => `"${a}"`).join(" ")} from ${etcDir}`);

        const logConsoleStream = fs.createWriteStream(this.logFile, { flags: 'a' });
        const logErrorStream = fs.createWriteStream(this.#errLogFile, { flags: 'w' });

        this.process = child_process.spawn(exe, args);
        this.process.stdout.pipe(logConsoleStream);
        this.process.stderr.pipe(logErrorStream);

        this.process.once('exit', async (code, signal) => {
            this.process = null;
            if (code == 0xE0434352) {
                code = ".NetException";
            }
            if (!this.killing) {
                const msg = `Server process ${this.#processName} closed with code ${code}, signal ${signal}`;
                logger(msg, true);

                // Store fail reason to send mail after restart
                try {
                    await this.#sendMail(`${msg}. Restarting`);
                } catch (err) {
                    logger(`Exception sending mail: ${err.message}`);
                }

                await new Promise(resolve => setTimeout(resolve, 3500));
                this.start();
            } else {
                logger(`Server process ${this.#processName} killed`);
            }
            this.killing = false;
        });

        this.process.on('err', err => {
            logger(`Server process ${this.#processName} FAIL TO START: ${err.message}`);
            this.process = null;
        });

        logger(`Home server ${this.#processName} started`);
    }

    startFromRest(res) {
        try {
            this.start();
            res.send(`${this.#processName} started`);
        } catch (err) {
            res.status(500).send(err.message);
        }
    }

    async #sendMail(body) {
        if (fs.existsSync(this.#errLogFile)) {
            body += `\n\n${fs.readFileSync(this.#errLogFile)}`;
        }
        console.error(`Sending restart mail: ${body}`);
        await jsonRemoteCall("notification/send_mail", {
            title: `Server Restarted: ${this.#processName}`,
            body,
            isAdminReport: true
        }, 0, true);
    }

    async #kill() {
        logger(`Server process ${this.#processName} killing...`);
        if (!this.process || !this.process.pid) {
            throw new Error(`Server process ${this.#processName} killed`);
        }
        if (!this.#killTopic) {
            throw new Error("Signal killing not available on this platform");
        }
        // Already started
        this.killing = true;
        await new Promise(async (resolve, reject) => {
            this.process.once('exit', () => {
                resolve();
            });
            void rawPublish(this.#killTopic, "kill").catch(err => reject(err));
        });
    };

    async kill(res) {
        try {
            await this.#kill();
            res.send(`${this.#processName} killed`);
        } catch (err) {
            res.status(500).send(err.message);
        }
    }

    async #restart() {
        await this.#kill();
        logger(`Server process ${this.#processName} killed for restarting...`);
        await new Promise(resolve => setTimeout(resolve, 3500));
        this.start();
    };

    async restart(res) {
        try {
            await this.#restart();
            res.send(`${this.#processName} restarted`);
        } catch (err) {
            res.status(500).send(err.message);
        }
    }
}
