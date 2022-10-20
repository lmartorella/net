This folder contains the Visual Studio code project to build a master node for a Linux OS (e.g. Raspberry).

The whole C Microchip core code is shared with this implementation.

> TODO: implement a event-based timer support instead of polling like in the Microchip version.

## RS485 USB dongle

USB dongle to connect with a RS485 line are available on the market. These adapters should be visible as `tty` device.

> Still to try.

## UART on Raspberry

Currently the RS485 line is directly driven for a Raspberry target with a level translator (MAX485).

The application will then use `UART0` (of the PL011), so configure the pins in the alternate mode to expose the `UART0` in the 40-pin header (`GPIO14` as TX and `GPIO15` as RX, pin 8 and 10 respectively). The `UART1` is not good since it doesn't support 9-bit transmission.

> It is however possible to write low-level code to simulate a serial line, but this requires more CPU power to continuously poll the electrical values.

```
raspi-gpio set 14 a0
raspi-gpio set 15 a0
```

In addition, the `GPIO2` (pin 3) is used to drive the MAX485 tx/rx enable line.

# How to build on a real Raspberry device

Simply connect the Raspberry on the same LAN of the build PC, and configure the Visual Studio Code project to use the remote GCC tools in the device.

# How to install it as a service

To install the master node as a service that automatically starts when the Linux OS is powered, you can use `systemd`.

Create a new service file, for example `/lib/systemd/system/netmaster.service`:
```
[Unit]
Description=NetMaster
After=network.target

[Service]
ExecStart=/home/pi/netmaster-release
Restart=always
RestartSec=3s

[Install]
WantedBy=multi-user.target
```
The restart mode set to `always` is needed since the application will treat any network issues (like socket errors, etc...) as application termination, like the embedded MCU counterpart. 

However the MCU is configured to restart after an error or a watchdog timeout, so here we need to configure the service in the same way.

The `ExecStart` should point to a valid executable file, or you can use a handy link to the compilation folder.

The service is configured to start after the network is up, before user login in case of headless servers.

Don't forget to enable the service after the creation:

```
sudo systemctl enable netmaster
sudo systemctl start netmaster
```

# How to build on Windows 10 (for testing)

Install the Windows Linux Subsystem and your preferred linux distro.

Enable SSH there and install gcc tools.

Configure VS Code to use WSL as a build host.
