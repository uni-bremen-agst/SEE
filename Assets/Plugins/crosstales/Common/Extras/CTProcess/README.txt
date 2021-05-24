# CTProcess

## Description
This package can start processes on all standalone platforms. 



## Notes:

### macOS (notarization and Mac App Store)
To get an app with this package through Apples signing process, do one of the following things:

1) Add the following key to the entitlement-file:
<key>com.apple.security.cs.disable-library-validation</key><true/>

2) Sign the libraries after building:
codesign --deep --force --verify --verbose --timestamp --sign "Developer ID Application : YourCompanyName (0123456789)" "YourApp.app/Contents/Plugins/libProcessStart.bundle"

If everything fails, delete "libProcessStart.bundle".
