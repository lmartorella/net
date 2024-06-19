import mqtt from 'mqtt';
import { logger } from './settings.mjs';

logger("Connecting to MQTT...");
const client  = mqtt.connect({ host: "127.0.0.1", clientId: "webserver", protocolVersion: 5 });

client.on('connect', () => {
    logger("Connected to MQTT");
    client.subscribe('ui/resp', err => {
        if (err) {
            console.error("Can't subscribe: " + err.message);
            throw new Error("Can't subscribe: " + err.message);
        }
    });
});

client.on('disconnect', () => {
    logger("Disconnected from MQTT, reconnecting...");
    setTimeout(() => {
        client.reconnect();
    }, 4000);
});

const msgs = { };
let msgIdx = 0;

client.on('message', (topic, payload, packet) => {
    if (topic === "ui/resp") {
        const correlationData = packet.properties?.correlationData.toString();
        const msg = msgs[correlationData];
        if (msg) {
            delete msgs[correlationData];
            if (packet.properties?.contentType === "application/net_err+text") {
                msg.reject(new Error(payload.toString()));
            } else {
                msg.resolve(payload.toString());
            }
        }
    }
});

export const rawPublish = (topic, payload) => {
    if (!client.connected) {
        throw new Error("Broker disconnected");
    }
    return new Promise((resolve, reject) => {
        client.publish(topic, payload, err => {
            if (err) {
                reject(new Error(`Can't publish request: ${err.message}`));
            }
        });
        resolve();
    });
};

export const rawRemoteCall = async (topic, payload, timeoutMs, post) => {
    if (!client.connected) {
        throw new Error("Broker disconnected");
    }
    // Make request to server
    const correlationData = Buffer.from(`C${msgIdx++}`);
    let deferred;
    const promise = new Promise((resolve, reject) => {
        deferred = { resolve, reject };
    });
    msgs[correlationData] = deferred;
    
    client.publish(topic, payload, { properties: { responseTopic: "ui/resp", correlationData } }, err => {
        if (err) {
            deferred.reject(new Error(`Can't publish request: ${err.message}`));
            delete msgs[correlationData];
        }
    });

    if (!post) {
        const timeoutPromise = timeoutMs && new Promise((_, reject) => {
            setTimeout(() => {
                reject(new Error("Timeout contacting the remote process"));
                delete msgs[correlationData];
            }, timeoutMs);
        });
        return await Promise.race([promise, timeoutPromise]);
    } else {
        return "";
    }
};

export const jsonRemoteCall = (topic, payload, timeoutMs, post) => {
    return rawRemoteCall(topic, JSON.stringify(payload), timeoutMs, post);
};
    
export const jsonRestRemoteCall = async (res, topic, json) => {
    try {
        const resp = JSON.parse(await rawRemoteCall(topic, JSON.stringify(json), 3500));
        res.send(resp);
    } catch (err) {
        res.status(500);
        res.statusMessage = err.message;
        res.send(err.message);
    }
};
