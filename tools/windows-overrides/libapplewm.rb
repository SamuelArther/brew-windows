class Libapplewm < Formula
  desc "Xlib-based library for the Windows-WM extension (libWindowsWM)"
  homepage "https://www.x.org/"
  url "https://xorg.freedesktop.org/archive/individual/lib/libWindowsWM-1.0.1.tar.bz2"
  sha256 "25f07cf847cbe91d36c20f348b4b9494c4504fe02966e8cdf65cf56316a70edc"
  license "MIT"

  # libAppleWM is the client side of XQuartz's Apple-WM extension, which lets X11 programs work with
  # the native macOS window manager. X servers on Windows (Cygwin/X's XWin, VcXsrv) run rootless
  # with the native Windows window manager the same way and expose the Windows-WM extension
  # instead; its client library is X.Org's libWindowsWM, which was forked from libAppleWM.
  # This keg carries libWindowsWM 1.0.1 and the windowswmproto headers, plus an applewm.h that maps
  # the Apple-WM calls Windows-WM shares (query, update control, input selection, front process)
  # onto it. There is no prebuilt Xlib for native Windows to link a library against, so
  # libWindowsWM is installed as its single source file (share/libapplewm/windowswm.c) to compile
  # into a program built with an Xlib for Windows (Cygwin/X or VcXsrv's build tree).

  livecheck do
    url "https://xorg.freedesktop.org/archive/individual/lib/"
    regex(/href=.*?libWindowsWM[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "windowswmproto" do
    url "https://xorg.freedesktop.org/archive/individual/proto/windowswmproto-1.0.4.tar.bz2"
    sha256 "c05bb0edb627554fe97aa1516aed44accf6566b1db0e50332689a24afcebd26b"
  end

  def install
    resource("windowswmproto").stage do
      (include/"X11/extensions").install "windowswm.h", "windowswmstr.h"
    end
    pkgshare.install "src/windowswm.c"
    man3.install "man/WindowsWM.3"
    (include/"X11/extensions/applewm.h").write <<~C
      /* Apple-WM on Windows: the calls the Windows-WM extension shares with Apple-WM, mapped onto
       * libWindowsWM. Window menus, window levels, Quit control and transient attachment are
       * macOS window manager features with no Windows-WM request and are not declared. */
      #ifndef _APPLEWM_H_
      #define _APPLEWM_H_

      #include <X11/Xlib.h>
      #include <X11/extensions/windowswm.h>

      #define AppleWMControllerNotify      WindowsWMControllerNotify
      #define AppleWMActivationNotify      WindowsWMActivationNotify
      #define AppleWMNumberEvents          WindowsWMNumberEvents
      #define AppleWMControllerNotifyMask  WindowsWMControllerNotifyMask
      #define AppleWMActivationNotifyMask  WindowsWMActivationNotifyMask
      #define AppleWMMinimizeWindow        WindowsWMMinimizeWindow
      #define AppleWMBringAllToFront       WindowsWMBringAllToFront
      #define AppleWMHideWindow            WindowsWMHideWindow
      #define AppleWMIsActive              WindowsWMIsActive
      #define AppleWMIsInactive            WindowsWMIsInactive
      #define AppleWMClientNotLocal        WindowsWMClientNotLocal
      #define AppleWMOperationNotSupported WindowsWMOperationNotSupported

      typedef XWindowsWMNotifyEvent XAppleWMNotifyEvent;

      static inline Bool XAppleWMQueryExtension(Display *dpy, int *event_base, int *error_base)
      { return XWindowsWMQueryExtension(dpy, event_base, error_base); }
      static inline Bool XAppleWMQueryVersion(Display *dpy, int *major, int *minor, int *patch)
      { return XWindowsWMQueryVersion(dpy, major, minor, patch); }
      static inline Bool XAppleWMDisableUpdate(Display *dpy, int screen)
      { return XWindowsWMDisableUpdate(dpy, screen); }
      static inline Bool XAppleWMReenableUpdate(Display *dpy, int screen)
      { return XWindowsWMReenableUpdate(dpy, screen); }
      static inline Bool XAppleWMSelectInput(Display *dpy, unsigned long mask)
      { return XWindowsWMSelectInput(dpy, mask); }
      static inline Bool XAppleWMSetFrontProcess(Display *dpy)
      { return XWindowsWMSetFrontProcess(dpy); }

      #endif /* _APPLEWM_H_ */
    C
    (lib/"pkgconfig/windowswm.pc").write <<~PC
      prefix=${pcfiledir}/../..
      includedir=${prefix}/include
      srcdir=${prefix}/share/libapplewm

      Name: WindowsWM
      Description: The WindowsWM Library (compile ${srcdir}/windowswm.c into the program)
      Version: #{version}
      Requires: x11 xext
      Cflags: -I${includedir}
    PC
    doc.install "README", "COPYING"
  end

  test do
    # No system C compiler or Xlib on Windows to build homebrew-core's test program;
    # check the Windows-WM API and the Apple-WM mapping instead.
    assert_match "XWindowsWMSetFrontProcess", (include/"X11/extensions/windowswm.h").read
    assert_match "xWindowsWMQueryVersionReply", (include/"X11/extensions/windowswmstr.h").read
    assert_match "return XWindowsWMSetFrontProcess(dpy);", (include/"X11/extensions/applewm.h").read
    assert_match "XWindowsWMFrameDraw", (pkgshare/"windowswm.c").read
    assert_match "Requires: x11 xext", (lib/"pkgconfig/windowswm.pc").read
  end
end
