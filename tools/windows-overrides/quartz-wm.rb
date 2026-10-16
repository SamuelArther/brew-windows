class QuartzWm < Formula
  desc "X11 windows as native Windows windows (VcXsrv's multiwindow window manager)"
  homepage "https://github.com/marchaesen/vcxsrv"
  url "https://github.com/marchaesen/vcxsrv/releases/download/21.1.16.1/vcxsrv-64.21.1.16.1.installer.exe"
  sha256 "df7fed8f49665d0592528ab6be9d07111ea73c6848283d128b77690e05b8f90b"
  license "GPL-3.0-only"

  # quartz-wm is XQuartz's window manager: it gives X11 clients native macOS window frames. On
  # Windows the same job is done by the X server's integrated multiwindow window manager, so this
  # packages the official VcXsrv build (its NSIS installer is read as an archive, never run) and
  # `quartz-wm` starts it in -multiwindow mode, with clipboard sharing and OpenGL.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/vcxsrv.exe", "quartz-wm", "-multiwindow", "-clipboard", "-wgl"
  end

  def caveats
    <<~EOS
      `quartz-wm` starts VcXsrv on display :0 with its multiwindow window manager.
      Point X11 clients at it with:
        set DISPLAY=localhost:0
    EOS
  end

  test do
    # The X server is a GUI program; check the installed server and its X tools instead.
    assert_path_exists libexec/"vcxsrv.exe"
    assert_path_exists libexec/"xkbdata"
    assert_match "xkbcomp", shell_output("#{libexec}/xkbcomp.exe -version 2>&1")
  end
end
