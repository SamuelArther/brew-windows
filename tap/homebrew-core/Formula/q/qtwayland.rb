class Qtwayland < Formula
  desc "Code generator for QtWayland C++ classes (qtwaylandscanner)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "qtwaylandscanner", "qtwaylandscanner"
  end

  def caveats
    <<~EOS
      qtwaylandscanner generates the QtWayland (client) and QtWaylandServer (compositor) C++
      classes for a protocol, as with Qt on Linux. The Wayland platform plugin and QtWaylandCompositor
      need a Wayland display server; on Windows, Qt apps use Qt's native windows platform plugin, and
      Wayland builds run inside WSL (WSLg).
    EOS
  end

  test do
    (testpath/"demo.xml").write <<~XML
      <protocol name="demo">
        <interface name="demo_thing" version="1">
          <request name="destroy" type="destructor"/>
          <request name="rename"><arg name="title" type="string"/></request>
          <event name="done"><arg name="serial" type="uint"/></event>
        </interface>
      </protocol>
    XML
    header = shell_output("#{bin}/qtwaylandscanner client-header #{testpath}/demo.xml")
    assert_match "class Q_WAYLAND_CLIENT_DEMO_EXPORT demo_thing", header
    assert_match "void rename(const QString &title);", header
    assert_match "virtual void demo_thing_done(uint32_t serial);", header
    assert_match "QtWaylandServer", shell_output("#{bin}/qtwaylandscanner server-header #{testpath}/demo.xml")
    code = shell_output("#{bin}/qtwaylandscanner client-code #{testpath}/demo.xml")
    assert_match "title.toUtf8().constData()", code
  end
end
