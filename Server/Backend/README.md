# SEE Managed Server Backend

This is the backend for managing running and managing SEE server instances.

## Development

Useful information concerning the development environment are collected here.

### NetBeans

NetBeans seems to work pretty well, enjoy coding!

### Eclipse

Eclipse seems to have problems recognizing getters/setters generated using Lombok.
There was an attempt to fix this issue, but it did help.
If you are using Eclipse and know how to fix it, please do and update this section.

#### Lombok

Install Project Lombok integration to allow Eclipse to see auto-generated getter/setter functions.

Check the [official manual](https://projectlombok.org/setup/eclipse) on the project website.

The process should boil down to this:

1. Click *Help* > *Install New Software…*
2. Add the source (*Work with:*): `https://projectlombok.org/p2`
3. Select and install Lombok.
4. Quit and open Eclipse again. Don't use the restart option.

**Note:** This did not make it work after all. Please update this section if you can resolve the issue!
