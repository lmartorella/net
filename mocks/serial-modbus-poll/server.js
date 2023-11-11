import { SerialPort } from 'serialport';
import { InterByteTimeoutParser } from '@serialport/parser-inter-byte-timeout';
import crc16modbus from 'crc/crc16modbus';

const openPort = async () => {
    const ports = (await SerialPort.list()).filter(port => port.friendlyName.indexOf("USB-to-Serial Comm Port") >= 0);

    const port = new SerialPort({
        path: ports[0].path,
        baudRate: 19200,
    });

    return new Promise((resolve, reject) => {
        // The open event is always emitted
        port.on('open', err => {
            if (err) {
                reject(err);
            } else {
                resolve(port);
            }
        });
    });
}

const toHex = buffer => {
    return Array.from(buffer).map(byte => `[${byte.toString(16).padStart(2, '0')}]`).join('');
};

(async () => {
    const port = await openPort();

    // Use RTS-based RS485 adapter. RTS low = transmit
    port.set({ rts: true });

    const parser = port.pipe(new InterByteTimeoutParser({ interval: 1 })); // in ms

    const writeData = async (data) => {
        console.log("-> ", toHex(data));
        port.set({ rts: false });
        await new Promise(resolve => setTimeout(resolve, 1));
        port.write(data);
        await new Promise((resolve, reject) => {
            port.drain(err => {
                if (err) reject(err); else resolve();
            });
        })
        // Wait for complete flush + 1ms
        const waitMs = 1 + (10 * data.length) / 19200 * 1000;
        const end = performance.now() + waitMs;
        while (performance.now() < end) { }
        port.set({ rts: true });
    }

    const toBe = n => ([n >> 8, n & 0xff]);
    const toLe = n => ([n & 0xff, n >> 8]);
    const fromBe = (data, i) => (data[i] << 8) + data[i + 1];
    const fromLe = (data, i) => (data[i + 1] << 8) + data[i];

    const poll = (address, count) => {
        console.log(`Polling address ${address}, count ${count}...`);
        const data = [
            0x1,    // Address modbus
            0x3,    // Function: read holding registers
            ...toBe(address),
            ...toBe(count)
        ]
        const crc = crc16modbus(data);
        data.push(...toLe(crc));
        writeData(Uint8Array.from(data));

        parser.once('data', async data => {
            console.log("<- ", toHex(data));
    
            if (data[0] != 1) {
                console.log(" Skip: not addressed to node 1");
                return;
            }
            const l = count * 2 + 2 + 3;
            if (data.length < l) {
                console.log(" Skip: truncated message");
                return;
            }

            const crc = fromLe(data, l - 2);
            if (crc16modbus(data.slice(0, l - 2)) !== crc) {
                console.log(" Skip: invalid CRC");
                return;
            }
    
            // Decode the function code
            if (data[1] != 3) {
                console.log(" Err: function code expected: 0x03");
                return;
            }
            if (data[2] != count * 2) {
                console.log(" Err: buffer size expected: " + count * 2);
                return;
            }
    
            let values = [];
            for (let i = 0; i < count; i++) {
                values.push(fromLe(data, i * 2 + 3));
            }
            console.log(`Data: ${values.join(", ")}`);
        });
    }

    setInterval(() => poll(0x200, 6), 1000);

})();