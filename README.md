# MultiplePing

MultiplePing is a command-line utility that performs ICMP ping requests to specified IP addresses or domain names and stores the responses in an XML file.

## Features

- Supports both **IP addresses and domain names** (automatic DNS resolution)
- Customizable **ping interval** (default: 100ms)
- Stores responses in a structured **XML file**

## Usage

```sh
multiping [ADDRESSES...] [DURATION]
```

### Example:

```sh
multiping www.google.com www.seznam.cz 30
```

This will ping `www.google.com` and `www.seznam.cz` every **100 ms** for **30 seconds**.

## Arguments

- **ADDRESSES** – One or more web addresses or IP addresses to ping.
- **DURATION** – Time in seconds for how long the pinging should run.

## Output

The results are stored in `ping.xml` in the following format:

```xml
<ping>
  <ipAddresses>
    <ipAddress name="8.8.8.8" idx="0" />
    <ipAddress name="77.75.74.172" idx="1" />
  </ipAddresses>
  <reply idx="0" duration="18" />
  <reply idx="1" duration="20" />
</ping>
```

## Author

David Drtil

## License

This project is licensed under the MIT License.

