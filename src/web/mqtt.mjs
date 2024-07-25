import mqtt from 'mqtt';
import { logger } from './settings.mjs';

logger("Connecting to MQTT...");
const client  = mqtt.connect({ host: "127.0.0.1", clientId: "webserver", protocolVersion: 5 });
const topics = { };
const msgs = { };
let msgIdx = 0;

const subscribeAllTopics = () => {
    Object.keys(topics).forEach(topic => {
        if (!topics[topic].subscribed) {
            logger("Subscribing " + topic);
            topics[topic].subscribed = true;
            client.subscribe(topic, err => {
                if (err) {
                    console.error("Can't subscribe: " + err.message);
                    topics[topic].errHandler(new Error("Can't subscribe: " + err.message));
                }
            });
        }
    });
};

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
        logger("Reconnecting...");
        client.reconnect();
    }, 4000);
});

client.on('message', (topic, payload) => {
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
    } else {
        const handlers = topics[topic];
        if (handlers) {
            let data;
            try {
                data = JSON.parse(payload.toString());
            } catch (err) {
                handlers.errHandler(new Error("Invalid data received"));
                return;
            }
            handlers.dataHandler(data);
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

const rawRemoteCall = async (topic, payload, timeoutMs, post) => {
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

export const subscribeJsonTopic = (topic, dataHandler, errHandler) => {
    topics[topic] = { dataHandler, errHandler };
    if (client.connected) {
        subscribeAllTopics();
    } else {
        logger("Parked subscription " + topic);
    }
};
