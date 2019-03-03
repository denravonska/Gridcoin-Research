packages:=boost openssl curl zlib
native_packages := native_ccache

qt_packages = qrencode

ifeq ($(QT_59),1)
qt_linux_packages:=qt59 expat dbus libxcb xcb_proto libXau xproto freetype fontconfig libX11 xextproto libXext xtrans
qt_darwin_packages=qt59
qt_mingw32_packages=qt59
else
qt_linux_packages:=qt expat dbus libxcb xcb_proto libXau xproto freetype fontconfig libX11 xextproto libXext xtrans
qt_darwin_packages=qt
qt_mingw32_packages=qt
endif

wallet_packages=bdb

upnp_packages=miniupnpc

darwin_native_packages = native_biplist native_ds_store native_mac_alias

ifneq ($(build_os),darwin)
darwin_native_packages += native_cctools native_cdrkit native_libdmg-hfsplus
endif
